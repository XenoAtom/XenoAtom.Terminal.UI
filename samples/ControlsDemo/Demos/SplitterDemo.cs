using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;

namespace XenoAtom.Terminal.UI.ControlsDemo.Demos;

[Demo("Splitters", "Layout", Description = "HSplitter and VSplitter for resizable panes.")]
public sealed class SplitterDemo : ControlsDemoBase
{
    public SplitterDemo() : base(DemoSource.Get())
    {
    }

    public override Visual Build(DemoContext context)
    {
        _ = context;

        return new VStack(
                DemoUi.Hint("Drag the splitter bar with the mouse."),
                new HSplitter(
                        new Border("Left").Padding(1),
                        new Border("Right").Padding(1))
                    .Ratio(0.35)
                    .MinHeight(6)
                    .MaxHeight(6),
                new Rule(),
                new VSplitter(
                        new Border("Top").Padding(1),
                        new Border("Bottom").Padding(1))
                    .Ratio(0.5)
                    .MinHeight(8)
                    .MaxHeight(8))
            .Spacing(1);
    }
}
