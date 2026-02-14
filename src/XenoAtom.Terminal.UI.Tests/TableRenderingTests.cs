// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Hosting;
using XenoAtom.Terminal.UI.Styling;

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

    [TestMethod]
    public void Table_Can_Separate_Last_Row_As_Footer()
    {
        var table = new Table();
        table.AddRow("A", "1")
            .AddRow("Total", "1")
            .LastRowIsFooter(true)
            .ShowFooterSeparator(true)
            .Style(TableStyle.Grid with { ShowRowSeparators = false });

        var rendered = RenderTable(table, width: 40, height: 8);

        StringAssert.Contains(rendered, "Total");
        StringAssert.Contains(rendered, "┼");
    }

    [TestMethod]
    public void Table_Footer_Separator_Does_Not_Duplicate_Row_Separators()
    {
        var baseTable = new Table();
        baseTable.AddRow("A", "1")
            .AddRow("Total", "1")
            .Style(TableStyle.Grid with { ShowRowSeparators = true });

        var footerTable = new Table();
        footerTable.AddRow("A", "1")
            .AddRow("Total", "1")
            .LastRowIsFooter(true)
            .ShowFooterSeparator(true)
            .Style(TableStyle.Grid with { ShowRowSeparators = true });

        var baseRendered = RenderTable(baseTable, width: 40, height: 8);
        var footerRendered = RenderTable(footerTable, width: 40, height: 8);

        Assert.AreEqual(CountOccurrences(baseRendered, '┼'), CountOccurrences(footerRendered, '┼'));
    }

    private static string RenderTable(Table table, int width, int height)
    {
        var root = new VStack { table };
        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(width, height));
        driver.Tick();

        var screen = new AnsiTestScreen(width, height);
        screen.Apply(driver.Backend.GetOutText());
        return screen.GetText();
    }

    private static int CountOccurrences(string text, char character)
    {
        var count = 0;
        for (var i = 0; i < text.Length; i++)
        {
            if (text[i] == character)
            {
                count++;
            }
        }

        return count;
    }
}

