// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal;
using XenoAtom.Terminal.Backends;
using XenoAtom.Terminal.UI.Hosting;
using XenoAtom.Terminal.UI.Rendering;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class InlineInteractiveHostTests
{
    private static CellBuffer CreateBuffer(int width, params string[] lines)
    {
        var buffer = new CellBuffer(width, lines.Length);
        buffer.Clear(Style.None);

        for (var i = 0; i < lines.Length; i++)
        {
            buffer.WriteText(0, i, lines[i].AsSpan(), Style.None);
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

    [TestMethod]
    public void Render_After_HandleResize_Forces_Repaint()
    {
        var backend = new InMemoryTerminalBackend(new TerminalSize(10, 5));
        using var session = Terminal.Open(backend, new TerminalOptions(), force: true);

        var host = new InlineInteractiveHost(session.Instance);

        host.Render(CreateBuffer(10, "Hello"), wantsCursor: false, cursorX: 0, cursorY: 0);
        var len1 = backend.GetOutText().Length;

        // Simulate terminal resize behavior that can invalidate saved cursor state.
        host.HandleResize();

        // Keep the same content and size, but ensure the host still repaints (it must not early-return).
        host.Render(CreateBuffer(10, "Hello"), wantsCursor: false, cursorX: 0, cursorY: 0);
        var len2 = backend.GetOutText().Length;

        Assert.IsGreaterThan(len1, len2, "Expected additional output after HandleResize().");
    }

    [TestMethod]
    public void PrepareForUserUpdate_Allows_Flow_Output_To_Push_Region_Down()
    {
        var backend = new InMemoryTerminalBackend(new TerminalSize(20, 10));
        using var session = Terminal.Open(backend, new TerminalOptions(), force: true);

        var host = new InlineInteractiveHost(session.Instance);

        // First render reserves a 3-line region.
        host.Render(CreateBuffer(20, "R1", "R2", "R3"), wantsCursor: false, cursorX: 0, cursorY: 0);

        // Simulate "flow output" written during the update phase.
        host.PrepareForUserUpdate();
        session.Instance.Write("FLOW\n");

        // Re-render the same region.
        host.Render(CreateBuffer(20, "R1", "R2", "R3"), wantsCursor: false, cursorX: 0, cursorY: 0);

        var screen = new AnsiTestScreen(20, 10);
        screen.Apply(backend.GetOutText());

        var text = screen.GetText().Split(Environment.NewLine);
        StringAssert.StartsWith(text[0], "FLOW");
        StringAssert.StartsWith(text[1], "R1");
        StringAssert.StartsWith(text[2], "R2");
        StringAssert.StartsWith(text[3], "R3");
    }
}
