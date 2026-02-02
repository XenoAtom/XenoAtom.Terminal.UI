using System.Text;
using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.ControlsDemo.Demos;

[Demo("Alpha blending", "Rendering", Description = "Layer multiple translucent panels using Color.RgbA(...) and CellBuffer blending.")]
public sealed class AlphaBlendingDemo : ControlsDemoBase
{
    public AlphaBlendingDemo() : base(DemoSource.Get())
    {
    }

    public override Visual Build(DemoContext context)
    {
        _ = context;

        return new VStack(
                DemoUi.Hint("RGBA colors are blended into the current CellBuffer when a cell is written. Later layers draw over earlier ones."),
                new Canvas()
                    .Painter(ctx =>
                    {
                        var bg = Style.None.WithBackground(Color.Rgb(0x12, 0x16, 0x20));
                        ctx.Clear(new Rune(' '), bg);

                        DrawPanel(ctx, 2, 2, 24, 9, Color.RgbA(0xF7, 0x5B, 0x72, 0x90), Color.Rgb(0xFB, 0x85, 0x90), "Layer A (red 56%)");
                        DrawPanel(ctx, 12, 5, 26, 10, Color.RgbA(0x67, 0xAF, 0x34, 0x90), Color.Rgb(0x75, 0xC7, 0x3B), "Layer B (green 56%)");
                        DrawPanel(ctx, 26, 1, 22, 10, Color.RgbA(0x50, 0x9A, 0xF6, 0x90), Color.Rgb(0x77, 0xB1, 0xFB), "Layer C (blue 56%)");

                        // A subtle white overlay at the bottom to show blending over multiple backgrounds.
                        var overlay = Style.None.WithBackground(Color.RgbA(0xFF, 0xFF, 0xFF, 0x22));
                        ctx.FillRect(0, ctx.Size.Height - 3, ctx.Size.Width, 3, new Rune(' '), overlay);
                        ctx.WriteText(2, ctx.Size.Height - 2, "Overlays can be RGBA too (blended over what’s already there).", Style.None.WithForeground(Color.RgbA(0xFF, 0xFF, 0xFF, 0xE0)));
                    })
                    .MinWidth(52)
                    .MaxWidth(52)
                    .MinHeight(18)
                    .MaxHeight(18)
                    .Style(CanvasStyle.Default with { DefaultRune = new Rune(' ') }))
            .Spacing(1);
    }

    private static void DrawPanel(CanvasContext ctx, int x, int y, int width, int height, Color bgRgba, Color borderRgb, string label)
    {
        var bg = Style.None.WithBackground(bgRgba);
        ctx.FillRect(x, y, width, height, new Rune(' '), bg);

        var border = Style.None.WithForeground(borderRgb) | TextStyle.Bold;
        ctx.DrawBox(x, y, width, height, LineGlyphs.Single, border);

        var text = Style.None.WithForeground(Color.RgbA(0xFF, 0xFF, 0xFF, 0xE0));
        ctx.WriteText(x + 2, y + 1, label, text);

        var hint = Style.None.WithForeground(Color.RgbA(0xDC, 0xD8, 0xE4, 0xC0)) | TextStyle.Dim;
        ctx.WriteText(x + 2, y + 3, "overlap region →", hint);
    }
}

