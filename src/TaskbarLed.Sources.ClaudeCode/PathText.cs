namespace TaskbarLed.Sources.ClaudeCode;

/// <summary>
/// Display helpers for paths found in hook payloads. Splits on both <c>/</c> and <c>\</c> so the result
/// does not depend on the OS the panel runs on (<see cref="Path.GetFileName(string)"/> ignores <c>\</c> on macOS).
/// </summary>
internal static class PathText
{
    private static readonly char[] Separators = ['/', '\\'];

    /// <summary>Last segment of <paramref name="path"/> (file or folder name); empty for a bare root.</summary>
    public static string LastSegment(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        var trimmed = path.TrimEnd(Separators);
        return trimmed[(trimmed.LastIndexOfAny(Separators) + 1)..];
    }
}
