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
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        Resources["Led.FeedPrefix"] = string.Empty;
    }

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
                PanelThemes.Apply(this, viewModel, "ClaudeCode");
                desktop.MainWindow = new FloatingPanelWindow(viewModel, () => demo.Compose(DateTimeOffset.Now), keepVisible: true,
                    portuguese: culture.TwoLetterISOLanguageName == "pt");
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
        var pt = culture.TwoLetterISOLanguageName == "pt";
        var preferences = PanelPreferences.Load();
        var currentTheme = PanelThemes.Apply(this, viewModel, preferences.Theme);
        var panel = new FloatingPanelWindow(viewModel, () => feed.Compose(DateTimeOffset.Now), feed.Acknowledge, feed.Skip, portuguese: pt);
        panel.ConfigurePlacement(preferences, next =>
        {
            try
            {
                next.Save();
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                var close = new Button { Content = "OK" };
                var error = new Window
                {
                    Title = "TaskbarLed",
                    Width = 320,
                    Height = 115,
                    Topmost = true,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    Content = new StackPanel
                    {
                        Margin = new Thickness(16),
                        Spacing = 12,
                        Children =
                        {
                            new TextBlock { Text = pt ? "Não foi possível salvar as preferências." : "Could not save panel preferences.",
                                TextWrapping = TextWrapping.Wrap },
                            close,
                        },
                    },
                };
                close.Click += (_, _) => error.Close();
                error.Show();
            }
        });
        desktop.MainWindow = panel;
        var cancellation = new CancellationTokenSource();
        var readers = Task.WhenAll(
            Task.Run(() => feed.ReadAsync(cancellation.Token)),
            Task.Run(() => feed.DetectProcessesAsync(cancellation.Token)));

        var menu = new NativeMenu();
        var panelMenu = new ContextMenu();
        panel.ContextMenu = panelMenu;
        var nativeThemes = new NativeMenu();
        var contextThemes = new MenuItem { Header = pt ? "Tema" : "Theme" };
        var checks = new List<(PanelThemes.Theme Theme, NativeMenuItem Native, MenuItem Context)>();
        foreach (var theme in PanelThemes.All)
        {
            var native = new NativeMenuItem(theme.Name) { ToggleType = MenuItemToggleType.CheckBox };
            var context = new MenuItem { Header = theme.Name, ToggleType = MenuItemToggleType.CheckBox };
            native.Click += (_, _) => SelectTheme(theme);
            context.Click += (_, _) => SelectTheme(theme);
            nativeThemes.Items.Add(native);
            contextThemes.Items.Add(context);
            checks.Add((theme, native, context));
        }

        void UpdateChecks()
        {
            foreach (var (theme, native, context) in checks)
            {
                native.IsChecked = context.IsChecked = theme == currentTheme;
            }
        }

        void SelectTheme(PanelThemes.Theme theme)
        {
            currentTheme = PanelThemes.Apply(this, viewModel, theme.Id);
            panel.SetTheme(currentTheme.Id);
            UpdateChecks();
        }

        void AddAction(string label, Action action)
        {
            var native = new NativeMenuItem(label);
            native.Click += (_, _) => action();
            menu.Items.Add(native);
            var context = new MenuItem { Header = label };
            context.Click += (_, _) => action();
            panelMenu.Items.Add(context);
        }

        AddAction(pt ? "Editar posição e tamanho…" : "Edit position and size…", panel.BeginEdit);
        AddAction(pt ? "Voltar para acima do Dock" : "Reset position above Dock", panel.ResetPosition);
        menu.Items.Add(new NativeMenuItem(pt ? "Tema" : "Theme") { Menu = nativeThemes });
        panelMenu.Items.Add(contextThemes);
        UpdateChecks();

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
