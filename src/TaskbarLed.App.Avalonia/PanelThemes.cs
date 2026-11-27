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
        string Child = "Transparent", string Font = "Menlo", string StatusBackground = "Transparent",
        string Divider = "Transparent", double Radius = 4, double FrameWidth = 1, string? HeaderStatus = null, string Node = "Transparent", Thickness? Padding = null);

    private static readonly Thickness DefaultPadding = new(5, 2, 7, 1);

    private static readonly Dictionary<string, Palette> MacPalettes = new(StringComparer.Ordinal)
    {
        ["LiquidGlass"] = new("#ED28323E", "#B8E5EDFF", "#F7FAFF", "#DAE0EC", "#F5FBFF", "#FFF2C6",
            "#FFFFFF", "#FFFFFF", "#39000000", "Transparent", "#70DEE8F8", Child: "#35000000",
            Font: "Helvetica Neue", Divider: "#25E5EDFF", Radius: 16, Padding: new Thickness(8, 3)),
        ["Matrix"] = new("#07130C", "#30523A", "#C2EDCE", "#A1BAA7", "#95E9A6", "#FFD38D",
            "#ACDFC0", "#DBF6DF", "#102519", "Transparent", "#41644B", Divider: "#233B2B"),
        ["AmberTree"] = new("#1C1D18", "#424237", "#EEEADB", "#A9AA9C", "#DDB06D", "#ECC788",
            "#9CB494", "#E2B171", "Transparent", "Transparent", "#48483B", Radius: 6),
        ["CompactCharcoal"] = new("#242522", "#42443E", "#D3D4C9", "#A1A397", "#D8B57C", "#E8BD79",
            "#A3BF9D", "#F0EDE1", "#2E302B", "Transparent", "Transparent", Radius: 5),
        ["AmberRail"] = new("#20221F", "#4A4E45", "#DEDFD1", "#A8AD9F", "#E2B076", "#F2CD8F",
            "#AABE9D", "#F2EFDC", "Transparent", "#B88953", "Transparent", Divider: "#30342E", Radius: 7),
        ["ClearList"] = new("#F8F8F2", "#DAE1DA", "#222D2D", "#526866", "#176B64", "#865511",
            "#4F6D50", "#222D2D", "Transparent", "Transparent", "Transparent", "#EEEFEB", "Helvetica Neue", Radius: 12),
        ["EditorialIndex"] = new("#FFFDF7", "#D8D0C5", "#302823", "#65554F", "#782E29", "#8A461E",
            "#526952", "#36251F", "Transparent", "#9A332C", "Transparent", Font: "Georgia",
            Divider: "#E7DFD4", Radius: 0),
        ["CobaltWindow"] = new("#DDE5F2", "#101F58", "#182848", "#3D507F", "#2D46A8", "#81451E",
            "#285E54", "#FFFDF7", "#253E9D", "Transparent", "Transparent", "#F1F4FC",
            "Helvetica Neue", Divider: "#CBD6E8", Radius: 2, HeaderStatus: "#E8F0FF"),
        ["VioletPhosphor"] = new("#161522", "#423A56", "#DDD4EF", "#BEAEDA", "#C9B7FC", "#FFB9C5",
            "#B9E0D4", "#EBE5FA", "Transparent", "Transparent", "Transparent", Font: "Helvetica Neue",
            StatusBackground: "#272339", Divider: "#41384E", Radius: 10),
        ["LineMap"] = new("#FBFCF9", "#D3D8CE", "#313B34", "#536860", "#146B74", "#A74232",
            "#416A51", "#232924", "Transparent", "#146B74", "#146B74", Font: "Helvetica Neue", Radius: 8, Node: "#146B74"),
        ["OliveLcd"] = new("#A7B88C", "#292D27", "#243321", "#3E5037", "#243321", "#443E24",
            "#2A4B2C", "#243321", "Transparent", "Transparent", "Transparent", Divider: "#738464",
            Radius: 13, FrameWidth: 9, Padding: new Thickness(7, 6)),
    };

    public static IReadOnlyList<Theme> All { get; } =
    [
        new("ClaudeCode", App.DisplayName),
        new("ModernTerminal", "Modern Terminal"),
        new("Matrix", "Matrix"),
        new("Mono", "Mono", true),
        new("Minimal", "Minimal"),
        new("Glass", "Glass"),
        new("LiquidGlass", "Liquid Glass"),
        new("Paper", "Paper"),
        new("AmberTree", "Âmbar em árvore"),
        new("CompactCharcoal", "Carvão compacto"),
        new("AmberRail", "Âmbar com régua"),
        new("ClearList", "Lista Clara"),
        new("EditorialIndex", "Índice Editorial"),
        new("CobaltWindow", "Janela Cobalto"),
        new("VioletPhosphor", "Fósforo Violeta"),
        new("LineMap", "Mapa de Linhas"),
        new("OliveLcd", "LCD Oliva", Uppercase: true),
    ];

    public static Theme Apply(Application app, PanelViewModel model, string themeId)
    {
        var theme = All.FirstOrDefault(t => t.Id == themeId) ?? All[0];
        model.Uppercase = theme.Uppercase;
        model.ThemeId = theme.Id;
        var glass = theme.Id == "LiquidGlass";
        app.Resources["Led.CompletionBrush"] = new SolidColorBrush(Color.Parse(glass ? "#A8FFFFFF" : "#BFE6C8"));
        app.Resources["Led.CompletionTint"] = new SolidColorBrush(Color.Parse(glass ? "#58FFFFFF" : "#55BFE6C8"));
        app.Resources["Led.BrandBackdropBrush"] = new SolidColorBrush(Color.Parse(glass ? "#60000000" : "Transparent"));
        if (MacPalettes.TryGetValue(theme.Id, out var palette))
        {
            ApplyPalette(app, palette);
            if (glass)
            {
                app.Resources["Led.PanelSurfaceBrush"] = new LinearGradientBrush
                {
                    StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
                    EndPoint = new RelativePoint(1, 1, RelativeUnit.Relative),
                    GradientStops =
                    {
                        new GradientStop(Color.Parse("#89000000"), 0),
                        new GradientStop(Color.Parse("#74000000"), 1),
                    },
                };
            }
            ApplyMenuPalette(app);
            return theme;
        }

        foreach (var value in SharedThemeResources(theme.Id))
        {
            var key = (string?)value.Attribute(ResourceKey);
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

        app.Resources["Led.PanelSurfaceBrush"] = app.Resources["Led.BackgroundBrush"];
        app.Resources["Led.SessionTitleBrush"] = app.Resources["Led.ForegroundBrush"];
        app.Resources["Led.SessionRunningBrush"] = app.Resources["Led.ForegroundBrush"];
        app.Resources["Led.SessionHeaderStatusBrush"] = app.Resources["Led.ForegroundBrush"];
        foreach (var key in new[] { "SessionHeaderBrush", "SessionRailBrush", "SessionTreeBrush", "SessionChildBrush", "SessionStatusBrush", "SessionDividerBrush", "SessionNodeBrush" })
        {
            app.Resources["Led." + key] = Brushes.Transparent;
        }

        app.Resources["Led.PanelRadius"] = new CornerRadius(4);
        app.Resources["Led.FrameThickness"] = new Thickness(1);
        app.Resources["Led.PanelPadding"] = DefaultPadding;
        ApplyMenuPalette(app);
        return theme;
    }

    private static readonly XName ResourceKey = XName.Get("Key", "http://schemas.microsoft.com/winfx/2006/xaml");

    private static XElement[] SharedThemeResources(string id)
    {
        using var resource = typeof(PanelThemes).Assembly.GetManifestResourceStream($"PanelThemes.{id}.xaml")!;
        return [.. XDocument.Load(resource).Root!.Elements()];
    }

    internal static (Color Background, Color Text) PreviewColors(Theme theme)
    {
        if (MacPalettes.TryGetValue(theme.Id, out var palette))
        {
            return (Color.Parse(palette.Background), Color.Parse(palette.Foreground));
        }

        var resources = SharedThemeResources(theme.Id);
        Color Read(string key) => Color.Parse(resources.First(element => (string?)element.Attribute(ResourceKey) == key)
            .Attribute("Color")!.Value);
        return (Read("Led.BackgroundBrush"), Read("Led.ForegroundBrush"));
    }

    private static void ApplyPalette(Application app, Palette colors)
    {
        void Brush(string key, string color) => app.Resources["Led." + key] = new SolidColorBrush(Color.Parse(color));

        Brush("BackgroundBrush", colors.Background);
        app.Resources["Led.PanelSurfaceBrush"] = app.Resources["Led.BackgroundBrush"];
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
        Brush("SessionHeaderStatusBrush", colors.HeaderStatus ?? colors.Running);
        Brush("SessionNodeBrush", colors.Node);
        Brush("SessionHeaderBrush", colors.Header);
        Brush("SessionRailBrush", colors.Rail);
        Brush("SessionTreeBrush", colors.Tree);
        Brush("SessionChildBrush", colors.Child);
        Brush("SessionStatusBrush", colors.StatusBackground);
        Brush("SessionDividerBrush", colors.Divider);
        app.Resources["Led.PanelRadius"] = new CornerRadius(colors.Radius);
        app.Resources["Led.FrameThickness"] = new Thickness(colors.FrameWidth);
        app.Resources["Led.PanelPadding"] = colors.Padding ?? DefaultPadding;
        app.Resources["Led.DotGridBrush"] = Brushes.Transparent;
        app.Resources["Led.GlowColor"] = Colors.Transparent;
        app.Resources["Led.GlowOpacity"] = 0.0;
        app.Resources["Led.FeedPrefix"] = string.Empty;
        app.Resources["Led.FontFamily"] = new FontFamily(colors.Font);
    }

    private static void ApplyMenuPalette(Application app)
    {
        Color Read(string key) => ((ISolidColorBrush)app.Resources["Led." + key]!).Color;
        void Brush(string key, Color color) => app.Resources[key] = new SolidColorBrush(color);

        var translucent = Read("BackgroundBrush");
        var background = Blend(Color.Parse("#192321"), translucent, translucent.A / 255.0);
        var foreground = Read("ForegroundBrush");
        var accent = Read("EditBrush");
        if (Contrast(accent, background) < 4.5)
        {
            accent = foreground;
        }

        var border = Blend(background, foreground, 0.25);
        var highlight = Blend(background, accent, 0.15);
        var pressed = Blend(background, accent, 0.23);
        if (Contrast(foreground, pressed) < 4.5)
        {
            foreground = Contrast(Colors.Black, background) > Contrast(Colors.White, background) ? Colors.Black : Colors.White;
        }

        Brush("MenuFlyoutPresenterBackground", background);
        Brush("MenuFlyoutPresenterBorderBrush", border);
        Brush("MenuFlyoutItemForeground", foreground);
        Brush("MenuFlyoutItemForegroundPointerOver", foreground);
        Brush("MenuFlyoutItemForegroundPressed", foreground);
        Brush("MenuFlyoutItemForegroundDisabled", accent);
        Brush("MenuFlyoutSubItemChevron", foreground);
        Brush("MenuFlyoutItemBackgroundPointerOver", highlight);
        Brush("MenuFlyoutItemBackgroundPressed", pressed);
        Brush("Led.MenuQuitBrush", highlight);
        Brush("Led.MenuAccentBrush", accent);
    }

    private static Color Blend(Color baseColor, Color over, double opacity) => Color.FromRgb(
        (byte)Math.Round(baseColor.R + (over.R - baseColor.R) * opacity),
        (byte)Math.Round(baseColor.G + (over.G - baseColor.G) * opacity),
        (byte)Math.Round(baseColor.B + (over.B - baseColor.B) * opacity));

    private static double Contrast(Color a, Color b)
    {
        static double Linear(byte channel)
        {
            var value = channel / 255.0;
            return value <= 0.04045 ? value / 12.92 : Math.Pow((value + 0.055) / 1.055, 2.4);
        }

        static double Luminance(Color color) => 0.2126 * Linear(color.R) + 0.7152 * Linear(color.G) + 0.0722 * Linear(color.B);
        var first = Luminance(a);
        var second = Luminance(b);
        return (Math.Max(first, second) + 0.05) / (Math.Min(first, second) + 0.05);
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
