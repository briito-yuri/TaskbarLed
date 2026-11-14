using System.Globalization;
using TaskbarLed.Core.Ipc;

namespace TaskbarLed.App.Infrastructure;

/// <summary>Minimal append-only log in %LOCALAPPDATA%\TaskbarLed\taskbarled.log, capped at 1 MB.</summary>
public sealed class AppLog
{
    private const long MaxBytes = 1024 * 1024;
    private readonly Lock _gate = new();

    public string FilePath { get; } = Path.Combine(IpcEndpoints.DataDirectory, "taskbarled.log");

    public void Info(string message) => Write("INFO", message, null);

    public void Error(string message, Exception? exception = null) => Write("ERROR", message, exception);

    private void Write(string level, string message, Exception? exception)
    {
        try
        {
            lock (_gate)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
                var file = new FileInfo(FilePath);
                if (file.Exists && file.Length > MaxBytes)
                {
                    File.Move(FilePath, FilePath + ".old", overwrite: true);
                }

                var line = string.Create(CultureInfo.InvariantCulture, $"{DateTimeOffset.Now:O} {level} {message}");
                if (exception is not null)
                {
                    line += Environment.NewLine + exception;
                }

                File.AppendAllText(FilePath, line + Environment.NewLine);
            }
        }
        catch (IOException)
        {
            // Logging must never crash the panel.
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
