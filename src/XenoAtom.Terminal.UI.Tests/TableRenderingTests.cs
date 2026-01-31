// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Hosting;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class TableRenderingTests
{
    [TestMethod]
    public void Table_Renders_Headers_And_Cells()
    {
        var table = new Table();
        table.Headers("Name", "Value")
            .AddRow("A", "1")
            .AddRow("B", "2");

        var root = new VStack { table };
        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(40, 10));
        driver.Tick();

        var outText = driver.Backend.GetOutText();
        StringAssert.Contains(outText, "Name");
        StringAssert.Contains(outText, "Value");
        StringAssert.Contains(outText, "A");
        StringAssert.Contains(outText, "2");
    }

    [TestMethod]
    public void Table_Allows_Multiline_Cell_Content()
    {
        var table = new Table();
        table.Headers("Field", "Value")
            .AddRow("Title", "Hello")
            .AddRow("Notes", new VStack("Line 1", "Line 2", "Line 3").Spacing(0));

        var root = new VStack { table };
        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(60, 14));
        driver.Tick();

        var screen = new AnsiTestScreen(60, 14);
        screen.Apply(driver.Backend.GetOutText());
        var rendered = screen.GetText();

        StringAssert.Contains(rendered, "Notes");
        StringAssert.Contains(rendered, "Line 1");
        StringAssert.Contains(rendered, "Line 2");
        StringAssert.Contains(rendered, "Line 3");
    }
}

