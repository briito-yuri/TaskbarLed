using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Threading;
using TaskbarLed.App.Avalonia.Presentation;
using TaskbarLed.Core.Presentation;
using TaskbarLed.Core.Visibility;
using TaskbarLed.Platform.Mac;
using CorePixelRect = TaskbarLed.Core.Visibility.PixelRect;

namespace TaskbarLed.App.Avalonia.Panels;

/// <summary>
/// The small borderless panel, centered horizontally just above the Dock (or the taskbar on Windows).
/// Shows live sessions independently of the foreground application.
/// </summary>
internal sealed class FloatingPanelWindow : Window
{
    /// <summary>Panel width in DIP/points.</summary>
    public const double PanelWidth = 420;

    /// <summary>Space between the panel and the Dock, in DIP/points.</summary>
    public const double DockGap = 8;

    public const double PanelHeight = PanelLayout.FloatingHeightDip + 18;

    private readonly PanelViewModel _viewModel;
    private readonly Func<PanelFrame> _frames;
    private readonly DispatcherTimer _frameTimer;
    private readonly Action<string?>? _acknowledge;
    private readonly Action? _skip;
    private PanelFrame? _frame;
    private readonly PanelView _panel;
    private readonly bool _pt;
    private (PixelPoint Position, double Width)? _beforeEdit;
    private (PixelPoint Pointer, PixelPoint Window, double Width, bool Resize)? _drag;
    private PanelPreferences _preferences;
    private Action<PanelPreferences>? _savePreferences;

    public bool KeepVisible { get; set; }

    public FloatingPanelWindow(PanelViewModel viewModel, Func<PanelFrame> frames,
        Action<string?>? acknowledge = null, Action? skip = null, bool keepVisible = false, bool portuguese = false)
    {
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        _frames = frames ?? throw new ArgumentNullException(nameof(frames));

        _acknowledge = acknowledge;
        _skip = skip;
        KeepVisible = keepVisible;
        _pt = portuguese;
        _preferences = new PanelPreferences();

        Title = "TaskbarLed";
        WindowDecorations = global::Avalonia.Controls.WindowDecorations.None;
        CanResize = false;
        Topmost = true;
        ShowInTaskbar = false;
        ShowActivated = false;
        Background = Brushes.Transparent;
        TransparencyLevelHint = [WindowTransparencyLevel.Transparent];
        WindowStartupLocation = WindowStartupLocation.Manual;
        Width = PanelWidth;
        Height = PanelHeight;
        _panel = new PanelView { DataContext = viewModel };
        Content = _panel;

        _frameTimer = new DispatcherTimer(TimeSpan.FromMilliseconds(250), DispatcherPriority.Background, (_, _) => Render());
        ApplyFrame();
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        RestorePlacement();

        if (OperatingSystem.IsMacOS())
        {
            // The default handle is the NSView; the window behavior lives on its NSWindow.
            if (TryGetPlatformHandle() is IMacOSTopLevelPlatformHandle mac)
            {
                MacFloatingWindow.JoinAllSpaces(mac.NSWindow);
            }
        }

        _frameTimer.Start();
    }

    protected override void OnClosed(EventArgs e)
    {
        _frameTimer.Stop();
        base.OnClosed(e);
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);
        base.OnPointerPressed(e);
        var point = e.GetCurrentPoint(this);
        if (_beforeEdit.HasValue)
        {
            if (point.Properties.IsLeftButtonPressed && point.Position.Y < PanelHeight)
            {
                _drag = (this.PointToScreen(point.Position), Position, Width, point.Position.X >= Width - 18);
                e.Pointer.Capture(this);
                e.Handled = true;
            }

            return;
        }

