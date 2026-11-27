using System.Globalization;
using System.Text.Json;
using TaskbarLed.Core.Sessions;

namespace TaskbarLed.App.Avalonia;

/// <summary>Reads turn boundaries and parent links from the local Codex session logs.</summary>
internal sealed class CodexSessions
{
    private readonly Dictionary<string, (DateTime Modified, long Length, Session? Session)> _cache = new(StringComparer.Ordinal);

    public Session[] Scan(string home, DateTimeOffset now)
    {
        try
        {
            var root = Path.Combine(home, "sessions");
            var files = Enumerable.Range(0, 3)
                .Select(days => Path.Combine(root, now.ToLocalTime().AddDays(-days).ToString("yyyy/MM/dd", CultureInfo.InvariantCulture)))
                .Where(Directory.Exists)
                .SelectMany(dir => new DirectoryInfo(dir).EnumerateFiles("rollout-*.jsonl"))
                .Where(file => file.LinkTarget is null && file.LastWriteTimeUtc >= now.UtcDateTime.AddHours(-12))
                .OrderByDescending(file => file.LastWriteTimeUtc).Take(128).ToArray();
            var paths = files.Select(f => f.FullName).ToHashSet(StringComparer.Ordinal);
            foreach (var old in _cache.Keys.Where(key => !paths.Contains(key)).ToArray())
            {
                _cache.Remove(old);
            }

            return [.. files.Select(Read).OfType<Session>()
                .DistinctBy(session => session.Id)];
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return [];
        }
    }

    private Session? Read(FileInfo file)
    {
        var modified = file.LastWriteTimeUtc;
        var length = file.Length;
        if (_cache.TryGetValue(file.FullName, out var cached) && cached.Modified == modified && cached.Length == length)
        {
            return cached.Session;
        }

        try
        {
            using var input = new FileStream(file.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            using var reader = new StreamReader(input);
            // Read only the first record and a bounded tail. A concurrent writer might leave a partial last line.
            var head = new char[32768];
            var count = reader.ReadBlock(head);
            var first = new string(head, 0, count).Split('\n', 2)[0];
            reader.DiscardBufferedData();
            var offset = Math.Max(0, length - 192 * 1024);
            input.Seek(offset, SeekOrigin.Begin);
            if (offset > 0)
            {
                reader.ReadLine();
            }

            var tail = new char[192 * 1024];
            var tailCount = reader.ReadBlock(tail);
            var session = Parse(first, new string(tail, 0, tailCount), modified, out var sawBoundary);
            if (!sawBoundary && offset > 0)
            {
                // A long turn can push its start out of the tail with tool output alone.
                reader.DiscardBufferedData();
                input.Seek(0, SeekOrigin.Begin);
                session = ParseEvents(first, ReadEventLines(reader), modified, out _);
            }
            _cache[file.FullName] = (modified, length, session);
            return session;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            return cached.Session;
        }
    }

    internal static Session? Parse(string header, string tail, DateTimeOffset modified) =>
        Parse(header, tail, modified, out _);

    private static Session? Parse(string header, string tail, DateTimeOffset modified, out bool sawBoundary) =>
        ParseEvents(header, tail.Split('\n', StringSplitOptions.RemoveEmptyEntries), modified, out sawBoundary);

    private static IEnumerable<string> ReadEventLines(StreamReader reader)
    {
        while (reader.ReadLine() is { } line)
        {
            if (line.Contains("\"event_msg\"", StringComparison.Ordinal))
            {
                yield return line;
            }
        }
    }

    private static Session? ParseEvents(string header, IEnumerable<string> lines, DateTimeOffset modified, out bool sawBoundary)
    {
        sawBoundary = false;
        using var meta = JsonDocument.Parse(header);
        if (Text(meta.RootElement, "type") != "session_meta" || !Guid.TryParse(Text(Field(meta.RootElement, "payload"), "id"), out var id))
        {
            return null;
        }

        var details = Field(meta.RootElement, "payload");
        var spawn = Field(Field(Field(details, "source"), "subagent"), "thread_spawn");
        var child = Guid.TryParse(Text(spawn, "parent_thread_id"), out var parent);
        var name = Text(spawn, "agent_nickname") ?? Text(spawn, "agent_role");
        var project = Path.GetFileName(Text(details, "cwd"));
        var state = (SessionStatus?)null;
        DateTimeOffset? started = null;
        var updated = modified;
        foreach (var line in lines)
        {
            if (!line.Contains("\"event_msg\"", StringComparison.Ordinal))
            {
                continue;
            }

            try
            {
                using var doc = JsonDocument.Parse(line);
                if (Text(doc.RootElement, "type") != "event_msg")
                {
                    continue;
                }

                var evt = Text(Field(doc.RootElement, "payload"), "type");
                var next = evt switch
                {
                    "task_started" or "turn_started" => SessionStatus.Running,
                    "task_complete" or "turn_complete" => SessionStatus.Completed,
                    "turn_aborted" or "shutdown_complete" => SessionStatus.Idle,
                    _ => (SessionStatus?)null,
                };
                if (next is null)
                {
                    continue;
                }

                updated = DateTimeOffset.TryParse(Text(doc.RootElement, "timestamp"), CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal, out var at) ? at : modified;
                state = next;
                if (state == SessionStatus.Running)
                {
                    started = updated;
                }
            }
            catch (JsonException)
            {
                // Ignore a partial record being appended.
            }
        }

        sawBoundary = state.HasValue;
        if (state is null or SessionStatus.Idle)
        {
            return null;
        }

        return Session.Create($"codex:{id:D}", "codex", updated) with
        {
            Status = state.Value,
            ConversationId = id,
            Title = child ? name ?? $"Codex {id.ToString("N")[..8]}" : "Codex",
            ProjectName = project,
            ParentId = child ? $"codex:{parent:D}" : null,
            PromptStartedAt = started,
            PromptEndedAt = state == SessionStatus.Completed ? updated : null,
        };
    }

    private static JsonElement Field(JsonElement element, string key) =>
        element.ValueKind == JsonValueKind.Object && element.TryGetProperty(key, out var value) ? value : default;

    private static string? Text(JsonElement element, string key) =>
        Field(element, key) is { ValueKind: JsonValueKind.String } value ? value.GetString() : null;
}
