// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.DataGrid;
using XenoAtom.Terminal.UI.Hosting;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class DataGridRenderingTests
{
    [TestMethod]
    public void DataGrid_Renders_Header_And_Cells()
    {
        var laneAccessor = new BindingAccessor<int>("lane", o => ((SwimRow)o).Lane, (o, v) => ((SwimRow)o).Lane = v);
        var swimmerAccessor = new BindingAccessor<string>("swimmer", o => ((SwimRow)o).Swimmer, (o, v) => ((SwimRow)o).Swimmer = v);

        var doc = new DataGridListDocument<SwimRow>();
        doc.SetColumns(new[]
        {
            new DataGridColumnInfo("lane", "lane", typeof(int), ReadOnly: false, laneAccessor),
            new DataGridColumnInfo("swimmer", "swimmer", typeof(string), ReadOnly: false, swimmerAccessor),
        });

        doc.AddRow(new SwimRow { Lane = 4, Swimmer = "Joseph" });
        doc.AddRow(new SwimRow { Lane = 2, Swimmer = "Michael" });

        using var view = new DataGridDocumentView(doc);

        var grid = new DataGridControl { View = view };
        grid.Columns.Add(new DataGridColumn<int> { Key = "lane", TypedAccessor = laneAccessor, Width = GridLength.Auto, CellAlignment = TextAlignment.Right });
        grid.Columns.Add(new DataGridColumn<string> { Key = "swimmer", TypedAccessor = swimmerAccessor, Width = GridLength.Star(1) });

        var root = new ScrollViewer(grid) { HorizontalAlignment = Align.Stretch, VerticalAlignment = Align.Stretch };
        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(40, 8));
        driver.Tick();

        var outText = driver.Backend.GetOutText();
        StringAssert.Contains(outText, "lane");
        StringAssert.Contains(outText, "swimmer");
        StringAssert.Contains(outText, "Joseph");
        StringAssert.Contains(outText, "Michael");
    }

    [TestMethod]
    public void DataGrid_Scrolls_Vertically_Inside_ScrollViewer()
    {
        var textAccessor = new BindingAccessor<string>("text", o => ((TextRow)o).Text, (o, v) => ((TextRow)o).Text = v);

        var doc = new DataGridListDocument<TextRow>();
        doc.SetColumns(new[]
        {
            new DataGridColumnInfo("text", "text", typeof(string), ReadOnly: false, textAccessor),
        });

        for (var i = 0; i < 20; i++)
        {
            doc.AddRow(new TextRow { Text = $"Item {i}" });
        }

        using var view = new DataGridDocumentView(doc);

        var grid = new DataGridControl { View = view };
        grid.Columns.Add(new DataGridColumn<string> { Key = "text", TypedAccessor = textAccessor, Width = GridLength.Star(1) });

        var root = new ScrollViewer(grid) { HorizontalAlignment = Align.Stretch, VerticalAlignment = Align.Stretch };
        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(16, 6));
        driver.Tick();

        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Wheel, Button = TerminalMouseButton.Wheel, WheelDelta = -1, X = 1, Y = 2 });
        driver.Tick();

        var screen = new AnsiTestScreen(16, 6);
        screen.Apply(driver.Backend.GetOutText());
        var rendered = screen.GetText();

        StringAssert.Contains(rendered, "Item 1");
        Assert.IsFalse(rendered.Contains("Item 0", StringComparison.Ordinal), "After scrolling down, Item 0 should no longer be visible.");
    }

    [TestMethod]
    public void DataGrid_Allows_Editing_String_Cell()
    {
        var nameAccessor = new BindingAccessor<string>("name", o => ((TextRow)o).Text, (o, v) => ((TextRow)o).Text = v);

        var doc = new DataGridListDocument<TextRow>();
        doc.SetColumns(new[]
        {
            new DataGridColumnInfo("name", "name", typeof(string), ReadOnly: false, nameAccessor),
        });

        var row = new TextRow { Text = string.Empty };
        doc.AddRow(row);

        using var view = new DataGridDocumentView(doc);

        var grid = new DataGridControl { View = view };
        grid.Columns.Add(new DataGridColumn<string> { Key = "name", TypedAccessor = nameAccessor, Width = GridLength.Star(1) });

        var root = new ScrollViewer(grid) { HorizontalAlignment = Align.Stretch, VerticalAlignment = Align.Stretch };
        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(20, 6));
        driver.Tick();

        // Open editor (F2), type, commit (Enter).
        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.F2 });
        driver.Tick();

        driver.Backend.PushEvent(new TerminalTextEvent { Text = "Hello" });
        driver.Tick();

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Enter });
        driver.Tick();

        Assert.AreEqual("Hello", row.Text);
    }

    [TestMethod]
    public void DataGrid_Allows_Tab_To_Move_To_Next_Cell_While_Editing()
    {
        var aAccessor = new BindingAccessor<string>("a", o => ((TwoColumnRow)o).A, (o, v) => ((TwoColumnRow)o).A = v);
        var bAccessor = new BindingAccessor<string>("b", o => ((TwoColumnRow)o).B, (o, v) => ((TwoColumnRow)o).B = v);

        var doc = new DataGridListDocument<TwoColumnRow>();
        doc.SetColumns(new[]
        {
            new DataGridColumnInfo("a", "a", typeof(string), ReadOnly: false, aAccessor),
            new DataGridColumnInfo("b", "b", typeof(string), ReadOnly: false, bAccessor),
        });

        var row = new TwoColumnRow();
        doc.AddRow(row);

        using var view = new DataGridDocumentView(doc);

        var grid = new DataGridControl { View = view };
        grid.Columns.Add(new DataGridColumn<string> { Key = "a", TypedAccessor = aAccessor, Width = GridLength.Star(1) });
        grid.Columns.Add(new DataGridColumn<string> { Key = "b", TypedAccessor = bAccessor, Width = GridLength.Star(1) });

        var root = new ScrollViewer(grid) { HorizontalAlignment = Align.Stretch, VerticalAlignment = Align.Stretch };
        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(20, 6));
        driver.Tick();

        // Open editor (F2), type in column A, Tab to next cell, type in column B.
        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.F2 });
        driver.Tick();

        driver.Backend.PushEvent(new TerminalTextEvent { Text = "A1" });
        driver.Tick();

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Tab });
        driver.Tick();

        driver.Backend.PushEvent(new TerminalTextEvent { Text = "B1" });
        driver.Tick();

        Assert.AreEqual("A1", row.A);
        Assert.AreEqual("B1", row.B);
    }

    private sealed class SwimRow
    {
        public int Lane { get; set; }
        public string Swimmer { get; set; } = string.Empty;
    }

    private sealed class TextRow
    {
        public string Text { get; set; } = string.Empty;
    }

    private sealed class TwoColumnRow
    {
        public string A { get; set; } = string.Empty;
        public string B { get; set; } = string.Empty;
    }
}
