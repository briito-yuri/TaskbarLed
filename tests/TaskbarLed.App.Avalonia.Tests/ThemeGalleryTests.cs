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

public class ThemeGalleryTests
{
    [AvaloniaFact]
    public void Every_theme_renders_a_provider_neutral_session_hierarchy()
    {
        var folder = Environment.GetEnvironmentVariable("TASKBARLED_THEME_GALLERY");
        if (folder is not null)
        {
            Directory.CreateDirectory(folder);
        }

        var model = new PanelViewModel(CultureInfo.GetCultureInfo("pt-BR"));
        var view = new PanelView { DataContext = model };
        var window = new Window
        {
            Width = 598,
            Height = 194,
            Background = new SolidColorBrush(Color.Parse("#53606C")),
            Content = new Border { Padding = new Thickness(10), Child = view },
        };
        model.Apply(PanelFrame.Empty("") with
        {
            SessionRows =
            [
                new("codex", "Codex → Referências do curso", SessionStatus.Running, true)
                    { Engine = "Codex", Heading = "Referências do curso" },
                new("research", "Subagente Pesquisador", SessionStatus.Running, false)
                    { Engine = "Codex", StationName = "Pesquisador" },
                new("review", "Subagente Revisor", SessionStatus.Completed, false, 8)
                    { Engine = "Codex", StationName = "Revisor", LastStation = true },
                new("agy", "Antigravity → Alemão", SessionStatus.Idle, true) { Engine = "Antigravity", Heading = "Alemão" },
                new("vocabulary", "Subagente Vocabulário", SessionStatus.Running, false)
                    { Engine = "Antigravity", StationName = "Vocabulário" },
                new("grammar", "Subagente Gramática", SessionStatus.AwaitingInput, false)
                    { Engine = "Antigravity", StationName = "Gramática", LastStation = true },
            ],
        });
        window.Show();
        try
        {
            foreach (var theme in PanelThemes.All)
            {
                PanelThemes.Apply(Application.Current!, model, theme.Id);
                using var bitmap = window.CaptureRenderedFrame();
                Assert.NotNull(bitmap);
                Assert.Equal(6, model.SessionRows.Count);
                Assert.Equal(2, model.SessionGroups.Count);
                if (folder is not null)
                {
                    bitmap.Save(Path.Combine(folder, theme.Id + ".png"), new PngBitmapEncoderOptions());
                }
            }
        }
        finally
        {
            window.Close();
        }
    }
}
