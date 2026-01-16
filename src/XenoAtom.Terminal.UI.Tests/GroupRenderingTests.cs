// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Hosting;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class GroupRenderingTests
{
    [TestMethod]
    public void Group_Renders_Corner_Texts()
    {
        var group = new Group
        {
            Padding = new Thickness(1),
            TopLeftText = "TL",
            TopRightText = "TR",
            BottomLeftText = "BL",
            BottomRightText = "BR",
            Content = new TextBlock("X"),
        };

        var root = new VStack { group };
        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(40, 8));
        driver.Tick();

        var outText = driver.Backend.GetOutText();
        StringAssert.Contains(outText, "TL");
        StringAssert.Contains(outText, "TR");
        StringAssert.Contains(outText, "BL");
        StringAssert.Contains(outText, "BR");
    }
}

