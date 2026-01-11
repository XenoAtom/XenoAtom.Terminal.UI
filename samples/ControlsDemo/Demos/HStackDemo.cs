using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;

namespace XenoAtom.Terminal.UI.ControlsDemo.Demos;

[Demo("HStack", "Layout", Description = "Horizontal stacking with spacing.")]
public sealed class HStackDemo : ControlsDemoBase
{
    public HStackDemo() : base(DemoSource.Get())
    {
    }

    public override Visual Build(DemoContext context)
    {
        _ = context;

        return new VStack(
                DemoUi.Hint("HStack stacks children horizontally."),
                new HStack("One", "Two", "Three").Spacing(2),
                new Rule(),
                DemoUi.Title("Mixed controls"),
                new HStack(
                        new Button("Left"),
                        new TextBox().Text("Middle"),
                        new Button("Right"))
                    .Spacing(1))
            .Spacing(1);
    }
}

