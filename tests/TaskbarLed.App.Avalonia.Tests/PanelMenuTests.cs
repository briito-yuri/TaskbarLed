using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.Threading;

namespace TaskbarLed.App.Avalonia.Tests;

public class PanelMenuTests
{
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
