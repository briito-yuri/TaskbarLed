using System.Globalization;
using System.Windows;
using System.Windows.Threading;
using TaskbarLed.App.Demo;
using TaskbarLed.App.Infrastructure;
using TaskbarLed.Core.Ipc;
using TaskbarLed.Platform.Windows.System;
using TaskbarLed.Sources.ClaudeCode;

namespace TaskbarLed.App;

/// <summary>
/// Entry point. Command line:
/// <list type="bullet">
///   <item><c>--install-hooks</c> / <c>--uninstall-hooks</c>: manage Claude Code hooks and exit (for scripts and uninstallers).</item>
///   <item><c>--render-demo &lt;folder&gt;</c>: render README screenshots of every state and theme, then exit.</item>
///   <item><c>--autostart</c>: marks a start at Windows sign-in (no behavior change today).</item>
///   <item><c>--restart</c>: wait for the previous instance to exit (used when the language changes).</item>
///   <item><c>--exit</c>: ask the running instance to exit cleanly, then exit.</item>
/// </list>
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Design", "CA1001:Types that own disposable fields should be disposable",
    Justification = "WPF owns the Application lifetime; the fields are released in OnExit.")]
public partial class App : Application
{
    public const string RestartArgument = "--restart";

    private const string ExitEventName = @"Local\TaskbarLed.Exit";

    private readonly AppLog _log = new();
    private Mutex? _singleInstance;
    private bool _ownsMutex;
    private AppHost? _host;
    private EventWaitHandle? _exitRequest;
    private RegisteredWaitHandle? _exitRequestWait;

    // "TaskbarLed.exe --exit" (scripts, uninstallers) signals this event; the running instance exits cleanly.
    private void ListenForExitRequests()
    {
        _exitRequest = new EventWaitHandle(false, EventResetMode.AutoReset, ExitEventName);
        _exitRequestWait = ThreadPool.RegisterWaitForSingleObject(
            _exitRequest,
            (_, _) => Dispatcher.BeginInvoke(() => Shutdown()),
            null,
            Timeout.Infinite,
            executeOnlyOnce: true);
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);
        base.OnStartup(e);

        ApplyLanguage(new SettingsStore(_log).Load().Language);

        if (TryRunCommand(e.Args, out var exitCode))
        {
            Shutdown(exitCode);
            return;
        }

        // Started from inside another app's package (e.g. a Claude Code session in the Claude desktop app):
        // our settings, log and auto-start entry would be redirected into that package. Start over outside it.
        if (PackageContext.IsRedirected(IpcEndpoints.DataDirectory))
        {
            if (Environment.ProcessPath is { } exe && PackageContext.TryRelaunchOutside(exe, IpcEndpoints.DataDirectory))
            {
                Shutdown(0);
                return;
            }

            _log.Error("Running inside another app's container; settings and auto-start may not persist.");
        }

        var restarting = e.Args.Contains(RestartArgument);
        _singleInstance = new Mutex(initiallyOwned: false, @"Local\TaskbarLed.SingleInstance");
        try
        {
            _ownsMutex = _singleInstance.WaitOne(restarting ? TimeSpan.FromSeconds(10) : TimeSpan.Zero);
        }
        catch (AbandonedMutexException)
        {
            _ownsMutex = true; // Previous instance exited without releasing it: ours now.
        }

        if (!_ownsMutex)
        {
            Shutdown(0); // Already running.
            return;
        }

        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += (_, args) => _log.Error("Unhandled exception.", args.ExceptionObject as Exception);

        _host = new AppHost();
        _host.Start();
        ListenForExitRequests();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _exitRequestWait?.Unregister(null);
        _exitRequest?.Dispose();
        _host?.Dispose();
        if (_ownsMutex)
        {
            _singleInstance?.ReleaseMutex();
        }

        _singleInstance?.Dispose();
        base.OnExit(e);
    }

    /// <summary><c>auto</c> follows Windows; otherwise the chosen language wins for every UI string.</summary>
    private static void ApplyLanguage(string? language)
    {
        if (language is "pt" or "en")
        {
            var culture = CultureInfo.GetCultureInfo(language == "pt" ? "pt-BR" : "en-US");
            CultureInfo.DefaultThreadCurrentUICulture = culture;
            CultureInfo.CurrentUICulture = culture;
        }
    }

    private bool TryRunCommand(string[] args, out int exitCode)
    {
        exitCode = 0;

        var demoIndex = Array.IndexOf(args, "--render-demo");
        if (demoIndex >= 0)
        {
            var folder = demoIndex + 1 < args.Length ? args[demoIndex + 1] : "docs/images";
            DemoRenderer.RenderAll(this, Path.GetFullPath(folder));
            return true;
        }

        if (args.Contains("--exit"))
        {
            if (EventWaitHandle.TryOpenExisting(ExitEventName, out var running))
            {
                using (running)
                {
                    running.Set();
                }
            }

            return true;
        }

        var command = args.FirstOrDefault(a => a is "--install-hooks" or "--uninstall-hooks");
        if (command is null)
        {
            return false;
        }

        var installer = new ClaudeCodeHookInstaller(
            ClaudeCodeHookInstaller.DefaultSettingsPath, Path.Combine(AppContext.BaseDirectory, "TaskbarLed.Hook.exe"));
        try
        {
            if (command == "--install-hooks")
            {
                installer.Install();
            }
            else
            {
                installer.Uninstall();
            }
        }
        catch (Exception ex) when (ex is InvalidOperationException or IOException or UnauthorizedAccessException)
        {
            _log.Error($"{command} failed.", ex);
            exitCode = 1;
        }

        return true;
    }

    // Keep the panel alive: a drawing glitch is better than a vanished app.
    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        _log.Error("Unhandled UI exception.", e.Exception);
        e.Handled = true;
    }
}
