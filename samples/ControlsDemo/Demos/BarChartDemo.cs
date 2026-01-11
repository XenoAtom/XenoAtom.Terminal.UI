using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;

namespace XenoAtom.Terminal.UI.ControlsDemo.Demos;

[Demo("BarChart", "Visualization", Description = "Simple bar chart control.")]
public sealed class BarChartDemo : ControlsDemoBase
{
    public BarChartDemo() : base(DemoSource.Get())
    {
    }

    public override Visual Build(DemoContext context)
    {
        _ = context;

        var values = new double[] { 1, 4, 2, 5, 3, 6 };

        return new VStack(
                DemoUi.Hint("BarChart shows values as bars (horizontal or vertical)."),
                new BarChart { Values = values }.MinHeight(4).MaxHeight(4),
                new Rule(),
                new BarChart { Values = values }.Orientation(Orientation.Vertical).MinHeight(4).MaxHeight(4))
            .Spacing(1);
    }
}

