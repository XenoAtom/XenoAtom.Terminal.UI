using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.ControlsDemo.Demos;

[Demo("Spinner", "Visualization", Description = "Animated spinner styles (single and multi-rune frames).")]
public sealed class SpinnerDemo : ControlsDemoBase
{
    public SpinnerDemo() : base(DemoSource.Get())
    {
    }

    public override Visual Build(DemoContext context)
    {
        _ = context;

        return new VStack(
                DemoUi.Hint("Spinners animate automatically; styles can be swapped via SpinnerStyle."),
                new HStack(
                        new Spinner().Style(SpinnerStyles.Dots),
                        "Loading…")
                    .Spacing(1),
                new HStack(
                        new Spinner().Style(SpinnerStyles.DotsBounce),
                        "Bouncing…")
                    .Spacing(1),
                new HStack(
                        new Spinner().Style(SpinnerStyles.BoxSlide),
                        "Wide frames…")
                    .Spacing(1),
                new Rule(),
                DemoUi.Title("Tone"),
                new HStack(
                        new Spinner().Tone(ControlTone.Primary).Style(SpinnerStyles.Arc),
                        new Spinner().Tone(ControlTone.Success).Style(SpinnerStyles.Arc),
                        new Spinner().Tone(ControlTone.Warning).Style(SpinnerStyles.Arc),
                        new Spinner().Tone(ControlTone.Error).Style(SpinnerStyles.Arc))
                    .Spacing(1))
            .Spacing(1);
    }
}
