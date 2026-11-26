using Avalonia;
using Avalonia.Controls;
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

    private readonly PanelViewModel _viewModel;
    private readonly Func<PanelFrame> _frames;
    private readonly DispatcherTimer _frameTimer;
    private readonly Action<string?>? _acknowledge;
    private readonly Action? _skip;
    private PanelFrame? _frame;

    public bool KeepVisible { get; set; }

    public FloatingPanelWindow(PanelViewModel viewModel, Func<PanelFrame> frames,
        Action<string?>? acknowledge = null, Action? skip = null, bool keepVisible = false)
    {
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        _frames = frames ?? throw new ArgumentNullException(nameof(frames));

        _acknowledge = acknowledge;
        _skip = skip;
        KeepVisible = keepVisible;

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
        Height = PanelLayout.FloatingHeightDip;
        Content = new PanelView { DataContext = viewModel };

        _frameTimer = new DispatcherTimer(TimeSpan.FromMilliseconds(250), DispatcherPriority.Background, (_, _) => Render());
        ApplyFrame();
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        PlaceAboveDock();

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
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            var channel = (e.Source as Control)?.DataContext as ChannelViewModel;
            _acknowledge?.Invoke(channel?.SessionId ?? _frame?.SelectedSessionId);
            Render();
        }
    }

    /// <summary>Rectangle for the panel on a screen's work area. Shared with the screenshot test.</summary>
    internal static CorePixelRect Layout(global::Avalonia.PixelRect workArea, double scaling) =>
        PanelLayout.AboveDock(
            new CorePixelRect(workArea.X, workArea.Y, workArea.Right, workArea.Bottom), scaling, PanelWidth, DockGap);

    private void PlaceAboveDock()
    {
        var screen = Screens.Primary ?? (Screens.All.Count > 0 ? Screens.All[0] : null);
        if (screen is null)
        {
            return;
        }

        var rect = Layout(screen.WorkingArea, screen.Scaling);
        Width = rect.Width / screen.Scaling;
        Height = rect.Height / screen.Scaling;
        Position = new PixelPoint(rect.Left, rect.Top);
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);
        _skip?.Invoke();
        Render();
    }

    private void ApplyFrame()
    {
        _frame = _frames();
        _viewModel.Apply(_frame);
    }

    private void Render()
    {
        ApplyFrame();
        var show = KeepVisible || _viewModel.HasActivity;
        if (show && !IsVisible)
        {
            Show();
            PlaceAboveDock(); // The Dock may have moved or resized while hidden.
        }
        else if (!show && IsVisible)
        {
            Hide();
        }
    }
}
