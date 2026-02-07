using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.ControlsDemo.Demos;

[Demo("Placeholder", "Content", Description = "Lightweight placeholder surfaces with optional text and brush gradients.")]
public sealed class PlaceholderDemo : ControlsDemoBase
{
    public PlaceholderDemo() : base(DemoSource.Get())
    {
    }

    public override Visual Build(DemoContext context)
    {
        _ = context;

        var liveText = new State<string?>("Live gradient tile");

        var heroBackground = Brush.LinearGradient(
            new GradientPoint(0f, 0f),
            new GradientPoint(1f, 1f),
            [
                new GradientStop(0f, Colors.MidnightBlue),
                new GradientStop(1f, Colors.SlateBlue),
            ],
            mixSpaceOverride: ColorMixSpace.Oklab);

        var heroForeground = Brush.LinearGradient(
            new GradientPoint(0f, 0f),
            new GradientPoint(1f, 0f),
            [
                new GradientStop(0f, Colors.White),
                new GradientStop(1f, Colors.LightSkyBlue),
            ],
            mixSpaceOverride: ColorMixSpace.Oklab);

        var sweepBackground = Brush.LinearGradient(
            new GradientPoint(0f, 0f),
            new GradientPoint(1f, 0f),
            [
                new GradientStop(0f, Colors.DarkSlateBlue),
                new GradientStop(0.5f, Colors.SteelBlue),
                new GradientStop(1f, Colors.DarkCyan),
            ],
            mixSpaceOverride: ColorMixSpace.Oklab);

        var sweepForeground = Brush.LinearGradient(
            new GradientPoint(0f, 0f),
            new GradientPoint(1f, 0f),
            [
                new GradientStop(0f, Colors.NavajoWhite),
                new GradientStop(0.5f, Colors.White),
                new GradientStop(1f, Colors.LightCyan),
            ],
            mixSpaceOverride: ColorMixSpace.Oklab);

        return new VStack(
                DemoUi.Hint("A colorful placeholder mosaic: solid blocks, text labels, and brush gradients."),
                new Border(
                        new VStack(
                                new HStack(
                                        new Placeholder("Custom label for p1")
                                            .HorizontalAlignment(Align.Stretch)
                                            .MinWidth(28)
                                            .MinHeight(6)
                                            .Style(PlaceholderStyle.Default with
                                            {
                                                Background = Colors.Purple,
                                                Foreground = Colors.White,
                                                TextStyle = TextStyle.Bold,
                                            }),
                                        new VStack(
                                                new Placeholder("Placeholder p2")
                                                    .HorizontalAlignment(Align.Stretch)
                                                    .MinHeight(3)
                                                    .Style(PlaceholderStyle.Default with
                                                    {
                                                        Background = Colors.DarkMagenta,
                                                        Foreground = Colors.White,
                                                    }),
                                                new HStack(
                                                        new Placeholder("#p3")
                                                            .HorizontalAlignment(Align.Stretch)
                                                            .MinWidth(12)
                                                            .Style(PlaceholderStyle.Default with
                                                            {
                                                                Background = Colors.IndianRed,
                                                                Foreground = Colors.White,
                                                                TextStyle = TextStyle.Bold,
                                                            }),
                                                        new Placeholder("#p4")
                                                            .HorizontalAlignment(Align.Stretch)
                                                            .MinWidth(8)
                                                            .Style(PlaceholderStyle.Default with
                                                            {
                                                                Background = Colors.Peru,
                                                                Foreground = Colors.White,
                                                                TextStyle = TextStyle.Bold,
                                                            }),
                                                        new Placeholder("#p5")
                                                            .HorizontalAlignment(Align.Stretch)
                                                            .MinWidth(8)
                                                            .Style(PlaceholderStyle.Default with
                                                            {
                                                                Background = Colors.Olive,
                                                                Foreground = Colors.White,
                                                                TextStyle = TextStyle.Bold,
                                                            }),
                                                        new Placeholder("Small")
                                                            .HorizontalAlignment(Align.Stretch)
                                                            .MinWidth(9)
                                                            .Style(PlaceholderStyle.Default with
                                                            {
                                                                Background = Colors.DarkOliveGreen,
                                                                Foreground = Colors.White,
                                                            }))
                                                    .Spacing(0))
                                            .Spacing(0)
                                            .HorizontalAlignment(Align.Stretch))
                                    .Spacing(0),
                                new HStack(
                                        new Placeholder("26 x 6")
                                            .HorizontalAlignment(Align.Stretch)
                                            .MinWidth(18)
                                            .MinHeight(6)
                                            .Style(PlaceholderStyle.Default with
                                            {
                                                Background = Colors.SeaGreen,
                                                Foreground = Colors.White,
                                                TextStyle = TextStyle.Bold,
                                            }),
                                        new Placeholder(liveText)
                                            .HorizontalAlignment(Align.Stretch)
                                            .MinWidth(20)
                                            .MinHeight(6)
                                            .Style(PlaceholderStyle.Default with
                                            {
                                                ForegroundBrush = sweepForeground,
                                                BackgroundBrush = sweepBackground,
                                                TextStyle = TextStyle.Bold,
                                                Padding = new Thickness(1),
                                            }),
                                        new Placeholder("27 x 6")
                                            .HorizontalAlignment(Align.Stretch)
                                            .MinWidth(18)
                                            .MinHeight(6)
                                            .Style(PlaceholderStyle.Default with
                                            {
                                                Background = Colors.Teal,
                                                Foreground = Colors.White,
                                                TextStyle = TextStyle.Bold,
                                            }))
                                    .Spacing(0),
                                new HStack(
                                        new Placeholder("Large text tile for empty-state descriptions and mockup copy.")
                                            .HorizontalAlignment(Align.Stretch)
                                            .MinWidth(28)
                                            .MinHeight(6)
                                            .TextAlignment(TextAlignment.Left)
                                            .Style(PlaceholderStyle.Default with
                                            {
                                                Background = Colors.DarkCyan,
                                                Foreground = Colors.White,
                                                Padding = new Thickness(1),
                                            }),
                                        new Placeholder("40 x 6")
                                            .HorizontalAlignment(Align.Stretch)
                                            .MinWidth(28)
                                            .MinHeight(6)
                                            .Style(PlaceholderStyle.Default with
                                            {
                                                BackgroundBrush = heroBackground,
                                                ForegroundBrush = heroForeground,
                                                TextStyle = TextStyle.Bold,
                                            }))
                                    .Spacing(0))
                            .Spacing(0))
                    .Padding(0),
                new HStack(
                        DemoUi.Title("Editable gradient label:"),
                        new TextBox(liveText)
                            .Placeholder("Edit live tile text")
                            .MaxWidth(32))
                    .Spacing(1))
            .Spacing(1);
    }
}
