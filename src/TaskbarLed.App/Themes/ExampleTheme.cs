namespace TaskbarLed.App.Themes;

/// <summary>
/// The file written into an empty themes folder, so users start from something that works.
/// Same content as docs/themes/ocean.json; keep them in sync.
/// </summary>
internal static class ExampleTheme
{
    public const string FileName = "ocean.json";

    public const string Json = """
        {
          // Your own TaskbarLed theme. Copy this file, rename it, change the values,
          // then pick it in the panel menu: Theme. Every field except "name" is optional;
          // anything left out keeps the Claude Code theme. Colors: "#RRGGBB" or "#AARRGGBB".
          "name": "Ocean",
          "background": "#F2071A2B",
          "border": "#16405E",
          "foreground": "#D6F1FF",
          "dim": "#6B93AD",
          "attention": "#FFC857",
          "done": "#5FE3B1",
          "tabBackground": "#0C2A40",
          "edit": "#4FC3F7",
          "accent": "#4FC3F7",
          "glow": "#4FC3F7",
          "glowStrength": 0.3,
          "font": "Cascadia Code, Consolas",
          "feedPrefix": "❯",
          "uppercase": false
        }
        """;
}
