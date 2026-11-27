using System.Globalization;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using TaskbarLed.App.Avalonia.Demo;
using TaskbarLed.App.Avalonia.Panels;
using TaskbarLed.App.Avalonia.Presentation;

namespace TaskbarLed.App.Avalonia.Tests;

/// <summary>
/// Renders the real panel XAML with the real composer, the way it will look floating above the macOS Dock.
/// The image goes to <c>TASKBARLED_SCREENSHOTS</c> (CI uploads it) or next to the test binaries.
/// </summary>
public class FloatingPanelTests
{
    private const double SceneWidth = 900;
    private const double SceneHeight = 170;
    private const double DockHeight = 64;

    private static readonly DateTimeOffset Now = new(2026, 11, 14, 14, 32, 0, TimeSpan.Zero);

    [Fact]
    public void Panel_is_centered_just_above_the_dock_on_a_retina_screen()
    {
        // 1440x900 points at 2x; menu bar 25 pt on top, Dock 70 pt at the bottom.
        var workArea = new PixelRect(0, 50, 2880, 1610);

        var rect = FloatingPanelWindow.Layout(workArea, 2.0);

        Assert.Equal(840, rect.Width);
        Assert.Equal((2880 - 840) / 2, rect.Left);
        Assert.Equal(50 + 1610 - 16, rect.Bottom);
    }

    [AvaloniaFact]
    public void Demo_feed_renders_lit_led_text_and_saves_a_screenshot()
    {
        var culture = CultureInfo.GetCultureInfo("en-US");
        var viewModel = new PanelViewModel(culture) { Uppercase = true };
        viewModel.Apply(new DemoFeed(culture, Now).Compose(Now));

        var window = new Window { Width = SceneWidth, Height = SceneHeight, Content = Scene(viewModel) };
        window.Show();
        var frame = window.CaptureRenderedFrame();
        Assert.NotNull(frame);

        Assert.Equal(3, viewModel.Channels.Count);
        Assert.Equal("EDITING SCRAPER.PY", viewModel.Feed);
        Assert.True(viewModel.IsBlinking); // "api" finished and has not been seen.
        Assert.True(CountLedGreenPixels(frame) > 200, "The feed should be drawn in LED green.");

        var folder = Environment.GetEnvironmentVariable("TASKBARLED_SCREENSHOTS") is { Length: > 0 } configured
            ? configured
            : Path.Combine(AppContext.BaseDirectory, "screenshots");
        Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, "mac-floating-panel.png");
        frame.Save(path, new PngBitmapEncoderOptions());
        Assert.True(new FileInfo(path).Length > 0);
    }

    // A stand-in desktop: wallpaper, a Dock at the bottom, and the panel centered 8 pt above it.
    private static Panel Scene(PanelViewModel viewModel)
    {
        var scene = new Panel
        {
            Background = new LinearGradientBrush
            {
                StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
                EndPoint = new RelativePoint(1, 1, RelativeUnit.Relative),
                GradientStops = { new GradientStop(Color.Parse("#2B3A55"), 0), new GradientStop(Color.Parse("#4A3B5C"), 1) },
            },
        };

        scene.Children.Add(new Border
        {
            Width = 480,
            Height = DockHeight,
            CornerRadius = new CornerRadius(18),
            Background = new SolidColorBrush(Color.Parse("#66FFFFFF")),
            BorderBrush = new SolidColorBrush(Color.Parse("#40FFFFFF")),
            BorderThickness = new Thickness(1),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Bottom,
            Margin = new Thickness(0, 0, 0, 6),
        });

        scene.Children.Add(new PanelView
        {
            DataContext = viewModel,
            Width = FloatingPanelWindow.PanelWidth,
            Height = FloatingPanelWindow.PanelHeight,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Bottom,
            Margin = new Thickness(0, 0, 0, 6 + DockHeight + FloatingPanelWindow.DockGap),
        });

        return scene;
    }

    private static int CountLedGreenPixels(WriteableBitmap bitmap)
    {
        using var buffer = bitmap.Lock();
        var bytes = new byte[buffer.RowBytes * buffer.Size.Height];
        Marshal.Copy(buffer.Address, bytes, 0, bytes.Length);

        var count = 0;
        for (var y = 0; y < buffer.Size.Height; y++)
        {
            for (var x = 0; x < buffer.Size.Width; x++)
            {
                var i = (y * buffer.RowBytes) + (x * 4);
                byte b = bytes[i], g = bytes[i + 1], r = bytes[i + 2]; // Bgra8888
                if (g > 180 && r < 120 && b < 150)
                {
                    count++;
                }
            }
        }

        return count;
    }
}
