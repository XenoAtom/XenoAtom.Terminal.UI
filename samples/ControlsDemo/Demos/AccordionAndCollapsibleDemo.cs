using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;

namespace XenoAtom.Terminal.UI.ControlsDemo.Demos;

[Demo("Accordion and collapsible", "Layout", Description = "Progressive disclosure with Collapsible and Accordion.", Tags = ["Collapsible", "Accordion"], Order = 10)]
public sealed class AccordionAndCollapsibleDemo : ControlsDemoBase
{
    public AccordionAndCollapsibleDemo() : base(DemoSource.Get())
    {
    }

    public override Visual Build(DemoContext context)
    {
        var collapsible = new Collapsible()
            .Header("Advanced options")
            .IsExpanded(true)
            .Content(new VStack(
                    new CheckBox("Enable feature X", isChecked: true),
                    new CheckBox("Enable feature Y"),
                    new Slider().Minimum(0).Maximum(10).Value(4).ShowValueLabel(true))
                .Spacing(1)
                .HorizontalAlignment(HorizontalAlignment.Stretch));

        collapsible.ExpandedChanged((_, e) => context.Log($"Collapsible expanded: {e.NewValue}"));

        var accordion = new Accordion();
        accordion.Add(
            new Collapsible()
                .Header("General")
                .IsExpanded(true)
                .Content(new Markup("[dim]General settings here…[/]")),
            new Collapsible()
                .Header("Editor")
                .Content(new Markup("[dim]Editor settings here…[/]")),
            new Collapsible()
                .Header("Build")
                .Content(new Markup("[dim]Build settings here…[/]")));

        return new VStack(
                new Group().TopLeftText("Collapsible").Padding(1).HorizontalAlignment(HorizontalAlignment.Stretch).Content(collapsible),
                new Group().TopLeftText("Accordion").Padding(1).HorizontalAlignment(HorizontalAlignment.Stretch).Content(accordion))
            .Spacing(1)
            .HorizontalAlignment(HorizontalAlignment.Stretch);
    }
}
