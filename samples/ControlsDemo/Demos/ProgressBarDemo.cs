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

        Visual Row(string label, ProgressBarStyle style)
            => new HStack(
                    label,
                    new ProgressBar().Value(progress).HorizontalAlignment(HorizontalAlignment.Stretch).Style(style),
                    new TextBlock(() => $"{(int)(progress.Value * 100),3}%"))
                .Spacing(1)
                .HorizontalAlignment(HorizontalAlignment.Stretch);

        return new VStack(
                DemoUi.Hint("ProgressBar is bindable; any value update triggers a render."),
                Row("Thin", ProgressBarStyle.Thin),
                Row("Segmented", ProgressBarStyle.Segmented),
                Row("Shaded", ProgressBarStyle.Shaded),
                Row("Bracketed", ProgressBarStyle.Bracketed))
            .Spacing(1);
    }
}
