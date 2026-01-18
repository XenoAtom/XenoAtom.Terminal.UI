using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Styling;

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

        var labeled = new Group()
            .TopLeftText("TopLeft")
            .TopRightText("TopRight")
            .BottomLeftText("BottomLeft")
            .BottomRightText("BottomRight")
            .Padding(1)
            .Content(new VStack(
                    "Inside a group",
                    "…with corner labels")
                .Spacing(0));

        return new VStack(
                DemoUi.Hint("Group supports corner labels and a content area."),
                labeled,
                new Rule(),
                DemoUi.Hint("GroupStyle can override glyphs per instance:"),
                new HStack(
                        new Group("Single").Style(GroupStyle.Single).Content("Content"),
                        new Group("Rounded").Style(GroupStyle.Rounded).Content("Content"),
                        new Group("Double").Style(GroupStyle.Double).Content("Content"),
                        new Group("Heavy").Style(GroupStyle.Heavy).Content("Content"),
                        new Group("ASCII").Style(GroupStyle.Ascii).Content("Content"))
                    .Spacing(2))
            .Spacing(1);
    }
}
