using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;

namespace XenoAtom.Terminal.UI.ControlsDemo.Demos;

[Demo("Border", "Layout", Description = "Padding + optional border around a single child.")]
public sealed class BorderDemo : ControlsDemoBase
{
    public BorderDemo() : base(DemoSource.Get())
    {
    }

    public override Visual Build(DemoContext context)
    {
        _ = context;

        return new VStack(
                DemoUi.Hint("Border provides a simple padded container."),
                new Border("Padded content").Padding(1),
                new Border(new VStack("Line 1", "Line 2").Spacing(0)).Padding(2))
            .Spacing(1);
    }
}
