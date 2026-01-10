using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.ControlsDemo.Demos;

[Demo("Visualization", "Visualization", Description = "Rule, ProgressBar variants, Spinner styles, sparklines and charts.", Tags = ["ProgressBar", "Spinner", "Sparkline", "BarChart", "LineChart", "Rule"], Order = 0)]
public sealed class VisualizationDemo : ControlsDemoBase
{
    public VisualizationDemo() : base(DemoSource.Get())
    {
    }

    public override Visual Build(DemoContext context)
    {
        // A small rolling buffer driven by the demo runtime.
        var values = new double[80];

        var history = new ComputedVisual(() =>
        {
            var _ = context.Runtime.Frame.Value;
            Array.Copy(values, 1, values, 0, values.Length - 1);
            values[^1] = context.Runtime.Progress01.Value;

            return new VStack(
                new Sparkline { Values = values }.HorizontalAlignment(HorizontalAlignment.Stretch),
                new BarChart { Values = values }.Orientation(Orientation.Vertical).MinHeight(4).MaxHeight(4).HorizontalAlignment(HorizontalAlignment.Stretch),
                new LineChart { Values = values }.MinHeight(4).MaxHeight(4).HorizontalAlignment(HorizontalAlignment.Stretch))
            {
                Spacing = 1,
                HorizontalAlignment = HorizontalAlignment.Stretch,
            };
        });

        var progress = context.Runtime.Progress01;

        var progressVariants = new VStack(
                new ProgressBar().Label("Thin").Value(progress).HorizontalAlignment(HorizontalAlignment.Stretch).Style(ProgressBarStyle.Thin),
                new ProgressBar().Label("Segmented").Value(progress).HorizontalAlignment(HorizontalAlignment.Stretch).Style(ProgressBarStyle.Segmented),
                new ProgressBar().Label("Shaded").Value(progress).HorizontalAlignment(HorizontalAlignment.Stretch).Style(ProgressBarStyle.Shaded),
                new ProgressBar().Label("Bracketed").Value(progress).HorizontalAlignment(HorizontalAlignment.Stretch).Style(ProgressBarStyle.Bracketed))
            .Spacing(1)
            .HorizontalAlignment(HorizontalAlignment.Stretch);

        var spinners = new HStack(
                new Spinner().Style(SpinnerStyles.Dots),
                new Spinner().Style(SpinnerStyles.BouncingBall),
                new Spinner().Style(SpinnerStyles.Star),
                new Spinner().Style(SpinnerStyles.Grow2))
            .Spacing(2);

        return new VStack(
                new Group().TopLeftText("Rule").Padding(1).HorizontalAlignment(HorizontalAlignment.Stretch).Content(new VStack(
                        new Rule(),
                        new Markup("[dim]Use rules to separate sections.[/]").Wrap(true))
                    .Spacing(1)),
                new Group().TopLeftText("ProgressBar").Padding(1).HorizontalAlignment(HorizontalAlignment.Stretch).Content(progressVariants),
                new Group().TopLeftText("Spinner").Padding(1).HorizontalAlignment(HorizontalAlignment.Stretch).Content(spinners),
                new Group().TopLeftText("Charts").Padding(1).HorizontalAlignment(HorizontalAlignment.Stretch).Content(history))
            .Spacing(1)
            .HorizontalAlignment(HorizontalAlignment.Stretch);
    }
}
