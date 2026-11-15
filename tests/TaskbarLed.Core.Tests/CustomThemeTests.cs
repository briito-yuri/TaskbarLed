using TaskbarLed.Core.Themes;

namespace TaskbarLed.Core.Tests;

public class CustomThemeTests
{
    [Fact]
    public void A_full_theme_is_read_with_every_value()
    {
        var ok = CustomTheme.TryParse("Ocean Blue.json", """
            {
              // Comments and trailing commas are fine.
              "name": "Ocean",
              "background": "#F2061A2B",
              "foreground": "#BFE6FF",
              "accent": "#4FC3F7",
              "glow": "#4FC3F7",
              "glowStrength": 0.4,
              "font": "Cascadia Code, Consolas",
              "feedPrefix": "❯",
              "uppercase": true,
            }
            """, out var theme, out var error);

        Assert.True(ok, error);
        Assert.Equal("custom:ocean-blue", theme!.Id);
        Assert.Equal("Ocean", theme.Name);
        Assert.Equal("#F2061A2B", theme.Colors["Led.BackgroundBrush"]);
        Assert.Equal("#BFE6FF", theme.Colors["Led.ForegroundBrush"]);
        Assert.Equal("#4FC3F7", theme.Colors["Led.AccentBrush"]);
        Assert.Equal("#4FC3F7", theme.GlowColor);
        Assert.Equal(0.4, theme.GlowOpacity);
        Assert.Equal("Cascadia Code, Consolas", theme.FontFamily);
        Assert.Equal("❯", theme.FeedPrefix);
        Assert.True(theme.Uppercase);
    }

    [Fact]
    public void Fields_left_out_keep_the_default_theme()
    {
        Assert.True(CustomTheme.TryParse("mine.json", """{"name":"Mine","foreground":"#FFFFFF"}""", out var theme, out _));

        Assert.Single(theme!.Colors);
        Assert.Null(theme.GlowColor);
        Assert.Null(theme.GlowOpacity);
        Assert.Null(theme.FontFamily);
        Assert.Null(theme.FeedPrefix);
        Assert.False(theme.Uppercase);
    }

    [Theory]
    [InlineData("""{"foreground":"#FFFFFF"}""", "name")]
    [InlineData("""{"name":""}""", "name")]
    [InlineData("""{"name":"A","foreground":"red"}""", "foreground")]
    [InlineData("""{"name":"A","foreground":"#FFF"}""", "foreground")]
    [InlineData("""{"name":"A","background":"#GG0000"}""", "background")]
    [InlineData("""{"name":"A","glowStrength":2}""", "glowStrength")]
    [InlineData("""{"name":"A","feedPrefix":"too long"}""", "feedPrefix")]
    [InlineData("""{"name":"A","uppercase":"yes"}""", "uppercase")]
    [InlineData("""[1,2,3]""", "JSON object")]
    [InlineData("""{"name":"A",""", "JSON")]
    public void Invalid_values_are_refused_with_a_reason(string json, string reasonMentions)
    {
        Assert.False(CustomTheme.TryParse("bad.json", json, out var theme, out var error));
        Assert.Null(theme);
        Assert.Contains(reasonMentions, error, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(@"C:\Windows\Fonts\#Arial")]
    [InlineData(@"\\attacker\share\#Evil")]
    [InlineData("file:///C:/fonts/x.ttf#X")]
    [InlineData("pack://application:,,,/#Font")]
    [InlineData("./fonts/#Mine")]
    public void Fonts_cannot_point_at_files_or_network_shares(string font)
    {
        var json = $$"""{"name":"A","font":{{System.Text.Json.JsonSerializer.Serialize(font)}}}""";

        Assert.False(CustomTheme.TryParse("bad.json", json, out _, out var error));
        Assert.Contains("font", error, StringComparison.Ordinal);
    }

    [Fact]
    public void Unknown_fields_and_xaml_text_are_just_ignored_data()
    {
        var ok = CustomTheme.TryParse("x.json", """
            {"name":"<ObjectDataProvider MethodName=\"Start\"/>","xaml":"<Window/>","foreground":"#FFFFFF"}
            """, out var theme, out _);

        Assert.True(ok);
        Assert.Equal("<ObjectDataProvider MethodName=\"Start\"/>", theme!.Name); // Shown as menu text, never parsed.
        Assert.Single(theme.Colors);
    }

    [Fact]
    public void Oversized_files_are_refused()
    {
        var json = "{\"name\":\"A\",\"pad\":\"" + new string('x', CustomTheme.MaxFileBytes) + "\"}";

        Assert.False(CustomTheme.TryParse("big.json", json, out _, out var error));
        Assert.Contains("16 KB", error, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("../../evil.json", "custom:evil")]
    [InlineData("Meu Tema.json", "custom:meu-tema")]
    public void Ids_come_from_the_file_name_and_stay_simple(string fileName, string expectedId)
    {
        Assert.True(CustomTheme.TryParse(fileName, """{"name":"A"}""", out var theme, out _));
        Assert.Equal(expectedId, theme!.Id);
    }

    [Fact]
    public void Empty_prefix_means_no_glyph()
    {
        Assert.True(CustomTheme.TryParse("a.json", """{"name":"A","feedPrefix":""}""", out var theme, out _));
        Assert.Equal(string.Empty, theme!.FeedPrefix);
    }
}
