using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;

namespace XenoAtom.Terminal.UI.ControlsDemo.Demos;

[Demo("Accordion", "Layout", Description = "Multiple collapsible sections.")]
public sealed class AccordionDemo : ControlsDemoBase
{
    public AccordionDemo() : base(DemoSource.Get())
    {
    }

    public override Visual Build(DemoContext context)
    {
        _ = context;

        var state = new State<bool>(false);

        var accordion = new Accordion(
                new Collapsible().Header("Section A").IsExpanded(true).Content("Content A"),
                new Collapsible().Header("Section B").Content("Content B"),
                new Collapsible().Header("Section C").Content("Content C"))
            .Spacing(1)
            .SingleExpanded(state);

        return new VStack(
                DemoUi.Hint("Accordion provides multiple collapsible sections."),
                new HStack(new CheckBox("Single Expanded").IsChecked(state)),
                accordion)
            .Spacing(1);
    }
}
