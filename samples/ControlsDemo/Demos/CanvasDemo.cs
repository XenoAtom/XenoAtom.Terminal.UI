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
        _ = context;

        var tick = new State<int>(0);

        return new VStack(
                DemoUi.Hint("Canvas draws directly into the CellBuffer during render via a Painter callback."),
                new Canvas()
                    .Painter(ctx =>
                    {
                        var t = tick.Value;
                        var bg = CellStyle.None.WithBackground(AnsiColor.Rgb(0x12, 0x1A, 0x30));
                        ctx.Clear(new Rune(' '), bg);

                        var ink = CellStyle.None.WithForeground(AnsiColor.Rgb(0x9E, 0xC7, 0xFC));
                        ctx.DrawBox(0, 0, ctx.Size.Width, ctx.Size.Height, LineGlyphs.Single, ink);

                        var accent = CellStyle.None.WithForeground(AnsiColor.Rgb(0xE4, 0x9F, 0x27));
                        var x0 = 2 + (t % 10);
                        ctx.DrawLine(2, 2, x0, 10, accent);
                        ctx.DrawHLine(4, 2, 10, new Rune('='), accent);
                        ctx.DrawCircle(30, 8, 3, new Rune('o'), accent);
                        ctx.WriteText(3, 13, "Custom draw ops: box/line/circle/text", ink);
                    })
                    .MinWidth(44)
                    .MaxWidth(44)
                    .MinHeight(16)
                    .MaxHeight(16)
                    .Style(CanvasStyle.Default with { DefaultRune = new Rune('█') }),
                new HStack(
                        new Button("Tick").Click(() => tick.Value++),
                        new TextBlock(() => $"Tick: {tick.Value}"))
                    .Spacing(1))
            .Spacing(1);
    }
}

