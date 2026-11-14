using System.Globalization;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using TaskbarLed.App.Avalonia.Demo;
using TaskbarLed.App.Avalonia.Panels;
using TaskbarLed.App.Avalonia.Presentation;

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
            var feed = new DemoFeed(culture, DateTimeOffset.Now);
            desktop.MainWindow = new FloatingPanelWindow(viewModel, () => feed.Compose(DateTimeOffset.Now));
            desktop.ShutdownMode = global::Avalonia.Controls.ShutdownMode.OnExplicitShutdown;
        }

        base.OnFrameworkInitializationCompleted();
    }
}
