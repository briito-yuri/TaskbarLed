using TaskbarLed.Core.Settings;

namespace TaskbarLed.Core.Tests;

public class PanelSettingsTests
{
    [Fact]
    public void New_installs_start_with_the_Claude_Code_theme()
    {
        Assert.Equal("ClaudeCode", new PanelSettings().Theme);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void A_missing_theme_falls_back_to_the_default(string? theme)
    {
        Assert.Equal(PanelSettings.DefaultTheme, (new PanelSettings() with { Theme = theme! }).Normalize().Theme);
    }

    [Fact]
    public void A_chosen_theme_is_kept()
    {
        Assert.Equal("Matrix", (new PanelSettings() with { Theme = "Matrix" }).Normalize().Theme);
    }
}
