using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.ControlsDemo.Demos;

[Demo("BreakdownChart", "Visualization", Description = "Segmented proportional bar with optional legend and tooltips.")]
public sealed class BreakdownDemo : ControlsDemoBase
{
    public BreakdownDemo() : base(DemoSource.Get())
    {
    }

    public override Visual Build(DemoContext context)
    {
        _ = context;

        var clicked = new State<string>("(none)");

        var breakdown = new BreakdownChart()
            .Title("Disk usage")
            .ShowValues(true)
            .ShowPercentages(true)
            .Segment(42, "🗃️  Data", color: Colors.DodgerBlue, tooltip: new Markup("[primary]Data[/] files and databases."))
            .Segment(18, "📦  Packages", color: Colors.LimeGreen, tooltip: new Markup("[success]Packages[/] in the cache."))
            .Segment(9, "🧹  Temp", color: Colors.Orange, tooltip: new Markup("[warning]Temporary[/] files."))
            .Segment(3, "🧯  Other", color: Colors.IndianRed, tooltip: new Markup("[error]Other[/] space usage."));

        breakdown.SegmentClicked((_, e) => clicked.Value = $"Clicked segment {e.Index}: {breakdown.ToStringValue(e.Segment.Value)}");

        return new VStack(
                DemoUi.Hint("BreakdownChart shows the proportional distribution of values as colored segments."),
                breakdown.Style(new BreakdownStyle { SegmentGap = 1 }),
                new Rule(),
                new TextBlock(() => $"Last click: {clicked.Value}"))
            .Spacing(1);
    }
}
