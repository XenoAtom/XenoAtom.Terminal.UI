using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;

namespace XenoAtom.Terminal.UI.ControlsDemo.Demos;

[Demo("Center", "Layout", Description = "Centers a single child within the available bounds.")]
public sealed class CenterDemo : ControlsDemoBase
{
    public CenterDemo() : base(DemoSource.Get())
    {
    }

    public override Visual Build(DemoContext context)
    {
        _ = context;

        return new VStack(
                DemoUi.Hint("Center measures its content and then places it centered during arrange."),
                new Border(
                        new Center()
                            .Content(new Border("Centered").Padding(1)))
                    .MinWidth(36)
                    .MaxWidth(36)
                    .MinHeight(9)
                    .MaxHeight(9)
                    .Padding(1))
            .Spacing(1);
    }
}

