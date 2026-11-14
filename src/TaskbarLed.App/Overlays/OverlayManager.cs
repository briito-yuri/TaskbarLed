using TaskbarLed.Core.Presentation;
using TaskbarLed.Core.Visibility;
using TaskbarLed.Platform.Windows.Windowing;

namespace TaskbarLed.App.Overlays;

/// <summary>Owns the full-screen companions (edge glow and alerts), one of each per monitor, created on demand.</summary>
public sealed class OverlayManager : IDisposable
{
    private readonly Dictionary<nint, EdgeGlowWindow> _edges = [];
    private readonly Dictionary<nint, AlertWindow> _alerts = [];

    /// <summary>An alert was clicked.</summary>
    public event EventHandler? OpenRequested;

    /// <summary>Shows the edge glow on <paramref name="glowOn"/> monitors and hides it everywhere else.</summary>
    public void SyncEdges(IReadOnlyList<MonitorInfo> glowOn, bool attention)
    {
        ArgumentNullException.ThrowIfNull(glowOn);

        var wanted = glowOn.ToDictionary(m => m.Handle);
        foreach (var (handle, window) in _edges)
        {
            if (!wanted.ContainsKey(handle))
            {
                window.HideGlow();
            }
        }

        foreach (var monitor in glowOn)
        {
            if (!_edges.TryGetValue(monitor.Handle, out var window))
            {
                window = new EdgeGlowWindow();
                _edges[monitor.Handle] = window;
            }

            window.ShowOn(PanelLayout.EdgeStrip(monitor.Bounds, monitor.Scale), attention);
        }
    }

    public void ShowAlert(PanelAlert alert, MonitorInfo monitor)
    {
        ArgumentNullException.ThrowIfNull(monitor);
        if (!_alerts.TryGetValue(monitor.Handle, out var window))
        {
            window = new AlertWindow();
            window.OpenRequested += (_, _) => OpenRequested?.Invoke(this, EventArgs.Empty);
            _alerts[monitor.Handle] = window;
        }

        window.Enqueue(alert, monitor);
    }

    /// <summary>Claude came back into view: alerts are no longer needed.</summary>
    public void DismissAlerts()
    {
        foreach (var window in _alerts.Values)
        {
            window.Dismiss();
        }
    }

    public void Dispose()
    {
        foreach (var window in _edges.Values.Cast<System.Windows.Window>().Concat(_alerts.Values))
        {
            window.Close();
        }

        _edges.Clear();
        _alerts.Clear();
    }
}
