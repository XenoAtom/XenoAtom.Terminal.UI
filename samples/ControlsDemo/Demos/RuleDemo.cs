using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;

namespace XenoAtom.Terminal.UI.ControlsDemo.Demos;

[Demo("Rule", "Content", Description = "Horizontal/vertical rules with optional labels.")]
public sealed class RuleDemo : ControlsDemoBase
{
    public RuleDemo() : base(DemoSource.Get())
    {
    }

    public override Visual Build(DemoContext context)
    {
        _ = context;

        return new VStack(
                DemoUi.Hint("Rules fill the available space (via layout stretch/flex)."),
                new Rule().StartLabel("Start").CenterLabel("Center").EndLabel("End"),
                new Rule().Orientation(Orientation.Vertical).MinHeight(6).MaxHeight(6))
            .Spacing(1);
    }
}

