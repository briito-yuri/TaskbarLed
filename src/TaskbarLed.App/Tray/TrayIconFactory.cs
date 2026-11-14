using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using TaskbarLed.Core.Sessions;

namespace TaskbarLed.App.Tray;

/// <summary>
/// Draws the tray icon (three LED bars) in a status color. Rendered with WPF and wrapped as a
/// PNG-based .ico in memory, so no icon handle is leaked and no binary asset is needed.
/// </summary>
internal static class TrayIconFactory
{
    private const int Size = 32;

    public static System.Drawing.Icon Create(SessionStatus status)
    {
        var lit = status switch
        {
            SessionStatus.AwaitingInput => Color.FromRgb(0xFF, 0xB0, 0x2E),
            SessionStatus.Idle => Color.FromRgb(0x1F, 0x7A, 0x38),
            _ => Color.FromRgb(0x33, 0xFF, 0x66),
        };

        var visual = new DrawingVisual();
        using (var dc = visual.RenderOpen())
        {
            dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromRgb(0x02, 0x0A, 0x02)), new Pen(new SolidColorBrush(Color.FromRgb(0x0F, 0x3D, 0x12)), 2), new Rect(1, 1, Size - 2, Size - 2), 6, 6);
            var brush = new SolidColorBrush(lit);
            dc.DrawRectangle(brush, null, new Rect(7, 11, 4, 10));
            dc.DrawRectangle(brush, null, new Rect(14, 7, 4, 18));
            dc.DrawRectangle(brush, null, new Rect(21, 11, 4, 10));
        }

        var bitmap = new RenderTargetBitmap(Size, Size, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(visual);

        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var png = new MemoryStream();
        encoder.Save(png);

        return new System.Drawing.Icon(new MemoryStream(WrapPngAsIco(png.ToArray())));
    }

    // ICO container with a single PNG image (supported since Windows Vista).
    private static byte[] WrapPngAsIco(byte[] png)
    {
        using var ico = new MemoryStream();
        using var writer = new BinaryWriter(ico);
        writer.Write((short)0);        // reserved
        writer.Write((short)1);        // type: icon
        writer.Write((short)1);        // image count
        writer.Write((byte)Size);      // width
        writer.Write((byte)Size);      // height
        writer.Write((byte)0);         // palette
        writer.Write((byte)0);         // reserved
        writer.Write((short)1);        // color planes
        writer.Write((short)32);       // bits per pixel
        writer.Write(png.Length);      // image size
        writer.Write(6 + 16);          // image offset
        writer.Write(png);
        writer.Flush();
        return ico.ToArray();
    }
}
