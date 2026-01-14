using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;

namespace XenoAtom.Terminal.UI.ControlsDemo.Demos;

[Demo("LineChart", "Visualization", Description = "Simple line chart control.")]
public sealed class LineChartDemo : ControlsDemoBase
{
    public LineChartDemo() : base(DemoSource.Get())
    {
    }

    public override Visual Build(DemoContext context)
    {
        _ = context;

        return new VStack(
                DemoUi.Hint("LineChart renders a trend line."),
                new LineChart().Values([1, 4, 2, 5, 3, 6, 4, 7]).MinHeight(4).MaxHeight(4))
            .Spacing(1);
    }
}

