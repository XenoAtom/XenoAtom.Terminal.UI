using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;

namespace XenoAtom.Terminal.UI.ControlsDemo.Demos;

[Demo("DockLayout", "Layout", Description = "Top/Bottom/Content docking.")]
public sealed class DockLayoutDemo : ControlsDemoBase
{
    public DockLayoutDemo() : base(DemoSource.Get())
    {
    }

    public override Visual Build(DemoContext context)
    {
        _ = context;

        var layout = new DockLayout()
            .Top(new Border("Top").Padding(1))
            .Bottom(new Border("Bottom").Padding(1))
            .Content(new Border("Content").Padding(1));

        return new VStack(
                DemoUi.Hint("DockLayout reserves space for Top and Bottom, and gives remaining space to Content."),
                layout.MinHeight(8).MaxHeight(8))
            .Spacing(1);
    }
}