        if (point.Properties.IsLeftButtonPressed)
        {
            var channel = (e.Source as Control)?.DataContext as ChannelViewModel;
            _acknowledge?.Invoke(channel?.SessionId ?? _frame?.SelectedSessionId);
            Render();
        }
    }

    /// <summary>Rectangle for the panel on a screen's work area. Shared with the screenshot test.</summary>
    internal static CorePixelRect Layout(global::Avalonia.PixelRect workArea, double scaling) =>
        DockLayout(workArea, scaling);

    private static CorePixelRect DockLayout(global::Avalonia.PixelRect workArea, double scaling)
    {
        var rect = PanelLayout.AboveDock(
            new CorePixelRect(workArea.X, workArea.Y, workArea.Right, workArea.Bottom), scaling, PanelWidth, DockGap);
        return rect with { Top = Math.Max(workArea.Y, rect.Bottom - (int)Math.Round(PanelHeight * scaling)) };
    }

    public void ConfigurePlacement(PanelPreferences preferences, Action<PanelPreferences> save)
    {
        _preferences = preferences;
        _savePreferences = save;
    }

    public void SetTheme(string id) => Persist(_preferences with { Theme = id });

    public void ResetPosition()
    {
        if (_beforeEdit.HasValue)
        {
            EndEdit(save: false);
        }

        Persist(_preferences with { X = null, Y = null, Width = PanelWidth });
        RestorePlacement();
    }

    public void BeginEdit()
    {
        if (_beforeEdit.HasValue)
        {
            return;
        }

        if (!IsVisible)
        {
            Show();
            RestorePlacement();
        }

        _beforeEdit = (Position, Width);
        var save = new Button { Content = _pt ? "Salvar" : "Save" };
        var cancel = new Button { Content = _pt ? "Cancelar" : "Cancel" };
        save.Click += (_, _) => EndEdit(save: true);
        cancel.Click += (_, _) => EndEdit(save: false);

        var grip = new Border
        {
            Width = 18,
            Background = Brushes.Transparent,
            HorizontalAlignment = HorizontalAlignment.Right,
            Cursor = new Cursor(StandardCursorType.SizeWestEast),
            Child = new TextBlock { Text = "⋮", HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center },
        };
        Content = null;
        var preview = new Grid { Cursor = new Cursor(StandardCursorType.SizeAll), Children = { _panel, grip } };
        var footer = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 6,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(4),
            Children = { save, cancel },
        };
        var editor = new Grid { RowDefinitions = new RowDefinitions { new(new GridLength(PanelHeight)), new(GridLength.Star) }, Children = { preview, footer } };
        Grid.SetRow(footer, 1);
        var border = new Border { Child = editor, BorderThickness = new Thickness(1) };
        border.Bind(Border.BackgroundProperty, new global::Avalonia.Markup.Xaml.MarkupExtensions.DynamicResourceExtension("Led.BackgroundBrush"));
        border.Bind(Border.BorderBrushProperty, new global::Avalonia.Markup.Xaml.MarkupExtensions.DynamicResourceExtension("Led.EditBrush"));
        Content = border;
        Height = PanelHeight + 40;
        var screen = Screens.ScreenFromPoint(Position) ?? Screens.Primary;
        if (screen is not null)
        {
            var bottom = screen.WorkingArea.Bottom - (int)Math.Ceiling(Height * screen.Scaling);
            Position = new PixelPoint(Position.X, Constrain(Position.Y, screen.WorkingArea.Y, bottom, snap: false));
        }

        Activate();
        save.Focus();
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (_drag is not { } drag)
        {
            return;
        }

        var pixel = this.PointToScreen(e.GetPosition(this));
        var dx = pixel.X - drag.Pointer.X;
        if (drag.Resize)
        {
            var screen = Screens.ScreenFromPoint(Position) ?? Screens.Primary;
            var scale = screen?.Scaling ?? 1;
            var max = screen is null ? 2000 : (screen.WorkingArea.Right - Position.X) / scale;
            Width = Math.Clamp(drag.Width + dx / scale, 200, Math.Max(200, max));
        }
        else
        {
            Position = new PixelPoint(drag.Window.X + dx, drag.Window.Y + pixel.Y - drag.Pointer.Y);
        }

        e.Handled = true;
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (_drag.HasValue)
        {
            _drag = null;
            e.Pointer.Capture(null);
            e.Handled = true;
        }
    }

    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        _drag = null;
        base.OnPointerCaptureLost(e);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (_beforeEdit.HasValue && e.Key is Key.Enter or Key.Escape)
        {
            EndEdit(e.Key == Key.Enter);
            e.Handled = true;
        }

        base.OnKeyDown(e);
    }

    private void EndEdit(bool save)
    {
        if (_beforeEdit is not { } before)
        {
            return;
        }

        _beforeEdit = null;
        _drag = null;
        var editor = (Grid)((Border)Content!).Child!;
        ((Grid)editor.Children[0]).Children.Remove(_panel);
        Content = _panel;
        Height = PanelHeight;
        if (save)
        {
            _preferences = _preferences with { X = Position.X, Y = Position.Y, Width = Width };
            RestorePlacement(snap: true);
            Persist(_preferences with { X = Position.X, Y = Position.Y, Width = Width });
        }
        else
        {
            Width = before.Width;
            Position = before.Position;
        }
    }

    private void Persist(PanelPreferences preferences)
    {
        _preferences = preferences;
        _savePreferences?.Invoke(preferences);
    }

    private void RestorePlacement(bool snap = false)
    {
        var primary = Screens.Primary ?? (Screens.All.Count > 0 ? Screens.All[0] : null);
        if (primary is null)
        {
            return;
        }

        if (_preferences is { X: int x, Y: int y })
        {
            var saved = new PixelPoint(x, y);
            var screen = Screens.ScreenFromPoint(saved) ?? primary;
            var area = screen.WorkingArea;
            var maxWidth = area.Width / screen.Scaling;
            var requestedWidth = double.IsFinite(_preferences.Width) ? _preferences.Width : PanelWidth;
            Width = Math.Clamp(requestedWidth, Math.Min(200, maxWidth), maxWidth);
            var right = area.Right - (int)Math.Ceiling(Width * screen.Scaling);
            var bottom = area.Bottom - (int)Math.Ceiling(Height * screen.Scaling);
            Position = new PixelPoint(Constrain(x, area.X, right, snap), Constrain(y, area.Y, bottom, snap));
            return;
        }

        var rect = Layout(primary.WorkingArea, primary.Scaling);
        Width = rect.Width / primary.Scaling;
        Height = rect.Height / primary.Scaling;
        Position = new PixelPoint(rect.Left, rect.Top);
    }

    internal static int Constrain(int value, int min, int max, bool snap)
    {
        var clamped = Math.Clamp(value, min, Math.Max(min, max));
        if (snap && clamped - min <= 12)
        {
            return min;
        }

        return snap && max >= min && max - clamped <= 12 ? max : clamped;
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);
        if (!e.Handled)
        {
            _skip?.Invoke();
            Render();
        }
    }

    private void ApplyFrame()
    {
        _frame = _frames();
        _viewModel.Apply(_frame);
    }

    private void Render()
    {
        ApplyFrame();
        var show = _beforeEdit.HasValue || KeepVisible || _viewModel.HasActivity || ContextMenu?.IsOpen == true;
        if (show && !IsVisible)
        {
            Show();
            RestorePlacement();
        }
        else if (!show && IsVisible)
        {
            Hide();
        }
    }
}
