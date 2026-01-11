using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.ControlsDemo.Demos;

[Demo("ProgressBar", "Visualization", Description = "Animated progress + style variants.")]
public sealed class ProgressBarDemo : ControlsDemoBase
{
    public ProgressBarDemo() : base(DemoSource.Get())
    {
    }

    public override Visual Build(DemoContext context)
    {
        var progress = context.Runtime.Progress01;

        return new VStack(
                DemoUi.Hint("ProgressBar is bindable; any value update triggers a render."),
                new ProgressBar().Label("Thin").Value(progress).Style(ProgressBarStyle.Thin),
                new ProgressBar().Label("Segmented").Value(progress).Style(ProgressBarStyle.Segmented),
                new ProgressBar().Label("Shaded").Value(progress).Style(ProgressBarStyle.Shaded),
                new ProgressBar().Label("Bracketed").Value(progress).Style(ProgressBarStyle.Bracketed))
            .Spacing(1);
    }
}

