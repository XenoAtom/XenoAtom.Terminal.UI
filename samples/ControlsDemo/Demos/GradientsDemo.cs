using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Figlet;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.ControlsDemo.Demos;

[Demo("Gradients", "Visualization", Description = "Gradient brushes for TextBlock, TextBox, and TextFiglet.")]
public sealed class GradientsDemo : ControlsDemoBase
{
    public GradientsDemo() : base(DemoSource.Get())
    {
    }

    public override Visual Build(DemoContext context)
    {
        var text = new State<string?>("Gradient-enabled TextBox");
        var figletText = new State<string?>("XenoAtom");

        var inputBackgroundBrush = Brush.LinearGradient(
            new GradientPoint(0f, 0f),
            new GradientPoint(1f, 0f),
            [
                new GradientStop(0f, Colors.MidnightBlue),
                new GradientStop(0.5f, Colors.SteelBlue),
                new GradientStop(1f, Colors.DarkSlateBlue),
            ],
            mixSpaceOverride: ColorMixSpace.Oklab);

        var inputForegroundBrush = Brush.LinearGradient(
            new GradientPoint(0f, 0f),
            new GradientPoint(1f, 0f),
            [
                new GradientStop(0f, Colors.NavajoWhite),
                new GradientStop(0.5f, Colors.White),
                new GradientStop(1f, Colors.LightSkyBlue),
            ],
            mixSpaceOverride: ColorMixSpace.Oklab);

        var titleForegroundBrush = Brush.LinearGradient(
            new GradientPoint(0f, 0f),
            new GradientPoint(1f, 0f),
            [
                new GradientStop(0f, Colors.SkyBlue),
                new GradientStop(0.5f, Colors.White),
                new GradientStop(1f, Colors.Plum),
            ],
            mixSpaceOverride: ColorMixSpace.Oklab);

        var titleBackgroundBrush = Brush.LinearGradient(
            new GradientPoint(0f, 0f),
            new GradientPoint(1f, 0f),
            [
                new GradientStop(0f, Colors.MidnightBlue),
                new GradientStop(1f, Colors.DarkSlateBlue),
            ],
            mixSpaceOverride: ColorMixSpace.Oklab);

        return new VStack(
                DemoUi.Hint("Brushes are opt-in. This demo combines static and animated gradients on TextBlock, TextBox, and TextFiglet."),
                DemoUi.Title("TextBlock gradient"),
                new TextBlock("Gradient title using TextBlockStyle brushes")
                    .HorizontalAlignment(Align.Stretch)
                    .Style(TextBlockStyle.Default with
                    {
                        ForegroundBrush = titleForegroundBrush,
                        BackgroundBrush = titleBackgroundBrush,
                        FillBackground = true,
                        TextStyle = TextStyle.Bold,
                    })
                    .MaxWidth(56),
                DemoUi.Title("TextBox gradient"),
                new TextBox()
                    .Text(text)
                    .Placeholder("Type to see the gradient foreground")
                    .HorizontalAlignment(Align.Stretch)
                    .Style(TextBoxStyle.Default with
                    {
                        BackgroundBrush = inputBackgroundBrush,
                        ForegroundBrush = inputForegroundBrush,
                    }).MaxWidth(30),
                new TextBlock(() => $"Value: {text.Value}"),
                new Rule(),
                DemoUi.Title("Animated TextFiglet gradient"),
                new TextBox().Text(figletText).Placeholder("Banner text").MinWidth(20).MaxWidth(28),
                new Border(
                    new TextFiglet()
                        .Text(figletText)
                        .Font(FigletPredefinedFont.Standard)
                        .LetterSpacing(1)
                        .TextAlignment(TextAlignment.Left)
                        .Style(() =>
                        {
                            var pulse = context.Runtime.Pulse01.Value;
                            var startX = (float)(-0.40 + pulse);
                            var endX = (float)(0.40 + pulse);
                            var sweepBrush = Brush.LinearGradient(
                                new GradientPoint(startX, 0f),
                                new GradientPoint(endX, 1f),
                                [
                                    new GradientStop(0f, Colors.DodgerBlue.WithOpacity(0.25f)),
                                    new GradientStop(0.5f, Colors.White),
                                    new GradientStop(1f, Colors.Orange.WithOpacity(0.25f)),
                                ],
                                tileMode: BrushTileMode.Mirror,
                                mixSpaceOverride: ColorMixSpace.Oklab);

                            return TextFigletStyle.Default with { ForegroundBrush = sweepBrush };
                        }))
                    .Padding(1))
            .Spacing(1);
    }
}
