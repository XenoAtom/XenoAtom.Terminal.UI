// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Hosting;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class StatusBarRenderingTests
{
    [TestMethod]
    public void StatusBar_Renders_Left_And_Right()
    {
        var status = new StatusBar { LeftText = "L", RightText = "R" };
        var layout = new DockLayout { Content = new TextBlock("X"), Bottom = status };

        using var driver = new TerminalAppTestDriver(layout, TerminalHostKind.Fullscreen, new TerminalSize(30, 5));
        driver.Tick();

        var outText = driver.Backend.GetOutText();
        StringAssert.Contains(outText, "L");
        StringAssert.Contains(outText, "R");
    }
}

