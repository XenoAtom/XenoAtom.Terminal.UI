// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Hosting;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class TerminalAppDebugOverlayTests
{
    [TestMethod]
    public void DebugOverlay_Shows_Metrics()
    {
        var root = new TextBlock("Body");
        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(80, 20));

        driver.Tick();

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.F12 });
        driver.Tick();
        driver.Tick();

        var outText = driver.Backend.GetOutText();
        var screen = new AnsiTestScreen(80, 20);
        screen.Apply(outText);
        var rendered = screen.GetText();

        StringAssert.Contains(rendered, "FPS:");
        StringAssert.Contains(rendered, "Calls: Prepare");
        StringAssert.Contains(rendered, "Diff:");
    }
}
