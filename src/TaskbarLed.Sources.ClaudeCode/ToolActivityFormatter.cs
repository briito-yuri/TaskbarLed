using System.Globalization;
using System.Text.Json;

namespace TaskbarLed.Sources.ClaudeCode;

/// <summary>Turns a Claude Code tool call into a short, human phrase ("Editing scraper.py").</summary>
public sealed class ToolActivityFormatter
{
    private const int MaxSubjectLength = 60;
    private readonly ActivityStrings _strings;
    private readonly CultureInfo _culture;

    public ToolActivityFormatter(ActivityStrings strings, CultureInfo culture)
    {
        _strings = strings ?? throw new ArgumentNullException(nameof(strings));
        _culture = culture ?? throw new ArgumentNullException(nameof(culture));
    }

    /// <summary>Tools that block on the user rather than doing work.</summary>
    public static bool IsUserPrompt(string toolName) => toolName is "AskUserQuestion" or "ExitPlanMode";

    public string Describe(string toolName, JsonElement input)
    {
        ArgumentNullException.ThrowIfNull(toolName);

        return toolName switch
        {
            "Edit" or "MultiEdit" or "NotebookEdit" => Format(_strings.Editing, FileName(input, "file_path", "notebook_path")),
            "Write" => Format(_strings.Writing, FileName(input, "file_path")),
            "Read" => Format(_strings.Reading, FileName(input, "file_path")),
            "Bash" or "PowerShell" => Format(_strings.Running, Text(input, "description") ?? FirstLine(Text(input, "command"))),
            "Grep" or "Glob" => Format(_strings.Searching, Text(input, "pattern")),
            "WebFetch" => Format(_strings.Browsing, Host(Text(input, "url"))),
            "WebSearch" => Format(_strings.WebSearching, Text(input, "query")),
            "Task" or "Agent" => Format(_strings.Delegating, Text(input, "description")),
            "TodoWrite" => _strings.Planning,
            "AskUserQuestion" => _strings.AskingYou,
            "ExitPlanMode" => _strings.ApprovingPlan,
            _ when toolName.StartsWith("mcp__", StringComparison.Ordinal) => Format(_strings.Using, McpToolName(toolName)),
            _ => Format(_strings.Using, toolName),
        };
    }

    private string Format(string template, string? subject) =>
        string.Format(_culture, template, Shorten(subject ?? "…")).Trim();

    private static string? Text(JsonElement input, string property) =>
        input.ValueKind == JsonValueKind.Object
        && input.TryGetProperty(property, out var value)
        && value.ValueKind == JsonValueKind.String
        && value.GetString() is { Length: > 0 } text
            ? text
            : null;

    private static string? FileName(JsonElement input, params string[] properties)
    {
        foreach (var property in properties)
        {
            if (Text(input, property) is { } path)
            {
                return PathText.LastSegment(path);
            }
        }

        return null;
    }

    private static string? FirstLine(string? text) =>
        text?.Split('\n', 2, StringSplitOptions.TrimEntries)[0];

    private static string? Host(string? url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri) ? uri.Host : url;

    // mcp__server__tool → "server tool", dropping opaque ids some hosts put in the server segment.
    private static string McpToolName(string toolName)
    {
        var parts = toolName.Split("__", StringSplitOptions.RemoveEmptyEntries);
        return parts.Length >= 3 ? parts[^1].Replace('_', ' ') : toolName;
    }

    private static string Shorten(string text)
    {
        var singleLine = text.ReplaceLineEndings(" ").Trim();
        return singleLine.Length <= MaxSubjectLength ? singleLine : string.Concat(singleLine.AsSpan(0, MaxSubjectLength - 1), "…");
    }
}
