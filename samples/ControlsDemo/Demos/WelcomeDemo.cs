using XenoAtom.Ansi;
using System.Text;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Figlet;
using XenoAtom.Terminal.UI.Geometry;

namespace XenoAtom.Terminal.UI.ControlsDemo.Demos;

[Demo("Welcome", "Welcome", Description = "Welcome screen and quick navigation tips.", Order = -1000)]
public sealed class WelcomeDemo : ControlsDemoBase
{
    public WelcomeDemo() : base(DemoSource.Get())
    {
    }

    public override Visual Build(DemoContext context)
    {
        _ = context;

        var banner = new TextFiglet("Welcome")
            .Font(FigletPredefinedFont.Slant)
            .LetterSpacing(1)
            .TextAlignment(TextAlignment.Left);

        var title = new Markup("[bold]XenoAtom.Terminal.UI[/]") { Wrap = false };

        var hints = new Markup("""
            [bold]Tips[/]
            - Use the [bold]Search[/] box to filter controls and demos.
            - Use [bold]Tab[/] to move focus and [bold]Mouse wheel[/] to scroll lists.
            - Use [bold]Ctrl+Q[/] to quit.
            """)
        { Wrap = true };

        var spectrum = new Canvas()
            .MinHeight(6)
            .Painter(ctx =>
            {
                var theme = DemoThemes.Dark;
                var baseStyle = theme.BaseTextStyle();
                var width = Math.Max(1, ctx.Bounds.Width);
                var height = Math.Max(1, ctx.Bounds.Height);

                // Hue across X, lightness down Y (similar to typical "spectrum" palettes).
                const float chroma = 0.18f;
                const float topLightness = 0.92f;
                const float bottomLightness = 0.25f;

                for (var y = 0; y < height; y++)
                {
                    var yt = height <= 1 ? 0f : y / (float)(height - 1);
                    var l = topLightness + ((bottomLightness - topLightness) * yt);

                    for (var x = 0; x < width; x++)
                    {
                        var xt = width <= 1 ? 0f : x / (float)(width - 1);
                        var hueDegrees = xt * 360f;
                        var color = Color.FromOklch(l, chroma, hueDegrees);
                        var style = baseStyle.WithBackground(color);
                        ctx.SetPixel(x, y, new Rune(' '), style);
                    }
                }
            }).HorizontalAlignment(HorizontalAlignment.Stretch).VerticalAlignment(VerticalAlignment.Stretch);

        return new VStack(
                banner,
                title,
                new Rule(),
                hints,
                new Rule(),
                new Group()
                    .TopLeftText("Spectrum")
                    .Padding(1)
                    .Content(spectrum).VerticalAlignment(VerticalAlignment.Stretch).HorizontalAlignment(HorizontalAlignment.Stretch))
            .Spacing(1);
    }
}
