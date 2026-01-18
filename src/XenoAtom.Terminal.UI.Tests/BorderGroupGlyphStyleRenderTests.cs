// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.Backends;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Hosting;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class BorderGroupGlyphStyleRenderTests
{
    [TestMethod]
    public void BorderStyle_AsAscii_Renders_Plus_Corners()
    {
        var border = new Border("X")
            .Style(BorderStyle.Ascii)
            .HorizontalAlignment(HorizontalAlignment.Left)
            .VerticalAlignment(VerticalAlignment.Top);

        using var driver = new TerminalAppTestDriver(border, TerminalHostKind.Fullscreen, new TerminalSize(10, 5));
        driver.Tick();

        var screen = new AnsiTestScreen(10, 5);
        screen.Apply(driver.Backend.GetOutText());
        var lines = screen.GetText().Split('\n');

        Assert.AreEqual('+', lines[0][0]);
        Assert.AreEqual('+', lines[0][border.Bounds.Width - 1]);
        Assert.AreEqual('+', lines[border.Bounds.Height - 1][0]);
    }

    [TestMethod]
    public void GroupStyle_AsAscii_Renders_Plus_Corners()
    {
        var group = new Group("Title")
            .Style(GroupStyle.Ascii)
            .Content("X")
            .HorizontalAlignment(HorizontalAlignment.Left)
            .VerticalAlignment(VerticalAlignment.Top);

        using var driver = new TerminalAppTestDriver(group, TerminalHostKind.Fullscreen, new TerminalSize(20, 6));
        driver.Tick();

        var screen = new AnsiTestScreen(20, 6);
        screen.Apply(driver.Backend.GetOutText());
        var lines = screen.GetText().Split('\n');

        Assert.AreEqual('+', lines[0][0]);
        Assert.AreEqual('+', lines[0][group.Bounds.Width - 1]);
        Assert.AreEqual('+', lines[group.Bounds.Height - 1][0]);
    }
}

