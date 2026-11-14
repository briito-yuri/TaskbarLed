using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Threading;
using TaskbarLed.Core.Presentation;
using TaskbarLed.Core.Sessions;
using TaskbarLed.Core.Visibility;
using TaskbarLed.Platform.Windows.Windowing;

namespace TaskbarLed.App.Overlays;

/// <summary>
/// Short LED-style notice in the bottom-right corner over full-screen video ("2 API › DONE IN 12M47S").
/// Shows for a few seconds per alert, queues bursts, and opens Claude when clicked.
/// </summary>
public sealed class AlertWindow : OverlayWindow
{
    private static readonly TimeSpan Duration = TimeSpan.FromSeconds(4);
    private static readonly System.Windows.Duration Fade = new(TimeSpan.FromMilliseconds(250));
    private const int MaxQueued = 3;

    private readonly Queue<PanelAlert> _queue = new();
    private readonly DispatcherTimer _timer;
    private readonly TextBlock _text;
    private MonitorInfo? _monitor;
    private bool _showing;

    public AlertWindow()
        : base(clickThrough: false)
    {
        Title = "TaskbarLed alert";
        SizeToContent = SizeToContent.WidthAndHeight;
        Cursor = Cursors.Hand;

        _text = new TextBlock { FontSize = 13, FontWeight = FontWeights.SemiBold, Margin = new Thickness(12, 7, 12, 7) };
        _text.SetResourceReference(TextBlock.FontFamilyProperty, "Led.FontFamily");
        _text.Effect = new DropShadowEffect { BlurRadius = 8, ShadowDepth = 0, Opacity = 0.75 };

        var border = new Border { CornerRadius = new CornerRadius(4), BorderThickness = new Thickness(1), Child = _text };
        border.SetResourceReference(Border.BackgroundProperty, "Led.BackgroundBrush");
        border.SetResourceReference(Border.BorderBrushProperty, "Led.BorderBrush");
        Content = border;

        _timer = new DispatcherTimer { Interval = Duration };
        _timer.Tick += (_, _) => FadeOut();
    }

    /// <summary>The user clicked the alert.</summary>
    public event EventHandler? OpenRequested;

    public void Enqueue(PanelAlert alert, MonitorInfo monitor)
    {
        ArgumentNullException.ThrowIfNull(alert);
        _monitor = monitor;
        if (_queue.Count >= MaxQueued)
        {
            _queue.Dequeue();
        }

        _queue.Enqueue(alert);
        if (!_showing)
        {
            ShowNext();
        }
    }

    public void Dismiss()
    {
        _queue.Clear();
        _timer.Stop();
        _showing = false;
        if (IsVisible)
        {
            Hide();
        }
    }

    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonUp(e);
        Dismiss();
        OpenRequested?.Invoke(this, EventArgs.Empty);
    }

    private void ShowNext()
    {
        if (_monitor is not { } monitor || !_queue.TryDequeue(out var alert))
        {
            _showing = false;
            Hide();
            return;
        }

        _showing = true;
        var attention = alert.Status == SessionStatus.AwaitingInput;
        _text.Text = (attention ? "! " : "✓ ") + alert.Text;
        var brushKey = attention ? "Led.AttentionBrush" : "Led.DoneBrush";
        _text.SetResourceReference(TextBlock.ForegroundProperty, brushKey);
        var glow = (DropShadowEffect)_text.Effect;
        glow.Color = ((SolidColorBrush)FindResource(brushKey)).Color;
        glow.Opacity = (double)FindResource("Led.GlowOpacity"); // Flat themes have no glow.

        Opacity = 0;
        if (!IsVisible)
        {
            Show();
        }

        UpdateLayout();
        var width = (int)Math.Ceiling(ActualWidth * monitor.Scale);
        var height = (int)Math.Ceiling(ActualHeight * monitor.Scale);
        Place(PanelLayout.Alert(monitor.Bounds, monitor.Scale, width, height));

        BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, Fade));
        _timer.Start();
    }

    private void FadeOut()
    {
        _timer.Stop();
        var fade = new DoubleAnimation(0, Fade);
        fade.Completed += (_, _) => ShowNext();
        BeginAnimation(OpacityProperty, fade);
    }
}
