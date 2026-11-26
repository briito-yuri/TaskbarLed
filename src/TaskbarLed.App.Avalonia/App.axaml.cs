using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using TaskbarLed.App.Avalonia.Demo;
using TaskbarLed.App.Avalonia.Panels;
using TaskbarLed.App.Avalonia.Presentation;
using TaskbarLed.Core.Ipc;

namespace TaskbarLed.App.Avalonia;

public sealed partial class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var culture = CultureInfo.CurrentUICulture;
            var viewModel = new PanelViewModel(culture) { Uppercase = true };
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            if (desktop.Args?.Contains("--demo", StringComparer.Ordinal) == true || !OperatingSystem.IsMacOS())
            {
                var demo = new DemoFeed(culture, DateTimeOffset.Now);
                desktop.MainWindow = new FloatingPanelWindow(viewModel, () => demo.Compose(DateTimeOffset.Now), keepVisible: true);
            }
            else
            {
                StartLive(desktop, culture, viewModel);
            }
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void StartLive(IClassicDesktopStyleApplicationLifetime desktop, CultureInfo culture, PanelViewModel viewModel)
    {
        Directory.CreateDirectory(IpcEndpoints.DataDirectory);
        FileStream instance;
        try
        {
            instance = File.Open(Path.Combine(IpcEndpoints.DataDirectory, "panel.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        }
        catch (IOException)
        {
            global::Avalonia.Threading.Dispatcher.UIThread.Post(() => desktop.Shutdown());
            return;
        }

        var feed = new LiveFeed(culture);
        var panel = new FloatingPanelWindow(viewModel, () => feed.Compose(DateTimeOffset.Now), feed.Acknowledge, feed.Skip);
        desktop.MainWindow = panel;
        var cancellation = new CancellationTokenSource();
        var readers = Task.WhenAll(
            Task.Run(() => feed.ReadAsync(cancellation.Token)),
            Task.Run(() => feed.DetectProcessesAsync(cancellation.Token)));

        var menu = new NativeMenu();
        var pt = culture.TwoLetterISOLanguageName == "pt";
        var toggle = new NativeMenuItem(pt ? "Mostrar painel sem atividade" : "Show panel when idle") { ToggleType = MenuItemToggleType.CheckBox };
        toggle.Click += (_, _) =>
        {
            panel.KeepVisible = !panel.KeepVisible;
            toggle.IsChecked = panel.KeepVisible;
        };
        menu.Items.Add(toggle);
        var quit = new NativeMenuItem(pt ? "Encerrar TaskbarLed" : "Quit TaskbarLed");
        quit.Click += (_, _) => desktop.Shutdown();
        menu.Items.Add(quit);
        var tray = new TrayIcon { ToolTipText = "TaskbarLed", Icon = CreateTrayIcon(), Menu = menu, IsVisible = true };
        TrayIcon.SetIcons(this, [tray]);
        desktop.Exit += (_, _) =>
        {
            cancellation.Cancel();
            tray.Dispose();
            _ = readers.ContinueWith(task =>
            {
                _ = task.Exception;
                cancellation.Dispose();
                instance.Dispose();
            }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
        };
    }

    private static WindowIcon CreateTrayIcon()
    {
        var dots = new TextBlock
        {
            Text = "•••",
            Foreground = Brushes.Black,
            Background = Brushes.Transparent,
            FontWeight = FontWeight.Bold,
            FontSize = 15,
            TextAlignment = TextAlignment.Center,
        };
        var rect = new Rect(0, 0, 24, 16);
        dots.Measure(rect.Size);
        dots.Arrange(rect);
        using var bitmap = new RenderTargetBitmap(new PixelSize(48, 32), new Vector(192, 192));
        bitmap.Render(dots);
        return new WindowIcon(bitmap);
    }
}
