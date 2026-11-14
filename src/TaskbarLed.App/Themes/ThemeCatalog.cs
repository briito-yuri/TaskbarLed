using System.Windows;

namespace TaskbarLed.App.Themes;

/// <summary>A selectable look for the panel and its overlays.</summary>
/// <param name="Id">Name stored in settings and the XAML file name (Themes/{Id}.xaml).</param>
/// <param name="DisplayName">Name shown in the menu.</param>
/// <param name="Uppercase">Show text in capitals (LED and terminal looks) or in normal case (modern looks).</param>
public sealed record ThemeInfo(string Id, string DisplayName, bool Uppercase);

/// <summary>
/// Built-in themes. To add one: create Themes/YourTheme.xaml with every <c>Led.*</c> key
/// (copy Matrix.xaml), then add a line here. Switching is live: all colors use DynamicResource.
/// </summary>
public static class ThemeCatalog
{
    public const string DefaultId = "Matrix";

    public static IReadOnlyList<ThemeInfo> All { get; } =
    [
        new("Matrix", "Matrix", Uppercase: true),
        new("Mono", "Mono", Uppercase: true),
        new("ModernTerminal", "Modern Terminal", Uppercase: false),
        new("ClaudeCode", "Claude Code", Uppercase: false),
        new("Minimal", "Minimal", Uppercase: false),
        new("Glass", "Glass", Uppercase: false),
        new("Paper", "Paper", Uppercase: false),
    ];

    // Ids that were renamed, so a saved choice keeps working.
    private static readonly Dictionary<string, string> RenamedIds = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Terminal"] = "ModernTerminal",
        ["Clay"] = "ClaudeCode",
    };

    /// <summary>The known theme matching <paramref name="id"/> (or its former name), or the default.</summary>
    public static ThemeInfo Resolve(string? id)
    {
        if (id is not null && RenamedIds.TryGetValue(id, out var renamed))
        {
            id = renamed;
        }

        return All.FirstOrDefault(t => string.Equals(t.Id, id, StringComparison.OrdinalIgnoreCase)) ?? All[0];
    }

    /// <summary>Swaps the theme dictionary in the application resources.</summary>
    public static void Apply(ResourceDictionary resources, string? id)
    {
        ArgumentNullException.ThrowIfNull(resources);

        var theme = new ResourceDictionary { Source = Uri(Resolve(id).Id) };
        var current = resources.MergedDictionaries
            .FirstOrDefault(d => d.Source?.OriginalString.Contains("Themes/", StringComparison.OrdinalIgnoreCase) == true);

        if (current is null)
        {
            resources.MergedDictionaries.Add(theme);
        }
        else
        {
            resources.MergedDictionaries[resources.MergedDictionaries.IndexOf(current)] = theme;
        }
    }

    /// <summary>Pack URI of a theme dictionary.</summary>
    public static Uri Uri(string id) => new($"pack://application:,,,/TaskbarLed;component/Themes/{id}.xaml", UriKind.Absolute);
}
