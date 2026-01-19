// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Text;
using XenoAtom.Terminal;
using XenoAtom.Terminal.Backends;
using XenoAtom.Terminal.UI.Rendering;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class CellBufferDiffRendererTests
{
    [TestMethod]
    public void RenderFullscreen_RestoresCursor_EvenWhenCursorDidNotMove()
    {
        var backend = new InMemoryTerminalBackend(new TerminalSize(6, 1));
        using var session = Terminal.Open(backend, new TerminalOptions { ImplicitStartInput = true }, force: true);
        using var renderer = new CellBufferDiffRenderer();

        var buffer = new CellBuffer(6, 1);
        buffer.Clear(Style.None);
        buffer.SetCell(0, 0, new Rune('A'), Style.None);

        renderer.RenderFullscreen(session.Instance, buffer, wantsCursor: true, cursorX: 0, cursorY: 0);
        var lengthAfterFirst = backend.GetOutText().Length;

        buffer.SetCell(1, 0, new Rune('B'), Style.None);
        renderer.RenderFullscreen(session.Instance, buffer, wantsCursor: true, cursorX: 0, cursorY: 0);

        var delta = backend.GetOutText().Substring(lengthAfterFirst);
        StringAssert.Contains(delta, "\x1b[?25h");
    }
}

