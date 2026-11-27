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
               substr(p.title,1,80) AS parent_title, c.last_modified_time AS modified, c.status AS status
        FROM conversation_summaries c
        LEFT JOIN conversation_summaries p ON p.conversation_id = c.parent_conversation_id
        WHERE c.killed = 0 AND
              (c.status IN ('CASCADE_RUN_STATUS_RUNNING', 'CASCADE_RUN_STATUS_BUSY')
               OR (c.status = 'CASCADE_RUN_STATUS_IDLE' AND
                   (c.last_modified_time >= strftime('%Y-%m-%d %H:%M:%S', 'now', '-30 minutes')
                    OR (c.parent_conversation_id <> '' AND c.last_modified_time < '0002'
                        AND p.last_modified_time >= strftime('%Y-%m-%d %H:%M:%S', 'now', '-30 minutes'))))
               OR c.conversation_id IN (SELECT parent_conversation_id FROM conversation_summaries
                   WHERE killed = 0 AND parent_conversation_id <> ''
                     AND status IN ('CASCADE_RUN_STATUS_RUNNING', 'CASCADE_RUN_STATUS_BUSY')))
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
            var (exitCode, json) = await ReadIndexAsync(db, timeout.Token).ConfigureAwait(false);
            if (exitCode == 14 && !File.Exists(db + "-wal"))
            {
                // Apple's SQLite opens a WAL database read-only only when the sidecars already exist.
                // With no WAL the checkpointed main file can be read without creating either sidecar.
                (exitCode, json) = await ReadIndexAsync(new Uri(db).AbsoluteUri + "?immutable=1",
                    timeout.Token).ConfigureAwait(false);
            }

            if (exitCode != 0)
            {
                return [];
            }

            var sessions = Parse(json);
            var stateDirectory = Path.GetDirectoryName(db)!;
            var presence = Path.Combine(stateDirectory, "presence");
            var activeStreams = sessions.Any(session => session.ParentId is not null && session.Status == SessionStatus.Idle)
                ? AntigravityStreams.Read(Path.Combine(stateDirectory, "log")) : [];
            var attached = sessions.Where(session => session.ParentId is null && session.ConversationId.HasValue)
                .Where(session => AntigravityPresence.IsAttached(Path.Combine(presence, $"{session.ConversationId:D}.lock")))
                .Select(session => session.Id).ToHashSet(StringComparer.Ordinal);
            for (var i = 0; i < sessions.Length; i++)
            {
                var worker = sessions[i];
                if (worker is { Status: SessionStatus.Idle, ConversationId: { } id, ParentId: { } parent }
                    && activeStreams.Contains(id) && attached.Contains(parent))
                {
                    sessions[i] = worker with { Status = SessionStatus.Running };
                }
            }

            var activeParents = sessions.Where(s => s.Status == SessionStatus.Running && s.ParentId is not null)
                .Select(s => s.ParentId!).ToHashSet(StringComparer.Ordinal);
            var cutoff = DateTimeOffset.UtcNow.AddSeconds(-20);
            for (var i = 0; i < sessions.Length; i++)
            {
                var session = sessions[i];
                if (session is { Status: SessionStatus.Running, ParentId: null, ConversationId: { } id }
                    && session.LastUpdated < cutoff && !activeParents.Contains(session.Id)
                    && AntigravityPresence.WasReleased(Path.Combine(presence, $"{id:D}.lock")))
                {
                    sessions[i] = session with { Status = SessionStatus.Idle };
                }
            }

            return sessions;
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

    private static async Task<(int ExitCode, string Json)> ReadIndexAsync(string db, CancellationToken cancellationToken)
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
            return (-1, string.Empty);
        }

        try
        {
            var read = sqlite.StandardOutput.ReadToEndAsync(cancellationToken);
            var errors = sqlite.StandardError.ReadToEndAsync(cancellationToken);
            await Task.WhenAll(read, errors, sqlite.WaitForExitAsync(cancellationToken)).ConfigureAwait(false);
            return (sqlite.ExitCode, await read.ConfigureAwait(false));
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
            if (!child && modified == DateTimeOffset.MinValue)
            {
                continue;
            }

            var agent = Clean(Text(row, "agent"));
            var title = Clean(Text(row, "title"));
            var parentName = Clean(Text(row, "parent_title"));
            var status = Text(row, "status") switch
            {
                "CASCADE_RUN_STATUS_RUNNING" or "CASCADE_RUN_STATUS_BUSY" => SessionStatus.Running,
                "CASCADE_RUN_STATUS_IDLE" => SessionStatus.Idle,
                _ => (SessionStatus?)null,
            };
            if (status is null)
            {
                continue;
            }

            var label = child ? agent ?? title ?? id.ToString("N")[..8] : title ?? agent ?? "Antigravity";
            sessions.Add(Session.Create($"antigravity:{id:D}", "antigravity", modified) with
            {
                Title = label,
                ProjectName = parentName,
                ConversationId = id,
                ParentId = child ? $"antigravity:{parent:D}" : null,
                Status = modified == DateTimeOffset.MinValue ? SessionStatus.Idle : status.Value,
            });
        }

        return [.. sessions];
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? Text(JsonElement row, string key) =>
        row.ValueKind == JsonValueKind.Object && row.TryGetProperty(key, out var value)
        && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
}
