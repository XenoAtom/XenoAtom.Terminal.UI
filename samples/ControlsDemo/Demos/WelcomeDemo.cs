using System.Text;
using XenoAtom.Ansi;
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

        static Color Hue(float t)
        {
            // Simple HSV->RGB (s=1,v=1) for a pleasant spectrum.
            t = Math.Clamp(t, 0f, 1f) * 6f;
            var i = (int)t;
            var f = t - i;

            float r, g, b;
            switch (i)
            {
                case 0: r = 1f; g = f; b = 0f; break;
                case 1: r = 1f - f; g = 1f; b = 0f; break;
                case 2: r = 0f; g = 1f; b = f; break;
                case 3: r = 0f; g = 1f - f; b = 1f; break;
                case 4: r = f; g = 0f; b = 1f; break;
                default: r = 1f; g = 0f; b = 1f - f; break;
            }

            return Color.Rgb(
                (byte)(r * 255f + 0.5f),
                (byte)(g * 255f + 0.5f),
                (byte)(b * 255f + 0.5f));
        }

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

                for (var y = 0; y < ctx.Bounds.Height; y++)
                {
                    for (var x = 0; x < ctx.Bounds.Width; x++)
                    {
                        var t = ctx.Bounds.Width <= 1 ? 0f : x / (float)(ctx.Bounds.Width - 1);
                        var color = Hue(t);
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
