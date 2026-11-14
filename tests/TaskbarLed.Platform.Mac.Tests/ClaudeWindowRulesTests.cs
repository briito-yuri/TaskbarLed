using TaskbarLed.Core.Visibility;

namespace TaskbarLed.Platform.Mac.Tests;

public class ClaudeWindowRulesTests
{
    private static readonly PixelRect[] Screen = [new(0, 0, 1440, 900)];
    private static readonly PixelRect ClaudeBounds = new(100, 100, 900, 700);

    [Fact]
    public void Claude_in_front_is_visible()
    {
        MacWindow[] windows = [new("Claude", 0, ClaudeBounds), new("Safari", 0, Screen[0])];

        Assert.True(ClaudeWindowRules.IsClaudeVisible(windows, Screen, 0.3));
    }

    [Fact]
    public void Claude_missing_from_the_list_is_hidden_because_it_is_minimized_or_on_another_space()
    {
        MacWindow[] windows = [new("Safari", 0, Screen[0])];

        Assert.False(ClaudeWindowRules.IsClaudeVisible(windows, Screen, 0.3));
    }

    [Fact]
    public void Claude_covered_by_a_full_window_in_front_is_hidden()
    {
        MacWindow[] windows = [new("Code", 0, Screen[0]), new("Claude", 0, ClaudeBounds)];

        Assert.False(ClaudeWindowRules.IsClaudeVisible(windows, Screen, 0.3));
    }

    [Fact]
    public void Dock_menu_bar_and_the_panel_itself_never_hide_claude()
    {
        MacWindow[] windows =
        [
            new("Dock", 20, Screen[0]),
            new("Window Server", 24, new(0, 0, 1440, 25)),
            new("TaskbarLed", 0, Screen[0]),
            new("Claude", 0, ClaudeBounds),
        ];

        Assert.True(ClaudeWindowRules.IsClaudeVisible(windows, Screen, 0.3, ownWindowOwner: "TaskbarLed"));
    }

    [Fact]
    public void Claude_side_by_side_with_another_app_stays_visible()
    {
        MacWindow[] windows = [new("Code", 0, new(0, 0, 720, 900)), new("Claude", 0, new(720, 0, 1440, 900))];

        Assert.True(ClaudeWindowRules.IsClaudeVisible(windows, Screen, 0.3));
    }
}
