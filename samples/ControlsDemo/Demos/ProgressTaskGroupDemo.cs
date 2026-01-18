using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.ControlsDemo.Demos;

[Demo("ProgressTaskGroup", "Visualization", Description = "Multiple progress tasks with configurable columns.")]
public sealed class ProgressTaskGroupDemo : ControlsDemoBase
{
    public ProgressTaskGroupDemo() : base(DemoSource.Get())
    {
    }

    public override Visual Build(DemoContext context)
    {
        var progress = context.Runtime.Progress01;
        var pulse = context.Runtime.Pulse01;

        var download = new ProgressTask("🗃️  Download").Value(progress).StyleBar(ProgressBarStyle.Shaded with { Filled = CellStyle.None.WithForeground(AnsiColorScheme.RootLoopsDark.Red) });
        var render = new ProgressTask("🎨  Render").Value(pulse).StyleBar(ProgressBarStyle.Segmented with { Filled = CellStyle.None.WithForeground(AnsiColorScheme.RootLoopsDark.Green) });
        var tests = new ProgressTask("🧪  Tests").Value(progress);

        return new VStack(
                DemoUi.Hint("ProgressTaskGroup composes progress tasks using regular controls (label/bar/percent/spinner)."),
                new ProgressTaskGroup()
                    .Columns([
                        ProgressTaskColumns.Spinner(),
                        ProgressTaskColumns.Label(),
                        ProgressTaskColumns.Bar(),
                        ProgressTaskColumns.Percentage(),
                    ])
                    .Tasks([download, render, tests]))
            .Spacing(1);
    }
}
