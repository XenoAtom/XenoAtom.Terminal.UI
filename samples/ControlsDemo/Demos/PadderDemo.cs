using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Geometry;

namespace XenoAtom.Terminal.UI.ControlsDemo.Demos;

[Demo("Padder", "Layout", Description = "Adds padding around a single child (no border).")]
public sealed class PadderDemo : ControlsDemoBase
{
    public PadderDemo() : base(DemoSource.Get())
    {
    }

    public override Visual Build(DemoContext context)
    {
        _ = context;

        return new VStack(
                DemoUi.Hint("Padder is a lightweight layout control that only adds padding."),
                new Padder("Padded content").Padding(1),
                new TextBlock("Any visual can also be padded using the Pad(...) helper:").Pad(1),
                new TextBlock("Hello").Pad(new Thickness(Left: 2, Top: 1, Right: 2, Bottom: 1)))
            .Spacing(1);
    }
}
