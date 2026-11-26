using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using TaskbarLed.App.Avalonia.Panels;
using TaskbarLed.App.Avalonia.Presentation;
using TaskbarLed.Core.Presentation;

namespace TaskbarLed.App.Avalonia.Tests;

public class PanelCustomizationTests
{
    [AvaloniaFact]
    public void All_Windows_themes_apply_to_the_same_panel()
    {
        var model = Model();
        var app = Application.Current!;
        var backgrounds = new HashSet<Color>();
        foreach (var theme in PanelThemes.All)
        {
            Assert.Equal(theme, PanelThemes.Apply(app, model, theme.Id));
            Assert.Equal(theme.Uppercase, model.Uppercase);
            backgrounds.Add(Assert.IsType<SolidColorBrush>(app.Resources["Led.BackgroundBrush"]).Color);
            Assert.IsAssignableFrom<IBrush>(app.Resources["Led.DotGridBrush"]);
        }

        Assert.Equal(7, backgrounds.Count);
        Assert.Equal("ClaudeCode", PanelThemes.Apply(app, model, "unknown").Id);
        Assert.Equal("✻", app.Resources["Led.FeedPrefix"]);
    }

    [AvaloniaFact]
    public void Drag_and_resize_save_coordinates_and_cancel_restores_them()
    {
        var panel = new FloatingPanelWindow(Model(), () => PanelFrame.Empty("Idle"), keepVisible: true);
        PanelPreferences? saved = null;
        panel.ConfigurePlacement(new PanelPreferences(X: 200, Y: 200), value => saved = value);
        panel.Show();
        try
        {
            panel.BeginEdit();
            Drag(panel, new Point(40, 15), new Point(120, 50));
            Assert.NotEqual(new PixelPoint(200, 200), panel.Position);
            Drag(panel, new Point(415, 15), new Point(470, 15));
            Assert.True(panel.Width > FloatingPanelWindow.PanelWidth);
            ClickEditorButton(panel, 0);
            Assert.NotNull(saved);
            Assert.Equal(panel.Position.X, saved.X);
            Assert.Equal(panel.Position.Y, saved.Y);
            Assert.Equal(panel.Width, saved.Width);

            var position = panel.Position;
            var width = panel.Width;
            panel.BeginEdit();
            Drag(panel, new Point(40, 15), new Point(95, 55));
            Assert.NotEqual(position, panel.Position);
            ClickEditorButton(panel, 1);
            Assert.Equal(position, panel.Position);
            Assert.Equal(width, panel.Width);
        }
        finally
        {
            panel.Close();
        }
    }

    private static PanelViewModel Model() => new(CultureInfo.GetCultureInfo("en-US"));

    private static void Drag(FloatingPanelWindow panel, Point start, Point end)
    {
        global::Avalonia.Threading.Dispatcher.UIThread.RunJobs();
        panel.MouseDown(start, MouseButton.Left);
        panel.MouseMove(end, RawInputModifiers.LeftMouseButton);
        panel.MouseUp(end, MouseButton.Left);
    }

    private static void ClickEditorButton(FloatingPanelWindow panel, int index)
    {
        var grid = Assert.IsType<Grid>(Assert.IsType<Border>(panel.Content).Child);
        var footer = Assert.IsType<StackPanel>(grid.Children[1]);
        var button = Assert.IsType<Button>(footer.Children[index]);
        button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
    }
}
