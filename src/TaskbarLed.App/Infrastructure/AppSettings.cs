using TaskbarLed.Core.Settings;

namespace TaskbarLed.App.Infrastructure;

/// <summary>Everything persisted between runs.</summary>
public sealed record AppSettings
{
    public PanelSettings Panel { get; init; } = new();

    /// <summary>Start with Windows. On by default (requirement RF15); the tray menu turns it off.</summary>
    public bool StartWithWindows { get; init; } = true;

    /// <summary>Null until the user answers the "connect to Claude Code?" prompt.</summary>
    public bool? ClaudeCodeHooksEnabled { get; init; }

    /// <summary>The one-time "right-click to edit" tip was already shown.</summary>
    public bool RightClickHintShown { get; init; }

    /// <summary>UI language: <c>auto</c> (follow Windows), <c>pt</c> or <c>en</c>.</summary>
    public string Language { get; init; } = "auto";
}
