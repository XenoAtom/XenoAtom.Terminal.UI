// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.Backends;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Rendering;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
[DoNotParallelize] // Observes the process-wide Terminal instance.
public sealed class TerminalAppSnapshotRendererTests
{
    [TestMethod]
    public void RenderSvg_Does_Not_Initialize_The_Global_Terminal()
    {
        Terminal.Close();
        Assert.IsFalse(Terminal.IsInitialized);

        var svg = TerminalAppSnapshotRenderer.RenderSvg(new TextBlock("Snapshot"), 20, 3);

        StringAssert.Contains(svg, "<svg");
        Assert.IsFalse(Terminal.IsInitialized);
    }

    [TestMethod]
    public void RenderSvg_Does_Not_Replace_Or_Write_To_The_Global_Terminal()
    {
        var backend = new InMemoryTerminalBackend(new TerminalSize(20, 3));
        using var session = Terminal.Open(backend, new TerminalOptions { ImplicitStartInput = false }, force: true);
        var global = Terminal.Instance;
        var outputBefore = backend.GetOutText();

        var svg = TerminalAppSnapshotRenderer.RenderSvg(new TextBlock("Snapshot"), 20, 3);

        StringAssert.Contains(svg, "<svg");
        Assert.IsTrue(Terminal.IsInitialized);
        Assert.AreSame(global, Terminal.Instance);
        Assert.AreEqual(outputBefore, backend.GetOutText());
    }
}
