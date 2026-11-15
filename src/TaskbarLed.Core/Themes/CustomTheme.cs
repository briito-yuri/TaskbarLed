using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace TaskbarLed.Core.Themes;

/// <summary>
/// A theme the user made as a JSON file. It holds values only: colors, a few numbers, a font name and a
/// short glyph. Nothing in it is ever loaded as XAML or code, so a theme shared on the internet cannot
/// run anything. Fields left out keep the value of the default theme.
/// </summary>
/// <param name="Id">Stable id stored in settings: <c>custom:</c> plus the file name.</param>
/// <param name="Name">Name shown in the menu.</param>
/// <param name="Colors">Theme key (e.g. <c>Led.ForegroundBrush</c>) → color as <c>#RRGGBB</c> or <c>#AARRGGBB</c>.</param>
/// <param name="GlowColor">Color of the text glow, or null to keep the default.</param>
/// <param name="GlowOpacity">Glow strength 0..1, or null to keep the default.</param>
/// <param name="FontFamily">Comma-separated installed font names, or null to keep the default.</param>
/// <param name="FeedPrefix">Glyph before the feed line (empty for none), or null to keep the default.</param>
/// <param name="Uppercase">Show text in capitals.</param>
public sealed record CustomTheme(
    string Id,
    string Name,
    IReadOnlyDictionary<string, string> Colors,
    string? GlowColor,
    double? GlowOpacity,
    string? FontFamily,
    string? FeedPrefix,
    bool Uppercase)
{
    /// <summary>Prefix of custom theme ids, so they can never collide with a built-in theme.</summary>
    public const string IdPrefix = "custom:";

    /// <summary>Files larger than this are refused: a theme is a few hundred bytes.</summary>
    public const int MaxFileBytes = 16 * 1024;

    /// <summary>At most this many custom themes are loaded.</summary>
    public const int MaxThemes = 50;

    private const int MaxNameLength = 40;
    private const int MaxFontLength = 100;
    private const int MaxPrefixTextElements = 2;

    /// <summary>JSON field → theme key it sets. Documented in README ("Make your own theme").</summary>
    public static IReadOnlyDictionary<string, string> ColorFields { get; } = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["background"] = "Led.BackgroundBrush",
        ["border"] = "Led.BorderBrush",
        ["foreground"] = "Led.ForegroundBrush",
        ["dim"] = "Led.DimBrush",
        ["attention"] = "Led.AttentionBrush",
        ["done"] = "Led.DoneBrush",
        ["tabBackground"] = "Led.ChannelOffBrush",
        ["edit"] = "Led.EditBrush",
        ["accent"] = "Led.AccentBrush",
    };

    private static readonly Regex ColorPattern = new("^#([0-9A-Fa-f]{6}|[0-9A-Fa-f]{8})$", RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100));

    // Font names only. No path, URI or '#' fragment: WPF would load a font file from there, even from a network share.
    private static readonly Regex FontPattern = new(@"^[\p{L}\p{N} ,\-]+$", RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100));

    private static readonly Regex FileNamePattern = new(@"[^\p{L}\p{N}\-_]", RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100));

    private static readonly JsonDocumentOptions JsonOptions = new()
    {
        AllowTrailingCommas = true,
        CommentHandling = JsonCommentHandling.Skip,
        MaxDepth = 4,
    };

    /// <summary>Reads one theme file. Never throws: a bad file gives <c>false</c> and a reason for the log.</summary>
    /// <param name="fileName">File name without folder, used for the id (e.g. <c>ocean.json</c>).</param>
    /// <param name="json">File contents.</param>
    public static bool TryParse(string fileName, string json, out CustomTheme? theme, out string? error)
    {
        theme = null;
        error = null;
        ArgumentNullException.ThrowIfNull(fileName);
        ArgumentNullException.ThrowIfNull(json);

        if (json.Length > MaxFileBytes)
        {
            return Fail("file is larger than 16 KB", out error);
        }

        var slug = FileNamePattern.Replace(Path.GetFileNameWithoutExtension(fileName), "-").ToLowerInvariant();
        if (slug.Length == 0)
        {
            return Fail("file name has no usable characters", out error);
        }

        try
        {
            using var document = JsonDocument.Parse(json, JsonOptions);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                return Fail("the file must contain a JSON object", out error);
            }

            var name = String(root, "name")?.Trim();
            if (string.IsNullOrEmpty(name) || name.Length > MaxNameLength || name.Any(char.IsControl))
            {
                return Fail($"\"name\" is required, up to {MaxNameLength} characters", out error);
            }

            var colors = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var (field, key) in ColorFields)
            {
                if (!root.TryGetProperty(field, out var value))
                {
                    continue;
                }

                if (!IsColor(value))
                {
                    return Fail($"\"{field}\" must be a color like \"#1F1E1D\" or \"#F21F1E1D\"", out error);
                }

                colors[key] = value.GetString()!;
            }

            string? glowColor = null;
            if (root.TryGetProperty("glow", out var glow))
            {
                if (!IsColor(glow))
                {
                    return Fail("\"glow\" must be a color like \"#D97757\"", out error);
                }

                glowColor = glow.GetString();
            }

            double? glowOpacity = null;
            if (root.TryGetProperty("glowStrength", out var strength))
            {
                if (strength.ValueKind != JsonValueKind.Number || !strength.TryGetDouble(out var opacity) || opacity is < 0 or > 1)
                {
                    return Fail("\"glowStrength\" must be a number from 0 to 1", out error);
                }

                glowOpacity = opacity;
            }

            var font = String(root, "font")?.Trim();
            if (root.TryGetProperty("font", out _) && (string.IsNullOrEmpty(font) || font.Length > MaxFontLength || !FontPattern.IsMatch(font)))
            {
                return Fail("\"font\" must be installed font names separated by commas, without paths", out error);
            }

            var prefix = String(root, "feedPrefix");
            if (root.TryGetProperty("feedPrefix", out _)
                && (prefix is null || prefix.Any(char.IsControl) || new StringInfo(prefix).LengthInTextElements > MaxPrefixTextElements))
            {
                return Fail($"\"feedPrefix\" must be text of up to {MaxPrefixTextElements} characters (or \"\" for none)", out error);
            }

            var uppercase = false;
            if (root.TryGetProperty("uppercase", out var upper))
            {
                if (upper.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
                {
                    return Fail("\"uppercase\" must be true or false", out error);
                }

                uppercase = upper.GetBoolean();
            }

            theme = new CustomTheme(IdPrefix + slug, name, colors, glowColor, glowOpacity, font, prefix, uppercase);
            return true;
        }
        catch (JsonException ex)
        {
            return Fail($"not valid JSON ({ex.Message})", out error);
        }
    }

    private static bool IsColor(JsonElement value) =>
        value.ValueKind == JsonValueKind.String && ColorPattern.IsMatch(value.GetString()!);

    private static string? String(JsonElement root, string property) =>
        root.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    private static bool Fail(string reason, out string? error)
    {
        error = reason;
        return false;
    }
}
