using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace TaskbarLed.App.Avalonia.Panels;

public sealed partial class PanelView : UserControl
{
    public PanelView() => AvaloniaXamlLoader.Load(this);
}
