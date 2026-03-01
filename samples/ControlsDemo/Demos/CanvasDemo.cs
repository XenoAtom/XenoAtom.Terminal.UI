using System.Text;
using XenoAtom.Ansi;
using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.ControlsDemo.Demos;

[Demo("Canvas", "Visualization", Description = "Immediate-mode cell drawing (lines, rectangles, circles, text).")]
public sealed class CanvasDemo : ControlsDemoBase
{
    public CanvasDemo() : base(DemoSource.Get())
    {
    }

    public override Visual Build(DemoContext context)
    {
        var tick = new State<int>(0);
        var useFinePixels = new State<bool>(true);

        void PaintCell(CanvasContext ctx)
        {
            var t = tick.Value;
            var bg = Style.None.WithBackground(Color.Rgb(0x12, 0x1A, 0x30));
            ctx.Clear(new Rune(' '), bg);

            var ink = Style.None.WithForeground(Color.Rgb(0x9E, 0xC7, 0xFC));
            ctx.DrawBox(0, 0, ctx.Size.Width, ctx.Size.Height, LineGlyphs.Single, ink);

            var accent = Style.None.WithForeground(Color.Rgb(0xE4, 0x9F, 0x27));
            var x0 = 2 + (t % 10);
            ctx.DrawLine(2, 2, x0, 10, accent);
            ctx.DrawHLine(4, 2, 10, new Rune('='), accent);
            ctx.DrawCircle(30, 8, 3, new Rune('o'), accent);
            ctx.WriteText(3, 13, "Custom draw ops: box/line/circle/text", ink);
        }

        void PaintFine(CanvasContext ctx)
        {
            var t = tick.Value;
            var bg = Style.None.WithBackground(Color.Rgb(0x12, 0x1A, 0x30));
            ctx.Clear(new Rune(' '), bg);

            var ink = Style.None.WithForeground(Color.Rgb(0x9E, 0xC7, 0xFC));
            ctx.DrawRect(0, 0, ctx.Size.Width, ctx.Size.Height, ink);

            var accent = Style.None.WithForeground(Color.Rgb(0xE4, 0x9F, 0x27));

            // Avoid rune-based overloads here so the demo shows the full extent of fine pixels.
            var x0 = 3 + (t % 12);
            ctx.DrawLine(2, 2, x0, 11, accent);
            ctx.DrawLine(2, 11, x0 + 4, 2, accent);
            ctx.DrawHLine(4, 3, 18, accent);
            ctx.DrawCircle(30, 8, 4, accent);
            ctx.DrawCircle(30, 8, 2, accent);

            ctx.WriteText(3, 13, "Fine pixels: same API, thinner strokes", ink);
        }

        return new VStack(
                DemoUi.Hint("Canvas draws directly into the CellBuffer during render via a Painter callback (cell + fine pixel mode)."),
                new HStack(
                        new CheckBox("Use fine pixels").IsChecked(useFinePixels),
                        new TextBlock(() => useFinePixels.Value ? "Fine: on (thin strokes)" : "Fine: off (cell strokes)"))
                    .Spacing(1),
                new VStack(
                        "Cell mode:",
                        new Canvas(PaintCell)
                            .UseFinePixels(false)
                            .MinWidth(44)
                            .MaxWidth(44)
                            .MinHeight(16)
                            .MaxHeight(16)
                            .Style(CanvasStyle.Default with { DefaultRune = new Rune('█') }),
                        "Fine pixel mode:",
                        new Canvas(PaintFine)
                            .UseFinePixels(useFinePixels)
                            .MinWidth(44)
                            .MaxWidth(44)
                            .MinHeight(16)
                            .MaxHeight(16)
                            .Style(CanvasStyle.Default with { DefaultRune = new Rune('█') }))
                    .Spacing(1),
                new HStack(
                        new Button("Tick").Click(() => tick.Value++),
                        new TextBlock(() => $"Tick: {tick.Value}"))
                    .Spacing(1))
            .Spacing(1);
    }
}

