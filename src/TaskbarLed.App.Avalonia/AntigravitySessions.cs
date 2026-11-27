using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using TaskbarLed.Core.Sessions;

namespace TaskbarLed.App.Avalonia;

/// <summary>Reads activity and parent links from the local CLI and desktop conversation indexes.</summary>
internal static class AntigravitySessions
{
    private const string Query = """
        SELECT c.conversation_id AS id, substr(c.title,1,80) AS title,
               substr(c.agent_name,1,80) AS agent, nullif(c.parent_conversation_id,'') AS parent,
               substr(p.title,1,80) AS parent_title, c.last_modified_time AS modified,
               CASE WHEN c.status IN ('CASCADE_RUN_STATUS_RUNNING', 'CASCADE_RUN_STATUS_BUSY')
                         OR c.not_fully_idle <> 0 THEN 1 ELSE 0 END AS active
        FROM conversation_summaries c
        LEFT JOIN conversation_summaries p ON p.conversation_id = c.parent_conversation_id
        WHERE c.killed = 0 AND
              (c.status IN ('CASCADE_RUN_STATUS_RUNNING', 'CASCADE_RUN_STATUS_BUSY') OR c.not_fully_idle <> 0
               OR (c.status = 'CASCADE_RUN_STATUS_IDLE' AND
                   c.last_modified_time >= strftime('%Y-%m-%d %H:%M:%S', 'now', '-30 minutes')))
        ORDER BY c.last_modified_time DESC, c.conversation_id;
        """;

    public static async Task<Session[]> ScanAsync(string geminiHome, CancellationToken cancellationToken)
    {
        var rows = await Task.WhenAll(
            ReadAsync(Path.Combine(geminiHome, "antigravity-cli", "conversation_summaries.db"), cancellationToken),
            ReadAsync(Path.Combine(geminiHome, "antigravity", "conversation_summaries.db"), cancellationToken),
            ReadAsync(Path.Combine(geminiHome, "antigravity-ide", "conversation_summaries.db"), cancellationToken)).ConfigureAwait(false);
        return [.. rows.SelectMany(row => row).OrderByDescending(s => s.LastUpdated).DistinctBy(s => s.Id)];
    }

    private static async Task<Session[]> ReadAsync(string db, CancellationToken cancellationToken)
    {
        if (!File.Exists(db))
        {
            return [];
        }

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(1));
        try
        {
            var options = new ProcessStartInfo("/usr/bin/sqlite3")
            {
                ArgumentList = { "-batch", "-init", "/dev/null", "-readonly", "-json", db, Query },
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            };
            using var sqlite = Process.Start(options);
            if (sqlite is null)
            {
                return [];
            }

            try
            {
                var read = sqlite.StandardOutput.ReadToEndAsync(timeout.Token);
                var errors = sqlite.StandardError.ReadToEndAsync(timeout.Token);
                await Task.WhenAll(read, errors, sqlite.WaitForExitAsync(timeout.Token)).ConfigureAwait(false);
                return sqlite.ExitCode == 0 ? Parse(await read.ConfigureAwait(false)) : [];
            }
            catch (OperationCanceledException)
            {
                if (!sqlite.HasExited)
                {
                    sqlite.Kill();
                }

                throw;
            }
        }
        catch (Exception ex) when (ex is JsonException or Win32Exception or IOException or InvalidOperationException)
        {
            return [];
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return [];
        }
    }

    internal static Session[] Parse(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        using var data = JsonDocument.Parse(json);
        if (data.RootElement.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var sessions = new List<Session>();
        foreach (var row in data.RootElement.EnumerateArray())
        {
            if (!Guid.TryParse(Text(row, "id"), out var id)
                || !DateTimeOffset.TryParse(Text(row, "modified"), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var modified))
            {
                continue;
            }

            var child = Guid.TryParse(Text(row, "parent"), out var parent);
            var agent = Clean(Text(row, "agent"));
            var title = Clean(Text(row, "title"));
            var parentName = Clean(Text(row, "parent_title"));
            var completed = row.TryGetProperty("active", out var active) && active.ValueKind == JsonValueKind.Number
                && active.TryGetInt32(out var flag) && flag == 0;
            var label = child ? agent ?? title ?? id.ToString("N")[..8] : title ?? agent ?? "Antigravity";
            sessions.Add(Session.Create($"antigravity:{id:D}", "antigravity", modified) with
            {
                Title = label,
                ProjectName = parentName,
                ParentId = child ? $"antigravity:{parent:D}" : null,
                Status = completed ? SessionStatus.Completed : SessionStatus.Running,
                PromptEndedAt = completed ? modified : null,
            });
        }

        return [.. sessions];
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? Text(JsonElement row, string key) =>
        row.ValueKind == JsonValueKind.Object && row.TryGetProperty(key, out var value)
        && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
}
