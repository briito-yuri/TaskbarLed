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
        model.Apply(PanelFrame.Empty("") with
        {
            SessionRows =
            [
                new("codex", "Codex → Referências do curso", SessionStatus.Running, true) { Engine = "Codex" },
                new("c-child", "Subagente Pesquisador", SessionStatus.Running, false) { Engine = "Codex", StationName = "Pesquisador" },
                new("c-review", "Subagente Revisor", SessionStatus.AwaitingInput, false)
                    { Engine = "Codex", StationName = "Revisor", LastStation = true },
                new("agy", "Antigravity → Alemão", SessionStatus.Idle, true) { Engine = "Antigravity" },
                new("a-child", "Subagente Gramática", SessionStatus.Running, false)
                    { Engine = "Antigravity", StationName = "Gramática", LastStation = true },
                new("b", "Byte → Documentação", SessionStatus.Running, true) { Engine = "Byte", LastStation = true },
                new("z", "Zeta → Revisão", SessionStatus.Running, true) { Engine = "Zeta", LastStation = true },
            ],
        });
        var window = new Window { Width = 510, Height = 185, Content = new PanelView { DataContext = model } };
        window.Show();
        try
        {
            Assert.Equal(["C", "A", "B", "Z"], model.SessionRows.Where(row => row.IsHeader).Select(row => row.LineLetter));
            Assert.Same(model.SessionRows[0].LineBrush, model.SessionRows[1].LineBrush);
            Assert.Equal("Pesquisador", model.SessionRows[1].StationLabel);
            Assert.False(model.SessionRows[2].ConnectToNext);
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
