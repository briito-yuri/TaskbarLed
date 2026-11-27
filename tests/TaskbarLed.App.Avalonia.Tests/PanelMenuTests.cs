using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;

namespace TaskbarLed.App.Avalonia.Tests;

public class PanelMenuTests
{
    [AvaloniaFact]
    public void The_open_menu_changes_with_the_panel_theme()
    {
        var model = new Presentation.PanelViewModel(CultureInfo.GetCultureInfo("pt-BR"));
        var app = Application.Current!;
        var menu = new ContextMenu { Classes = { "neon-panel-menu" } };
        menu.Items.Add(new MenuItem { Header = App.DisplayName, IsEnabled = false, Classes = { "neon-menu-heading" } });
        menu.Items.Add(new MenuItem { Header = "Tema" });
        App.AddQuitItems(new NativeMenu(), menu, portuguese: true, () => { });
        var window = new Window { Width = 360, Height = 220, ContextMenu = menu };
        window.Show();
        try
        {
            PanelThemes.Apply(app, model, "EditorialIndex");
            menu.Open(window);
            Dispatcher.UIThread.RunJobs();
            Assert.True(menu.IsOpen);
            var light = Assert.IsAssignableFrom<ISolidColorBrush>(menu.Background).Color;
            Assert.Equal(Color.Parse("#FFFDF7"), light);
            var save = Environment.GetEnvironmentVariable("TASKBARLED_SCREENSHOTS");
            if (save is not null && TopLevel.GetTopLevel(menu) is { } popup)
            {
                Directory.CreateDirectory(save);
                using var frame = popup.CaptureRenderedFrame();
                frame?.Save(Path.Combine(save, "neon-context-menu.png"), new PngBitmapEncoderOptions());
            }

            PanelThemes.Apply(app, model, "AmberTree");
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(Color.Parse("#1C1D18"), Assert.IsAssignableFrom<ISolidColorBrush>(menu.Background).Color);
            Assert.NotEqual(light, Assert.IsAssignableFrom<ISolidColorBrush>(menu.Background).Color);
        }
        finally
        {
            menu.Close();
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Right_click_menu_can_quit_the_app()
    {
        var native = new NativeMenu();
        var context = new ContextMenu();
        var quit = false;
        App.AddQuitItems(native, context, portuguese: true, () => quit = true);
        var item = Assert.IsType<MenuItem>(context.Items[^1]);
        Assert.Equal("Encerrar", item.Header);
        Assert.Equal("Encerrar", Assert.IsType<NativeMenuItem>(native.Items[^1]).Header);

        item.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
        Dispatcher.UIThread.RunJobs();
        Assert.True(quit);
    }
}
