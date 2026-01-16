// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Hosting;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class TextBlockRenderingTests
{
    [TestMethod]
    public void TextBlock_EndEllipsis_Trims_To_Width()
    {
        var tb = new TextBlock("HelloWorld")
        {
            Wrap = false,
            Trimming = TextTrimming.EndEllipsis,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            MaxWidth = 5,
        };

        var root = new VStack(tb);
        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(10, 2));
        driver.Tick();

        var outText = driver.Backend.GetOutText();
        StringAssert.Contains(outText, "Hell…");
    }

    [TestMethod]
    public void TextBlock_StartEllipsis_Trims_To_Width()
    {
        var tb = new TextBlock("HelloWorld")
        {
            Wrap = false,
            Trimming = TextTrimming.StartEllipsis,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            MaxWidth = 5,
        };

        var root = new VStack(tb);
        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(10, 2));
        driver.Tick();

        var outText = driver.Backend.GetOutText();
        StringAssert.Contains(outText, "…orld");
    }

    [TestMethod]
    public void TextBlock_Can_Center_Align_Text_When_Stretched()
    {
        var tb = new TextBlock("Hi")
        {
            Wrap = false,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            TextAlignment = TextAlignment.Center,
        };

        var root = new VStack(tb);
        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(10, 2));
        driver.Tick();

        var outText = driver.Backend.GetOutText();
        StringAssert.Contains(outText, "    Hi");
    }
}
