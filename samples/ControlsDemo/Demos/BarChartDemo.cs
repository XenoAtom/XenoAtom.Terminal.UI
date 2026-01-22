using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.ControlsDemo.Demos;

[Demo("BarChart", "Visualization", Description = "Horizontal bar chart with labels and optional value display.")]
public sealed class BarChartDemo : ControlsDemoBase
{
    public BarChartDemo() : base(DemoSource.Get())
    {
    }

    public override Visual Build(DemoContext context)
    {
        _ = context;

        var showValues = new State<bool>(true);
        var showPercentages = new State<bool>(false);

        var chart = new BarChart()
            .Title("Distribution")
            .ShowValues(showValues)
            .ShowPercentages(showPercentages)
            .Minimum(0)
            .Maximum(10)
            .Items(
                new BarChartItem("Alpha", 8) { BarColor = Colors.DodgerBlue },
                new BarChartItem("Beta", 5) { BarColor = Colors.LimeGreen },
                new BarChartItem("Gamma", 2) { BarColor = Colors.Orange },
                new BarChartItem("Delta", 1) { BarColor = Colors.IndianRed });

        return new VStack(
                DemoUi.Hint("BarChart displays items as horizontal bars. Use Value/Percentage toggles to control the right column."),
                new HStack(
                        new CheckBox().Text("Show values").IsChecked(showValues),
                        new CheckBox().Text("Show %").IsChecked(showPercentages))
                    .Spacing(2),
                chart)
            .Spacing(1);
    }
}
