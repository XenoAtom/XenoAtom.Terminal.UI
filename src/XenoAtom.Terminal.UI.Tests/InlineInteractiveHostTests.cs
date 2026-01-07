// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal;
using XenoAtom.Terminal.Backends;
using XenoAtom.Terminal.UI.Hosting;
using XenoAtom.Terminal.UI.Rendering;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class InlineInteractiveHostTests
{
    private static CellBuffer CreateBuffer(int width, params string[] lines)
    {
        var buffer = new CellBuffer(width, lines.Length);
        buffer.Clear(CellStyle.None);

        for (var i = 0; i < lines.Length; i++)
        {
            buffer.WriteText(0, i, lines[i].AsSpan(), CellStyle.None);
        }

        return buffer;
    }

    [TestMethod]
    public void Render_Restores_Cursor_Position_When_Cursor_Moved()
    {
        var backend = new InMemoryTerminalBackend(new TerminalSize(20, 10));
        using var session = Terminal.Open(backend, new TerminalOptions(), force: true);

        var host = new InlineInteractiveHost(session.Instance);

        host.Render(CreateBuffer(20, "A", "B", "C"), wantsCursor: false, cursorX: 0, cursorY: 0);

        var out1 = backend.GetOutText();
        StringAssert.Contains(out1, "\x1b[s");

        session.Instance.SetCursorPosition(new TerminalPosition(0, 0));

        host.Render(CreateBuffer(20, "A", "B", "D"), wantsCursor: false, cursorX: 0, cursorY: 0);

        var out2 = backend.GetOutText();
        var delta = out2.Substring(out1.Length);
        StringAssert.Contains(delta, "\x1b[u");
    }

    [TestMethod]
    public void Render_Same_Lines_Produces_No_Output()
    {
        var backend = new InMemoryTerminalBackend(new TerminalSize(20, 10));
        using var session = Terminal.Open(backend, new TerminalOptions(), force: true);

        var host = new InlineInteractiveHost(session.Instance);

        host.Render(CreateBuffer(20, "A", "B", "C"), wantsCursor: false, cursorX: 0, cursorY: 0);

        var len = backend.GetOutText().Length;

        host.Render(CreateBuffer(20, "A", "B", "C"), wantsCursor: false, cursorX: 0, cursorY: 0);

        Assert.AreEqual(len, backend.GetOutText().Length);
    }

    [TestMethod]
    public void Render_Clamps_To_Visible_Height_And_Survives_Resize()
    {
        var backend = new InMemoryTerminalBackend(new TerminalSize(20, 3));
        using var session = Terminal.Open(backend, new TerminalOptions(), force: true);

        var host = new InlineInteractiveHost(session.Instance);

        host.Render(CreateBuffer(20, "L1", "L2", "L3", "L4", "L5"), wantsCursor: false, cursorX: 0, cursorY: 0);

        backend.SetSize(new TerminalSize(20, 2), raiseEvent: false);

        host.Render(CreateBuffer(20, "L1", "L2", "L3", "L4", "L5"), wantsCursor: false, cursorX: 0, cursorY: 0);
    }
}
