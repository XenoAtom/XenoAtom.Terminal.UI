using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;

namespace XenoAtom.Terminal.UI.ControlsDemo.Demos;

[Demo("VStack", "Layout", Description = "Vertical stacking with spacing.")]
public sealed class VStackDemo : ControlsDemoBase
{
    public VStackDemo() : base(DemoSource.Get())
    {
    }

    public override Visual Build(DemoContext context)
    {
        _ = context;

        return new VStack(
                DemoUi.Hint("VStack stacks children vertically. Spacing controls the gap between items."),
                new VStack(
                        "Item 1",
                        "Item 2",
                        "Item 3")
                    .Spacing(1),
                new Rule(),
                DemoUi.Title("Spacing = 0"),
                new VStack("A", "B", "C").Spacing(0))
            .Spacing(1);
    }
}

