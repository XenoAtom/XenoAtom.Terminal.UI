using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;

namespace XenoAtom.Terminal.UI.ControlsDemo.Demos;

[Demo("Sparkline", "Visualization", Description = "Compact inline chart.")]
public sealed class SparklineDemo : ControlsDemoBase
{
    public SparklineDemo() : base(DemoSource.Get())
    {
    }

    public override Visual Build(DemoContext context)
    {
        _ = context;

        var values = new double[] {  };

        return new VStack(
                DemoUi.Hint("Sparkline is great for inline trends."),
                new Sparkline().Values([0, 1, 3, 2, 5, 4, 6, 7, 3, 2, 4, 5, 8, 7, 6, 9])
            )
            .Spacing(1);
    }
}

