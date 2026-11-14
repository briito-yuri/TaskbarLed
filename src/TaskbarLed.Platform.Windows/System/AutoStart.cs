using Microsoft.Win32;

namespace TaskbarLed.Platform.Windows.System;

/// <summary>Starts the panel with Windows, per user (HKCU Run key; no admin rights needed).</summary>
public sealed class AutoStart
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private readonly string _valueName;
    private readonly string _command;

    /// <param name="valueName">Registry value name, e.g. <c>TaskbarLed</c>.</param>
    /// <param name="executablePath">The app executable.</param>
    /// <param name="arguments">Arguments that mark an automatic start.</param>
    public AutoStart(string valueName, string executablePath, string arguments = "--autostart")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(valueName);
        ArgumentException.ThrowIfNullOrWhiteSpace(executablePath);
        _valueName = valueName;
        _command = $"\"{executablePath}\" {arguments}".TrimEnd();
    }

    /// <summary>True when Windows will start this exact executable at sign-in.</summary>
    public bool IsEnabled
    {
        get
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey);
            return string.Equals(key?.GetValue(_valueName) as string, _command, StringComparison.OrdinalIgnoreCase);
        }
    }

    public void Enable()
    {
        using var key = Registry.CurrentUser.CreateSubKey(RunKey);
        key.SetValue(_valueName, _command, RegistryValueKind.String);
    }

    public void Disable()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: true);
        key?.DeleteValue(_valueName, throwOnMissingValue: false);
    }
}
