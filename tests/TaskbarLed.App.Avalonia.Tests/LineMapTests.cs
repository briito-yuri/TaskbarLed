using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using TaskbarLed.App.Avalonia.Panels;
using TaskbarLed.App.Avalonia.Presentation;
using TaskbarLed.Core.Presentation;
using TaskbarLed.Core.Sessions;

namespace TaskbarLed.App.Avalonia.Tests;

public class LineMapTests
{
    [AvaloniaFact]
    public void Map_renders_different_line_letters_and_child_stations()
    {
        var model = new PanelViewModel(CultureInfo.GetCultureInfo("pt-BR"));
        PanelThemes.Apply(Application.Current!, model, "LineMap");
        var time = DateTimeOffset.UtcNow;
        var feed = new LiveFeed(CultureInfo.GetCultureInfo("pt-BR"));
        var sessions = Enumerable.Range(0, 7).SelectMany(index =>
        {
            var parent = $"route-{index}";
            return new[]
            {
                Session.Create(parent, "codex", time) with { Title = $"Sessão {index + 1}", Status = SessionStatus.Running },
                Session.Create($"worker-{index}", "codex", time) with
                {
                    Title = "Pesquisador",
                    ParentId = parent,
                    Status = SessionStatus.Running,
                },
            };
        }).ToArray();
        var initial = feed.Compose(time, sessions);
        model.Apply(initial);
        var shuffled = feed.Compose(time, sessions.Reverse().ToArray());
        foreach (var row in initial.SessionRows.Where(row => row.IsHeader))
        {
            Assert.Equal(row.RouteLetter, Assert.Single(shuffled.SessionRows, other => other.SessionId == row.SessionId).RouteLetter);
        }

        var window = new Window { Width = 510, Height = 380, Content = new PanelView { DataContext = model } };
        window.Show();
        try
        {
            Assert.Equal(["A", "B", "C", "D", "E", "F", "G"], model.SessionRows.Where(row => row.IsHeader).Select(row => row.LineLetter));
            Assert.Same(model.SessionRows[0].LineBrush, model.SessionRows[1].LineBrush);
            Assert.Equal("Pesquisador", model.SessionRows[1].StationLabel);
            Assert.False(model.SessionRows[1].ConnectToNext);
            using var frame = window.CaptureRenderedFrame();
            Assert.NotNull(frame);
            var folder = Environment.GetEnvironmentVariable("TASKBARLED_SCREENSHOTS") ?? Path.Combine(AppContext.BaseDirectory, "screenshots");
            Directory.CreateDirectory(folder);
            frame.Save(Path.Combine(folder, "mac-line-map.png"), new PngBitmapEncoderOptions());
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Editor_button_labels_have_explicit_contrasting_surfaces_in_every_theme()
    {
        var model = new PanelViewModel(CultureInfo.GetCultureInfo("pt-BR"));
        var window = new FloatingPanelWindow(model, () => PanelFrame.Empty("Ocioso"), keepVisible: true, portuguese: true);
        window.Show();
        window.BeginEdit();
        try
        {
            var editor = Assert.IsType<Grid>(Assert.IsType<Border>(window.Content).Child);
            var buttons = Assert.IsType<StackPanel>(editor.Children[1]);
            foreach (var theme in PanelThemes.All)
            {
                PanelThemes.Apply(Application.Current!, model, theme.Id);
                CheckButton(buttons, 0, "Salvar", "#204C40", "#FFFFFF");
                CheckButton(buttons, 1, "Cancelar", "#E8EDF0", "#202C35");
            }

            PanelThemes.Apply(Application.Current!, model, "EditorialIndex");
            using var frame = window.CaptureRenderedFrame();
            Assert.NotNull(frame);
            var folder = Environment.GetEnvironmentVariable("TASKBARLED_SCREENSHOTS") ?? Path.Combine(AppContext.BaseDirectory, "screenshots");
            Directory.CreateDirectory(folder);
            frame.Save(Path.Combine(folder, "mac-light-editor.png"), new PngBitmapEncoderOptions());
        }
        finally
        {
            window.Close();
        }
    }

    private static void CheckButton(StackPanel footer, int index, string expected, string background, string foreground)
    {
        var surface = Assert.IsType<Border>(Assert.IsType<Button>(footer.Children[index]).Content);
        var label = Assert.IsType<TextBlock>(surface.Child);
        Assert.Equal(expected, label.Text);
        Assert.Equal(Color.Parse(background), Assert.IsType<SolidColorBrush>(surface.Background).Color);
        Assert.Equal(Color.Parse(foreground), Assert.IsType<SolidColorBrush>(label.Foreground).Color);
    }
}
