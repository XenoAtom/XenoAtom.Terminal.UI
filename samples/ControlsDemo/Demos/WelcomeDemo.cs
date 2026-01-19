using XenoAtom.Ansi;
using System.Text;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Figlet;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Styling;

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

        var schemePanel = BuildSchemePanel(DemoThemes.Dark);

        return new VStack(
                banner,
                title,
                new Rule(),
                hints,
                new Rule(),
                new Group()
                    .TopLeftText("Spectrum")
                    .Padding(1)
                    .Content(spectrum).VerticalAlignment(VerticalAlignment.Stretch).HorizontalAlignment(HorizontalAlignment.Stretch),
                schemePanel)
            .Spacing(1);
    }

    private static Visual BuildSchemePanel(Theme theme)
    {
        var scheme = theme.Scheme;
        if (scheme is null)
        {
            return (Visual)"[dim]No ColorScheme available for this theme.[/]";
        }

        var table = new Table()
            .Headers("Name", "Sample", "Value");

        void Add(string name, Color? color)
        {
            table.AddRow(
                new TextBlock(name),
                CreateSwatch(theme, color),
                new Markup($"[dim]{FormatColor(color)}[/]") { Wrap = false });
        }

        Add("Background", scheme.Background);
        Add("Foreground", scheme.Foreground);
        Add("Black", scheme.Black);
        Add("Red", scheme.Red);
        Add("Green", scheme.Green);
        Add("Yellow", scheme.Yellow);
        Add("Blue", scheme.Blue);
        Add("Purple", scheme.Purple);
        Add("Cyan", scheme.Cyan);
        Add("White", scheme.White);
        Add("BrightBlack", scheme.BrightBlack);
        Add("BrightRed", scheme.BrightRed);
        Add("BrightGreen", scheme.BrightGreen);
        Add("BrightYellow", scheme.BrightYellow);
        Add("BrightBlue", scheme.BrightBlue);
        Add("BrightPurple", scheme.BrightPurple);
        Add("BrightCyan", scheme.BrightCyan);
        Add("BrightWhite", scheme.BrightWhite);

        return new Group()
            .TopLeftText($"Scheme: {scheme.Name}")
            .Padding(1)
            .Content(table);
    }

    private static Visual CreateSwatch(Theme theme, Color? color)
    {
        const int width = 10;
        const int height = 1;

        return new Canvas()
            .MinWidth(width)
            .MaxWidth(width)
            .MinHeight(height)
            .MaxHeight(height)
            .Painter(ctx =>
            {
                var style = theme.BaseTextStyle();
                if (color is { } c && c.Kind != ColorKind.Default)
                {
                    style = style.WithBackground(c);
                }

                for (var y = 0; y < ctx.Bounds.Height; y++)
                {
                    for (var x = 0; x < ctx.Bounds.Width; x++)
                    {
                        ctx.SetPixel(x, y, new Rune(' '), style);
                    }
                }
            });
    }

    private static string FormatColor(Color? color)
    {
        if (color is null)
        {
            return "Default";
        }

        var c = color.Value;
        return c.Kind switch
        {
            ColorKind.Default => "Default",
            ColorKind.Basic16 => $"Basic16({c.Index}) {c.ToHexString()}",
            ColorKind.Indexed256 => $"Indexed256({c.Index}) {c.ToHexString()}",
            ColorKind.Rgb => c.ToHexString(),
            ColorKind.RgbA => c.ToHexString(includeAlpha: true),
            _ => c.ToHexString(),
        };
    }
}
