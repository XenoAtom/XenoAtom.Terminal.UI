// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Text;
using XenoAtom.Terminal;
using XenoAtom.Terminal.Backends;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class CanvasTests
{
    [TestMethod]
    public void Canvas_Renders_Line_And_Box()
    {
        var backend = new InMemoryTerminalBackend(new TerminalSize(12, 6));
        using var session = Terminal.Open(backend, new TerminalOptions { ImplicitStartInput = true }, force: true);

        var canvas = new Canvas()
            .MinWidth(12)
            .MaxWidth(12)
            .MinHeight(6)
            .MaxHeight(6)
            .Painter(ctx =>
            {
                ctx.Clear(new Rune(' '), CellStyle.None);
                ctx.DrawBox(0, 0, 12, 6, LineGlyphs.Single, CellStyle.None);
                ctx.DrawLine(1, 1, 10, 4, new Rune('*'), CellStyle.None);
            });

        session.Instance.Write(canvas);

        var screen = new AnsiTestScreen(12, 6);
        screen.Apply(backend.GetOutText());
        var rendered = screen.GetText();

        StringAssert.Contains(rendered, "*");
        StringAssert.Contains(rendered, LineGlyphs.Single.TopLeft.ToString());
        StringAssert.Contains(rendered, LineGlyphs.Single.BottomRight.ToString());
    }

    [TestMethod]
    public void Canvas_Renders_Circle()
    {
        var backend = new InMemoryTerminalBackend(new TerminalSize(11, 7));
        using var session = Terminal.Open(backend, new TerminalOptions { ImplicitStartInput = true }, force: true);

        var canvas = new Canvas()
            .MinWidth(11)
            .MaxWidth(11)
            .MinHeight(7)
            .MaxHeight(7)
            .Painter(ctx =>
            {
                ctx.Clear(new Rune(' '), CellStyle.None);
                ctx.DrawCircle(5, 3, 2, new Rune('o'), CellStyle.None);
            });

        session.Instance.Write(canvas);

        var screen = new AnsiTestScreen(11, 7);
        screen.Apply(backend.GetOutText());
        var rendered = screen.GetText();

        // Midpoint circle draws symmetric points; ensure we rendered multiple 'o' characters.
        var count = rendered.Count(c => c == 'o');
        Assert.IsGreaterThan(4, count);
    }
}
