using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;

namespace XenoAtom.Terminal.UI.ControlsDemo.Demos;

[Demo("Group", "Layout", Description = "Border with corner labels (top/bottom left/right).")]
public sealed class GroupDemo : ControlsDemoBase
{
    public GroupDemo() : base(DemoSource.Get())
    {
    }

    public override Visual Build(DemoContext context)
    {
        _ = context;

        return new VStack(
                DemoUi.Hint("Group supports corner labels and a content area."),
                new Group()
                    .TopLeftText("TopLeft")
                    .TopRightText("TopRight")
                    .BottomLeftText("BottomLeft")
                    .BottomRightText("BottomRight")
                    .Padding(1)
                    .Content(new VStack(
                            "Inside a group",
                            "…with corner labels")
                        .Spacing(0)))
            .Spacing(1);
    }
}

