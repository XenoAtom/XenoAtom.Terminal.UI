using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;

namespace XenoAtom.Terminal.UI.ControlsDemo.Demos;

[Demo("ZStack", "Layout", Description = "Overlay children on the same bounds.")]
public sealed class ZStackDemo : ControlsDemoBase
{
    public ZStackDemo() : base(DemoSource.Get())
    {
    }

    public override Visual Build(DemoContext context)
    {
        _ = context;

        // ZStack draws children in order; later children appear "on top".
        return new VStack(
                DemoUi.Hint("ZStack overlays children. Use it for backdrops and layers."),
                new Border().Padding(1).Content(
                    new ZStack(
                        new TextBlock("Background"),
                        new Center().Content("Foreground"))))
            .Spacing(1);
    }
}

