using System.Globalization;
using System.Xml.Linq;
using Avalonia;
using Avalonia.Media;
using TaskbarLed.App.Avalonia.Presentation;

namespace TaskbarLed.App.Avalonia;

internal static class PanelThemes
{
    internal sealed record Theme(string Id, string Name, bool Uppercase = false);

    public static IReadOnlyList<Theme> All { get; } =
    [
        new("ClaudeCode", "Claude Code"),
        new("ModernTerminal", "Modern Terminal"),
        new("Matrix", "Matrix", true),
        new("Mono", "Mono", true),
        new("Minimal", "Minimal"),
        new("Glass", "Glass"),
        new("Paper", "Paper"),
    ];

    public static Theme Apply(Application app, PanelViewModel model, string themeId)
    {
        var theme = All.FirstOrDefault(t => t.Id == themeId) ?? All[0];
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

        model.Uppercase = theme.Uppercase;
        return theme;
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
