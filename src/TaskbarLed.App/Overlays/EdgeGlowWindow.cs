using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using TaskbarLed.Core.Visibility;

namespace TaskbarLed.App.Overlays;

/// <summary>
/// Thin glowing line on the bottom edge of a monitor, shown over full-screen video when a session
/// finished (green) or needs you (amber). Blinks for a few seconds, then stays softly lit until seen.
/// Clicks pass through, so it never gets in the way of the video player.
/// </summary>
public sealed class EdgeGlowWindow : OverlayWindow
{
    private const int BlinkCount = 10;
    private readonly Border _bar = new();
    private bool? _attention;

    public EdgeGlowWindow()
        : base(clickThrough: true)
    {
        Title = "TaskbarLed edge";
        Content = _bar;
    }

    public void ShowOn(PixelRect bounds, bool attention)
    {
        if (!IsVisible)
        {
            Show();
            _attention = null;
        }

        Place(bounds);

        if (_attention != attention)
        {
            _attention = attention;
            _bar.SetResourceReference(Border.BackgroundProperty, attention ? "Led.AttentionBrush" : "Led.DoneBrush");
            StartBlink();
        }
    }

    public void HideGlow()
    {
        if (IsVisible)
        {
            _bar.BeginAnimation(OpacityProperty, null);
            Hide();
        }

        _attention = null;
    }

    // On/off every half second, then a steady soft glow so it does not distract for the whole movie.
    private void StartBlink()
    {
        var animation = new DoubleAnimationUsingKeyFrames { FillBehavior = FillBehavior.HoldEnd };
        for (var i = 0; i < BlinkCount * 2; i++)
        {
            animation.KeyFrames.Add(new DiscreteDoubleKeyFrame(i % 2 == 0 ? 1 : 0.15, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(i * 0.5))));
        }

        animation.KeyFrames.Add(new DiscreteDoubleKeyFrame(0.7, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(BlinkCount))));
        Timeline.SetDesiredFrameRate(animation, 10);
        _bar.BeginAnimation(OpacityProperty, animation);
    }
}
