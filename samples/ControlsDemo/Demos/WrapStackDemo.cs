using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.ControlsDemo.Demos;

[Demo("WrapStack", "Layout", Description = "WrapHStack and WrapVStack for flow-style layout with justification.")]
public sealed class WrapStackDemo : ControlsDemoBase
{
    public WrapStackDemo() : base(DemoSource.Get())
    {
    }

    public override Visual Build(DemoContext context)
    {
        _ = context;

        var justify = new State<WrapJustify>(WrapJustify.Start);
        var measureMode = new State<WrapMeasureMode>(WrapMeasureMode.ConstrainToRun);
        var spacing = new State<int>(1);
        var runSpacing = new State<int>(1);

        var chips = new Visual[]
        {
            Chip("alpha"),
            Chip("beta"),
            Chip("gamma"),
            Chip("delta"),
            Chip("epsilon"),
            Chip("zeta"),
            Chip("eta"),
            Chip("theta"),
            Chip("iota"),
            Chip("kappa"),
        };

        var wrapH = new Border(
                new WrapHStack(chips)
                    .Spacing(spacing)
                    .RunSpacing(runSpacing)
                    .Justify(justify)
                    .MeasureMode(measureMode)
                    .HorizontalAlignment(Align.Stretch))
            .Padding(new Thickness(1, 0, 1, 0))
            .Style(BorderStyle.Rounded);

        var wrapV = new Border(
                new WrapVStack(chips)
                    .Spacing(spacing)
                    .RunSpacing(runSpacing)
                    .Justify(justify)
                    .MeasureMode(measureMode)
                    .VerticalAlignment(Align.Stretch))
            .MinHeight(10)
            .Padding(new Thickness(1, 0, 1, 0))
            .Style(BorderStyle.Rounded);

        return new VStack(
                DemoUi.Hint("Wrap stacks flow children into runs (rows or columns) when there is not enough space."),
                new HStack(
                        DemoUi.Title("Justify"),
                        new EnumSelect<WrapJustify>().Value(justify),
                        DemoUi.Title("Measure mode"),
                        new EnumSelect<WrapMeasureMode>().Value(measureMode))
                    .Spacing(2),
                new HStack(
                        DemoUi.Title("Spacing"),
                        new Slider<int> { Minimum = 0, Maximum = 4, Step = 1 }.Value(spacing),
                        DemoUi.Title("Run spacing"),
                        new Slider<int> { Minimum = 0, Maximum = 4, Step = 1 }.Value(runSpacing))
                    .Spacing(2),
                new Rule(),
                DemoUi.Title("WrapHStack (rows)"),
                wrapH,
                new Rule(),
                DemoUi.Title("WrapVStack (columns)"),
                wrapV)
            .Spacing(1);
    }

    private static Visual Chip(string text)
        => new Border(text)
            .Padding(new Thickness(1, 0, 1, 0))
            .Style(BorderStyle.Rounded);
}
