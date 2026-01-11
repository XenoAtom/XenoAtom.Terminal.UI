using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;

namespace XenoAtom.Terminal.UI.ControlsDemo.Demos;

[Demo("Collapsible", "Layout", Description = "Single collapsible region.")]
public sealed class CollapsibleDemo : ControlsDemoBase
{
    public CollapsibleDemo() : base(DemoSource.Get())
    {
    }

    public override Visual Build(DemoContext context)
    {
        _ = context;

        var c = new Collapsible()
            .Header("Click to expand/collapse")
            .IsExpanded(true)
            .Content(new VStack(
                    "Line 1",
                    "Line 2",
                    "Line 3")
                .Spacing(0));

        return new VStack(
                DemoUi.Hint("Collapsible is useful for progressive disclosure."),
                c)
            .Spacing(1);
    }
}

