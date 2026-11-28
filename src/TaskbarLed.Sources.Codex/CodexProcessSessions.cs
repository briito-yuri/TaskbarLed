using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Text.Json;

namespace TaskbarLed.Sources.Codex;

public static class CodexProcessSessions
{
    public static async Task<Guid?> FindAsync(string processInspector, int pid, CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromMilliseconds(700));
        try
        {
            var options = new ProcessStartInfo(processInspector)
            {
                ArgumentList = { "-nP", "-a", "-p", pid.ToString(CultureInfo.InvariantCulture), "-Fn" },
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            };
            using var process = Process.Start(options);
            if (process is null)
            {
                return null;
            }

            try
            {
                var output = process.StandardOutput.ReadToEndAsync(timeout.Token);
                var errors = process.StandardError.ReadToEndAsync(timeout.Token);
                await Task.WhenAll(output, errors, process.WaitForExitAsync(timeout.Token)).ConfigureAwait(false);
                return FindConversation(await output.ConfigureAwait(false));
            }
            catch (OperationCanceledException)
            {
                if (!process.HasExited)
                {
                    process.Kill();
                }

                throw;
            }
        }
        catch (Exception ex) when (ex is Win32Exception or IOException or InvalidOperationException)
        {
            return null;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return null;
        }
    }

    internal static Guid? FindConversation(string output, Func<string, bool>? isSubagent = null)
    {
        isSubagent ??= IsSubagent;
        Guid? conversation = null;
        foreach (var line in output.Split('\n'))
        {
            if (line.StartsWith('n') && Path.GetFileName(line) is { Length: >= 50 } name
                && name.StartsWith("rollout-", StringComparison.Ordinal) && name.EndsWith(".jsonl", StringComparison.Ordinal)
                && Guid.TryParse(name.AsSpan(name.Length - 42, 36), out var id)
                && !isSubagent(line[1..]))
            {
                if (conversation.HasValue && conversation != id)
                {
                    return null;
                }

                conversation = id;
            }
        }

        return conversation;
    }
    private static bool IsSubagent(string path)
    {
        try
        {
            using var input = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            using var reader = new StreamReader(input);
            var first = reader.ReadLine();
            if (first is null)
            {
                return false;
            }

            using var header = JsonDocument.Parse(first);
            return header.RootElement.TryGetProperty("payload", out var payload)
                && payload.TryGetProperty("source", out var source) && source.ValueKind == JsonValueKind.Object
                && source.TryGetProperty("subagent", out var subagent) && subagent.ValueKind == JsonValueKind.Object
                && subagent.TryGetProperty("thread_spawn", out _);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            return false;
        }
    }
}
