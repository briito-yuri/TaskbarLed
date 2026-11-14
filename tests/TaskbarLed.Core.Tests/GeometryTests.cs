using TaskbarLed.Core.Presentation;
using TaskbarLed.Core.Visibility;

namespace TaskbarLed.Core.Tests;

public class OcclusionCalculatorTests
{
    private static readonly PixelRect Screen = new(0, 0, 1920, 1080);
    private static readonly PixelRect[] Screens = [Screen];

    [Fact]
    public void Uncovered_window_is_fully_visible()
    {
        Assert.Equal(1, OcclusionCalculator.VisibleFraction(new PixelRect(100, 100, 900, 700), Screens, []));
    }

    [Fact]
    public void Maximized_window_on_top_hides_it()
    {
        Assert.Equal(0, OcclusionCalculator.VisibleFraction(new PixelRect(100, 100, 900, 700), Screens, [Screen]));
    }

    [Fact]
    public void Half_covered_window_is_about_half_visible()
    {
        var target = new PixelRect(0, 0, 1000, 1000);
        var fraction = OcclusionCalculator.VisibleFraction(target, [new PixelRect(0, 0, 2000, 2000)], [new PixelRect(500, 0, 1000, 1000)]);
        Assert.InRange(fraction, 0.45, 0.55);
    }

    [Fact]
    public void Snapped_side_by_side_counts_as_visible()
    {
        var claude = new PixelRect(0, 0, 960, 1040);
        var other = new PixelRect(960, 0, 1920, 1040);
        Assert.Equal(1, OcclusionCalculator.VisibleFraction(claude, Screens, [other]));
    }

    [Fact]
    public void Window_moved_off_every_screen_is_hidden()
    {
        Assert.Equal(0, OcclusionCalculator.VisibleFraction(new PixelRect(-32000, -32000, -31000, -31000), Screens, []));
    }
}

public class DurationFormatTests
{
    [Theory]
    [InlineData(0, "00:00", "0s")]
    [InlineData(45, "00:45", "45s")]
    [InlineData(767, "12:47", "12m47s")]
    [InlineData(3840, "1:04:00", "1h04m")]
    public void Formats_clock_and_human(int seconds, string clock, string human)
    {
        Assert.Equal(clock, DurationFormat.Clock(TimeSpan.FromSeconds(seconds)));
        Assert.Equal(human, DurationFormat.Human(TimeSpan.FromSeconds(seconds)));
    }
}
