using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;

namespace XenoAtom.Terminal.UI.ControlsDemo;

internal static class DemoUi
{
    public static Visual Title(string text) => new Markup($"[bold]{text}[/]");

    public static Visual Hint(string text) => new Markup($"[dim]{text}[/]").Wrap(true);
}
