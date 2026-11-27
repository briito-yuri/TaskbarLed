namespace TaskbarLed.Sources.Antigravity;

/// <summary>Reads worker stream boundaries from recent CLI logs.</summary>
internal static class AntigravityStreams
{
    private const string Started = "Starting conversation update stream for ";
    private const string Ended = "Stream goroutine exited for ";
    private const int TailBytes = 256 * 1024;

    public static HashSet<Guid> Read(string directory)
    {
        var latest = new Dictionary<Guid, bool>();
        try
        {
            if (!Directory.Exists(directory))
            {
                return [];
            }

            var cutoff = DateTime.UtcNow.AddMinutes(-30);
            foreach (var log in new DirectoryInfo(directory).EnumerateFiles("cli-*.log")
                .Where(file => file.LastWriteTimeUtc >= cutoff && file.LinkTarget is null)
                .OrderBy(file => file.LastWriteTimeUtc).TakeLast(8))
            {
                using var file = log.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                var offset = Math.Max(0, file.Length - TailBytes);
                file.Seek(offset, SeekOrigin.Begin);
                using var reader = new StreamReader(file);
                if (offset > 0)
                {
                    reader.ReadLine();
                }

                while (reader.ReadLine() is { } line)
                {
                    Record(line, latest);
                }
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // The index remains available even if the CLI rotates a log during the read.
        }

        return latest.Where(pair => pair.Value).Select(pair => pair.Key).ToHashSet();
    }

    internal static void Record(string line, Dictionary<Guid, bool> latest)
    {
        var start = line.IndexOf(Started, StringComparison.Ordinal);
        var end = line.IndexOf(Ended, StringComparison.Ordinal);
        var offset = start >= 0 ? start + Started.Length : end >= 0 ? end + Ended.Length : -1;
        if (offset >= 0 && line.Length >= offset + 36 && Guid.TryParse(line.AsSpan(offset, 36), out var id))
        {
            latest[id] = start >= 0;
        }
    }
}
