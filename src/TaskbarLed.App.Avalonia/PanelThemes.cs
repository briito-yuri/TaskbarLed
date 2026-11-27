using System.Globalization;
using System.Xml.Linq;
using Avalonia;
using Avalonia.Media;
using TaskbarLed.App.Avalonia.Presentation;

namespace TaskbarLed.App.Avalonia;

internal static class PanelThemes
{
    internal sealed record Theme(string Id, string Name, bool Uppercase = false);

    private sealed record Palette(string Background, string Border, string Foreground, string Dim,
        string Running, string Waiting, string Done, string Title, string Header, string Rail, string Tree,
        string Child = "Transparent", string Font = "Menlo");

    private static readonly Dictionary<string, Palette> MacPalettes = new(StringComparer.Ordinal)
    {
        ["AmberTree"] = new("#1C1D18", "#424237", "#EEEADB", "#A9AA9C", "#DDB06D", "#ECC788",
            "#9CB494", "#E2B171", "Transparent", "Transparent", "#48483B"),
        ["CompactCharcoal"] = new("#242522", "#42443E", "#D3D4C9", "#A1A397", "#D8B57C", "#E8BD79",
            "#A3BF9D", "#F0EDE1", "#2E302B", "Transparent", "Transparent"),
        ["AmberRail"] = new("#20221F", "#4A4E45", "#DEDFD1", "#A8AD9F", "#E2B076", "#F2CD8F",
            "#AABE9D", "#F2EFDC", "Transparent", "#B88953", "Transparent"),
        ["ClearList"] = new("#F8F8F2", "#DAE1DA", "#222D2D", "#526866", "#176B64", "#865511",
            "#4F6D50", "#222D2D", "Transparent", "Transparent", "Transparent", "#EEEFEB", "Helvetica Neue"),
    };

    public static IReadOnlyList<Theme> All { get; } =
    [
        new("ClaudeCode", "Claude Code"),
        new("ModernTerminal", "Modern Terminal"),
        new("Matrix", "Matrix", true),
        new("Mono", "Mono", true),
        new("Minimal", "Minimal"),
        new("Glass", "Glass"),
        new("Paper", "Paper"),
        new("AmberTree", "Âmbar em árvore"),
        new("CompactCharcoal", "Carvão compacto"),
        new("AmberRail", "Âmbar com régua"),
        new("ClearList", "Lista Clara"),
    ];

    public static Theme Apply(Application app, PanelViewModel model, string themeId)
    {
        var theme = All.FirstOrDefault(t => t.Id == themeId) ?? All[0];
        model.Uppercase = theme.Uppercase;
        if (MacPalettes.TryGetValue(theme.Id, out var palette))
        {
            ApplyPalette(app, palette);
            return theme;
        }

        using var resource = typeof(PanelThemes).Assembly.GetManifestResourceStream($"PanelThemes.{theme.Id}.xaml")!;
        var xml = XDocument.Load(resource);
        var keyName = XName.Get("Key", "http://schemas.microsoft.com/winfx/2006/xaml");
        foreach (var value in xml.Root!.Elements())
        {
            var key = (string?)value.Attribute(keyName);
            if (key is null)
            {
                continue;
            }

            switch (key)
            {
                case "Led.FontFamily":
                    app.Resources[key] = new FontFamily(value.Value + ", Menlo, SF Mono");
                    break;
                case "Led.GlowColor":
                    app.Resources[key] = Color.Parse(value.Value);
                    break;
                case "Led.GlowOpacity":
                    app.Resources[key] = double.Parse(value.Value, CultureInfo.InvariantCulture);
                    break;
                case "Led.FeedPrefix":
                    app.Resources[key] = value.Value;
                    break;
                case "Led.DotGridBrush":
                    app.Resources[key] = BuildDotGrid(value);
                    break;
                default:
                    if (value.Attribute("Color") is { } color)
                    {
                        app.Resources[key] = new SolidColorBrush(Color.Parse(color.Value));
                    }

                    break;
            }
        }

        app.Resources["Led.SessionTitleBrush"] = app.Resources["Led.ForegroundBrush"];
        app.Resources["Led.SessionRunningBrush"] = app.Resources["Led.ForegroundBrush"];
        foreach (var key in new[] { "SessionHeaderBrush", "SessionRailBrush", "SessionTreeBrush", "SessionChildBrush" })
        {
            app.Resources["Led." + key] = Brushes.Transparent;
        }

        return theme;
    }

    private static void ApplyPalette(Application app, Palette colors)
    {
        void Brush(string key, string color) => app.Resources["Led." + key] = new SolidColorBrush(Color.Parse(color));

        Brush("BackgroundBrush", colors.Background);
        Brush("BorderBrush", colors.Border);
        Brush("ForegroundBrush", colors.Foreground);
        Brush("DimBrush", colors.Dim);
        Brush("AttentionBrush", colors.Waiting);
        Brush("DoneBrush", colors.Done);
        Brush("ChannelOffBrush", colors.Header);
        Brush("EditBrush", colors.Running);
        Brush("AccentBrush", colors.Running);
        Brush("SessionTitleBrush", colors.Title);
        Brush("SessionRunningBrush", colors.Running);
        Brush("SessionHeaderBrush", colors.Header);
        Brush("SessionRailBrush", colors.Rail);
        Brush("SessionTreeBrush", colors.Tree);
        Brush("SessionChildBrush", colors.Child);
        app.Resources["Led.DotGridBrush"] = Brushes.Transparent;
        app.Resources["Led.GlowColor"] = Colors.Transparent;
        app.Resources["Led.GlowOpacity"] = 0.0;
        app.Resources["Led.FeedPrefix"] = string.Empty;
        app.Resources["Led.FontFamily"] = new FontFamily(colors.Font);
    }

    private static IBrush BuildDotGrid(XElement value)
    {
        if (value.Name.LocalName != "DrawingBrush")
        {
            return Brushes.Transparent;
        }

        var grid = value.Descendants().First(n => n.Name.LocalName == "GeometryDrawing");
        var color = new SolidColorBrush(Color.Parse(grid.Attribute("Brush")!.Value));
        var group = new DrawingGroup();
        group.Children.Add(new GeometryDrawing { Brush = Brushes.Transparent, Geometry = new RectangleGeometry(new Rect(0, 0, 3, 3)) });
        foreach (var rectangle in grid.Descendants().Where(n => n.Name.LocalName == "RectangleGeometry"))
        {
            group.Children.Add(new GeometryDrawing { Brush = color, Geometry = new RectangleGeometry(Rect.Parse(rectangle.Attribute("Rect")!.Value)) });
        }

        return new DrawingBrush
        {
            Drawing = group,
            DestinationRect = new RelativeRect(0, 0, 3, 3, RelativeUnit.Absolute),
            TileMode = TileMode.Tile,
            Opacity = double.Parse(value.Attribute("Opacity")!.Value, CultureInfo.InvariantCulture),
        };
    }
}
