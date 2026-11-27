using System.ComponentModel;
using System.Diagnostics;
using System.Text.Json;

namespace TaskbarLed.Platform.Mac;

/// <summary>Identifies local agent processes by executable name, without reading their arguments.</summary>
public static class AgentProcesses
{
    private static readonly Dictionary<string, string> BuiltIn = new(StringComparer.OrdinalIgnoreCase)
    {
        ["agy"] = "Antigravity",
        ["antigravity"] = "Antigravity",
        ["octo"] = "Octo",
        ["aider"] = "Aider",
        ["amp"] = "Amp",
        ["claude"] = "Claude",
        ["claude-code"] = "Claude",
        ["cline"] = "Cline",
        ["codex"] = "Codex",
        ["copilot"] = "GitHub Copilot",
        ["cursor"] = "Cursor",
        ["cursor-agent"] = "Cursor",
        ["gemini"] = "Gemini CLI",
        ["goose"] = "Goose",
        ["kimi"] = "Kimi Code",
        ["kimi-cli"] = "Kimi Code",
        ["qwen"] = "Qwen Code",
        ["qwen-code"] = "Qwen Code",
        ["kiro-cli"] = "Kiro",
        ["lm studio"] = "LM Studio",
        ["lms"] = "LM Studio",
        ["ollama"] = "Ollama",
        ["opencode"] = "OpenCode",
        ["windsurf"] = "Windsurf",
    };

    public static async Task<string[]?> ScanAsync(string rulesFile, CancellationToken cancellationToken)
    {
        try
        {
            var options = new ProcessStartInfo("/bin/ps")
            {
                ArgumentList = { "-U", Environment.UserName, "-o", "comm=" },
                UseShellExecute = false,
                RedirectStandardOutput = true,
            };
            using var ps = Process.Start(options);
            if (ps is null)
            {
                return null;
            }

            var commands = await ps.StandardOutput.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
            await ps.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
            return ps.ExitCode == 0 ? Match(commands, LoadRules(rulesFile)) : null;
        }
        catch (Exception ex) when (ex is Win32Exception or IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    internal static string[] Match(string commands, IReadOnlyDictionary<string, string>? custom = null)
    {
        var found = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var line in commands.Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            var executable = Path.GetFileName(line);
            if ((custom is not null && custom.TryGetValue(executable, out var name))
                || BuiltIn.TryGetValue(executable, out name))
            {
                found.Add(name);
            }
        }

        return [.. found.Order(StringComparer.OrdinalIgnoreCase)];
    }

    private static Dictionary<string, string>? LoadRules(string file)
    {
        try
        {
            var info = new FileInfo(file);
            if (!info.Exists || info.Length > 16384)
            {
                return null;
            }

            var entries = JsonSerializer.Deserialize<Dictionary<string, string?>>(File.ReadAllText(file));
            if (entries is null)
            {
                return null;
            }

            var rules = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var (executable, title) in entries)
            {
                if (executable.Length is > 0 and <= 120 && executable == Path.GetFileName(executable)
                    && title is { Length: > 0 and <= 80 } && !string.IsNullOrWhiteSpace(title))
                {
                    rules[executable] = title.Trim();
                }
            }

            return rules;
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }
}
