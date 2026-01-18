using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Styling;

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
                new Border(new VStack("Line 1", "Line 2").Spacing(0)).Padding(2),
                new Rule(),
                DemoUi.Hint("BorderStyle can override glyphs per instance:"),
                new HStack(
                        new Border("Single").Style(BorderStyle.Single).Padding(1),
                        new Border("Rounded").Style(BorderStyle.Rounded).Padding(1),
                        new Border("Double").Style(BorderStyle.Double).Padding(1),
                        new Border("Heavy").Style(BorderStyle.Heavy).Padding(1),
                        new Border("ASCII").Style(BorderStyle.Ascii).Padding(1))
                    .Spacing(2))
            .Spacing(1);
    }
}
