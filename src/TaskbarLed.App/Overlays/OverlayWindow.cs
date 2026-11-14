using System.Windows;
using System.Windows.Interop;
using TaskbarLed.Core.Visibility;
using TaskbarLed.Platform.Windows.Windowing;

namespace TaskbarLed.App.Overlays;

/// <summary>Borderless, topmost, non-activating window placed in physical pixels. Base for full-screen overlays.</summary>
public abstract class OverlayWindow : Window
{
    private readonly bool _clickThrough;
    private PixelRect _bounds;

    protected OverlayWindow(bool clickThrough)
    {
        _clickThrough = clickThrough;
        WindowStyle = WindowStyle.None;
        AllowsTransparency = true;
        Background = System.Windows.Media.Brushes.Transparent;
        ShowInTaskbar = false;
        ShowActivated = false;
        Topmost = true;
        ResizeMode = ResizeMode.NoResize;
        UseLayoutRounding = true;
        SnapsToDevicePixels = true;
    }

    protected nint Handle { get; private set; }

    public void Place(PixelRect bounds)
    {
        if (Handle == 0)
        {
            return;
        }

        if (bounds == _bounds)
        {
            WindowActions.KeepOnTop(Handle);
            return;
        }

        _bounds = bounds;
        WindowActions.PlaceOnTop(Handle, bounds);
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        Handle = new WindowInteropHelper(this).Handle;
        WindowActions.MakeOverlay(Handle);
        WindowActions.SetClickThrough(Handle, _clickThrough);
    }
}
