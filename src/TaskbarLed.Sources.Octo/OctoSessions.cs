using System.Buffers.Binary;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using TaskbarLed.Core.Sessions;
using TaskbarLed.Core.Sources;

namespace TaskbarLed.Sources.Octo;

/// <summary>Reads the status message from Octo's local holder protocol v1 without attaching to the terminal.</summary>
public sealed class OctoSessions(string agents) : ISessionScanner
{
    private const string Source = "octo";
    private const int Protocol = 1;

    public string SourceId => Source;

    public async ValueTask<IReadOnlyList<Session>> ScanAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (!Directory.Exists(agents))
            {
                return [];
            }

            var directories = new DirectoryInfo(agents).EnumerateDirectories()
                .Where(directory => directory.LinkTarget is null)
                .Take(64);
            var sessions = await Task.WhenAll(directories.Select(d => ReadAsync(d.FullName, cancellationToken)))
                .ConfigureAwait(false);
            return [.. sessions.OfType<Session>().OrderBy(s => s.Id, StringComparer.Ordinal)];
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return [];
        }
    }

    private static async Task<Session?> ReadAsync(string directory, CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromMilliseconds(900));
        var token = timeout.Token;
        try
        {
            using var runtime = await ReadJsonAsync(Path.Combine(directory, "runtime.json"), token).ConfigureAwait(false);
            using var agent = await ReadJsonAsync(Path.Combine(directory, "agent.json"), token).ConfigureAwait(false);
            if (runtime is null || agent is null
                || Number(runtime.RootElement, "protocol") != Protocol
                || Number(runtime.RootElement, "port") is not (>= 1 and <= 65535 and var port)
                || Number(runtime.RootElement, "holder_pid") is not (> 0 and <= int.MaxValue and var holder)
                || Text(runtime.RootElement, "token") is not { Length: > 0 and <= 256 } key
                || Number(agent.RootElement, "id") is not (> 0 and <= uint.MaxValue and var id)
                || !IsOctoHolder((int)holder, Text(runtime.RootElement, "exe")))
            {
                return null;
            }

            using var client = new TcpClient(AddressFamily.InterNetwork);
            await client.ConnectAsync(IPAddress.Loopback, (int)port, token).ConfigureAwait(false);
            using var stream = client.GetStream();
            var hello = JsonSerializer.SerializeToUtf8Bytes(new { token = key, protocol = Protocol });
            var header = new byte[5];
            BinaryPrimitives.WriteInt32LittleEndian(header, hello.Length + 1);
            header[4] = 1;
            await stream.WriteAsync(header, token).ConfigureAwait(false);
            await stream.WriteAsync(hello, token).ConfigureAwait(false);

            using var welcome = await ReadFrameAsync(stream, 64, token).ConfigureAwait(false);
            if (welcome is null || Number(welcome.RootElement, "protocol") != Protocol
                || Number(welcome.RootElement, "holder_pid") != holder
                || Number(welcome.RootElement, "agent_id") != id)
            {
                return null;
            }

            using var status = await ReadFrameAsync(stream, 65, token).ConfigureAwait(false);
            var session = status is null ? null : Parse(agent.RootElement, status.RootElement);
            if (session is null || session.ProcessId is not null)
            {
                return session;
            }

            var childPid = Number(runtime.RootElement, "child_pid");
            return childPid is > 0 and <= int.MaxValue ? session with { ProcessId = (int)childPid } : session;
        }
        catch (Exception ex) when (ex is SocketException or IOException or UnauthorizedAccessException or JsonException)
        {
            return null;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return null;
        }
    }

    internal static Session? Parse(JsonElement agent, JsonElement status)
    {
        var state = Text(status, "state") switch
        {
            "working" => SessionStatus.Running,
            "paused" => SessionStatus.AwaitingInput,
            "done" => SessionStatus.Completed,
            "idle" => SessionStatus.Idle,
            _ => (SessionStatus?)null,
        };
        var id = Number(agent, "id");
        var nested = status.ValueKind == JsonValueKind.Object && status.TryGetProperty("nested", out var child) ? child : default;
        var kind = Text(nested, "kind") ?? Text(agent, "kind");
        if (state is null || id is not (> 0 and <= uint.MaxValue) || kind is null or "terminal"
            || Timestamp(status, "state_since_ms") is not { } at)
        {
            return null;
        }

        var label = kind switch
        {
            "antigravity" => "Antigravity",
            "claude" => "Claude",
            "codex" => "Codex",
            "gemini" => "Gemini",
            _ => kind,
        };
        var name = Text(agent, "name");
        var title = string.IsNullOrWhiteSpace(name) ? $"Octo · {label}" : $"{label} · {name[..Math.Min(name.Length, 64)]}";
        var started = Timestamp(status, "turn_started_ms");
        if (started is null && Number(status, "last_turn_ms") is > 0 and var duration
            && duration <= at.ToUnixTimeMilliseconds())
        {
            started = at.AddMilliseconds(-duration);
        }

        return new Session(string.Create(CultureInfo.InvariantCulture, $"octo:{id}"), Source, title, null, state.Value, started ?? at,
            state == SessionStatus.Completed ? at : null, null, null, at)
        {
            EngineName = label,
            ConversationId = Guid.TryParse(Text(nested, "conversation_id") ?? Text(agent, "conversation_id"), out var conversation)
                ? conversation : null,
            ProcessId = Number(nested, "pid") is > 0 and <= int.MaxValue and var pid ? (int)pid : null,
            ParentId = Number(agent, "parent") is > 0 and <= uint.MaxValue and var parent
                ? string.Create(CultureInfo.InvariantCulture, $"octo:{parent}") : null,
        };
    }

    private static bool IsOctoHolder(int pid, string? executable)
    {
        if (!string.Equals(Path.GetFileName(executable), "octo", StringComparison.Ordinal) || !File.Exists(executable))
        {
            return false;
        }

        try
        {
            using var process = Process.GetProcessById(pid);
            return string.Equals(process.ProcessName, "octo", StringComparison.Ordinal);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            return false;
        }
    }

    private static async Task<JsonDocument?> ReadJsonAsync(string path, CancellationToken token)
    {
        var info = new FileInfo(path);
        if (!info.Exists || info.LinkTarget is not null || info.Length > 131072)
        {
            return null;
        }

        await using var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        return await JsonDocument.ParseAsync(file, cancellationToken: token).ConfigureAwait(false);
    }

    private static async Task<JsonDocument?> ReadFrameAsync(Stream stream, byte kind, CancellationToken token)
    {
        var header = new byte[5];
        await stream.ReadExactlyAsync(header, token).ConfigureAwait(false);
        var length = BinaryPrimitives.ReadInt32LittleEndian(header);
        if (header[4] != kind || length is < 2 or > 131072)
        {
            return null;
        }

        var payload = new byte[length - 1];
        await stream.ReadExactlyAsync(payload, token).ConfigureAwait(false);
        return JsonDocument.Parse(payload);
    }

    private static DateTimeOffset? Timestamp(JsonElement element, string key) =>
        Number(element, key) is > 0 and <= 253402300799999 and var ms ? DateTimeOffset.FromUnixTimeMilliseconds(ms) : null;

    private static long? Number(JsonElement element, string key) =>
        element.ValueKind == JsonValueKind.Object && element.TryGetProperty(key, out var value)
        && value.ValueKind == JsonValueKind.Number && value.TryGetInt64(out var number) ? number : null;

    private static string? Text(JsonElement element, string key) =>
        element.ValueKind == JsonValueKind.Object && element.TryGetProperty(key, out var value)
        && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
}
