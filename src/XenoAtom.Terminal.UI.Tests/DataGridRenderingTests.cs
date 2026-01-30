// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Commands;
using XenoAtom.Terminal.UI.DataGrid;
using XenoAtom.Terminal.UI.Hosting;
using XenoAtom.Terminal.UI.Layout;
using System.Linq;

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

    [TestMethod]
    public void DataGrid_KeyboardDown_Scrolls_And_Rerenders()
    {
        var textAccessor = new BindingAccessor<string>("text", o => ((TextRow)o).Text, (o, v) => ((TextRow)o).Text = v);

        var doc = new DataGridListDocument<TextRow>();
        doc.SetColumns(new[]
        {
            new DataGridColumnInfo("text", "text", typeof(string), ReadOnly: false, textAccessor),
        });

        for (var i = 0; i < 30; i++)
        {
            doc.AddRow(new TextRow { Text = $"Item {i:00}" });
        }

        using var view = new DataGridDocumentView(doc);

        var grid = new DataGridControl { View = view, ShowHeader = true };
        grid.Columns.Add(new DataGridColumn<string> { Key = "text", TypedAccessor = textAccessor, Width = GridLength.Star(1) });

        var root = new ScrollViewer(grid) { HorizontalAlignment = Align.Stretch, VerticalAlignment = Align.Stretch };
        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(18, 6));
        driver.Tick();

        for (var i = 0; i < 12; i++)
        {
            driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Down });
            driver.Tick();
        }

        var screen = new AnsiTestScreen(18, 6);
        screen.Apply(driver.Backend.GetOutText());
        var rendered = screen.GetText();

        StringAssert.Contains(rendered, "Item 10");
        Assert.IsFalse(rendered.Contains("Item 00", StringComparison.Ordinal), "After keyboard scrolling down, Item 00 should no longer be visible.");
    }

    [TestMethod]
    public void DataGrid_Clicking_Current_Cell_Starts_Edit()
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

        using var driver = new TerminalAppTestDriver(grid, TerminalHostKind.Fullscreen, new TerminalSize(20, 4));
        driver.Tick();

        // First click selects the cell; second click starts editing.
        var x = grid.Bounds.X + 2;
        var y = grid.Bounds.Y + 1; // header row at y=0, first data row at y=1

        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Down, Button = TerminalMouseButton.Left, X = x, Y = y });
        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Up, Button = TerminalMouseButton.Left, X = x, Y = y });
        driver.Tick();

        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Down, Button = TerminalMouseButton.Left, X = x, Y = y });
        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Up, Button = TerminalMouseButton.Left, X = x, Y = y });
        driver.Tick();

        driver.Backend.PushEvent(new TerminalTextEvent { Text = "Hello" });
        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Enter });
        driver.TickUntil(() => row.Text == "Hello");
    }

    [TestMethod]
    public void DataGrid_UpDown_While_Editing_Closes_Editor_And_Navigates()
    {
        var nameAccessor = new BindingAccessor<string>("name", o => ((TextRow)o).Text, (o, v) => ((TextRow)o).Text = v);

        var doc = new DataGridListDocument<TextRow>();
        doc.SetColumns(new[]
        {
            new DataGridColumnInfo("name", "name", typeof(string), ReadOnly: false, nameAccessor),
        });

        var row0 = new TextRow { Text = string.Empty };
        var row1 = new TextRow { Text = string.Empty };
        doc.AddRow(row0);
        doc.AddRow(row1);

        using var view = new DataGridDocumentView(doc);

        var grid = new DataGridControl { View = view };
        grid.Columns.Add(new DataGridColumn<string> { Key = "name", TypedAccessor = nameAccessor, Width = GridLength.Star(1) });

        using var driver = new TerminalAppTestDriver(grid, TerminalHostKind.Fullscreen, new TerminalSize(20, 5));
        driver.Tick();

        // Start editing, type, then Down should exit edit mode and move to next row.
        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.F2 });
        driver.Backend.PushEvent(new TerminalTextEvent { Text = "Hello" });
        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Down });
        driver.Backend.PushEvent(new TerminalTextEvent { Text = "X" });
        driver.Tick();

        Assert.AreEqual("Hello", row0.Text);
        Assert.AreEqual(string.Empty, row1.Text);
        Assert.AreEqual(1, grid.CurrentCell.Row);
    }

    [TestMethod]
    public void DataGrid_CtrlHomeEnd_Moves_To_Start_And_End()
    {
        var aAccessor = new BindingAccessor<string>("a", o => ((TwoColumnRow)o).A, (o, v) => ((TwoColumnRow)o).A = v);
        var bAccessor = new BindingAccessor<string>("b", o => ((TwoColumnRow)o).B, (o, v) => ((TwoColumnRow)o).B = v);

        var doc = new DataGridListDocument<TwoColumnRow>();
        doc.SetColumns(new[]
        {
            new DataGridColumnInfo("a", "A", typeof(string), ReadOnly: false, aAccessor),
            new DataGridColumnInfo("b", "B", typeof(string), ReadOnly: false, bAccessor),
        });

        doc.AddRow(new TwoColumnRow { A = "r0a", B = "r0b" });
        doc.AddRow(new TwoColumnRow { A = "r1a", B = "r1b" });
        doc.AddRow(new TwoColumnRow { A = "r2a", B = "r2b" });

        using var view = new DataGridDocumentView(doc);

        var grid = new DataGridControl { View = view };
        grid.Columns.Add(new DataGridColumn<string> { Key = "a", TypedAccessor = aAccessor, Width = GridLength.Auto });
        grid.Columns.Add(new DataGridColumn<string> { Key = "b", TypedAccessor = bAccessor, Width = GridLength.Auto });
        grid.CurrentCell = new DataGridCell(1, 0);

        using var driver = new TerminalAppTestDriver(grid, TerminalHostKind.Fullscreen, new TerminalSize(30, 6));
        driver.Tick();

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.End, Modifiers = TerminalModifiers.Ctrl });
        driver.Tick();
        Assert.AreEqual(new DataGridCell(2, 1), grid.CurrentCell);

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Home, Modifiers = TerminalModifiers.Ctrl });
        driver.Tick();
        Assert.AreEqual(new DataGridCell(0, 0), grid.CurrentCell);
    }

    [TestMethod]
    public void DataGrid_CtrlA_Selects_Entire_Table_And_CtrlC_Copies()
    {
        var aAccessor = new BindingAccessor<string>("a", o => ((TwoColumnRow)o).A, (o, v) => ((TwoColumnRow)o).A = v);
        var bAccessor = new BindingAccessor<string>("b", o => ((TwoColumnRow)o).B, (o, v) => ((TwoColumnRow)o).B = v);

        var doc = new DataGridListDocument<TwoColumnRow>();
        doc.SetColumns(new[]
        {
            new DataGridColumnInfo("a", "A", typeof(string), ReadOnly: false, aAccessor),
            new DataGridColumnInfo("b", "B", typeof(string), ReadOnly: false, bAccessor),
        });

        doc.AddRow(new TwoColumnRow { A = "r0a", B = "r0b" });
        doc.AddRow(new TwoColumnRow { A = "r1a", B = "r1b" });

        using var view = new DataGridDocumentView(doc);

        var grid = new DataGridControl { View = view };
        grid.Columns.Add(new DataGridColumn<string> { Key = "a", TypedAccessor = aAccessor, Width = GridLength.Auto });
        grid.Columns.Add(new DataGridColumn<string> { Key = "b", TypedAccessor = bAccessor, Width = GridLength.Auto });

        using var driver = new TerminalAppTestDriver(grid, TerminalHostKind.Fullscreen, new TerminalSize(30, 6));
        driver.Tick();

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Unknown, Char = TerminalChar.CtrlA, Modifiers = TerminalModifiers.Ctrl });
        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Unknown, Char = TerminalChar.CtrlC, Modifiers = TerminalModifiers.Ctrl });
        driver.Tick();

        Assert.AreEqual("A\tB\nr0a\tr0b\nr1a\tr1b", driver.Terminal.Clipboard.Text);
    }

    [TestMethod]
    public void DataGrid_Shows_Ellipsis_When_Cell_Text_Is_Clipped()
    {
        var textAccessor = new BindingAccessor<string>("text", o => ((TextRow)o).Text, (o, v) => ((TextRow)o).Text = v);

        var doc = new DataGridListDocument<TextRow>();
        doc.SetColumns(new[]
        {
            new DataGridColumnInfo("text", "text", typeof(string), ReadOnly: false, textAccessor),
        });

        doc.AddRow(new TextRow { Text = "abcdefghij" });

        using var view = new DataGridDocumentView(doc);

        var grid = new DataGridControl { View = view, ShowRowAnchor = false, ShowHeader = false };
        grid.Columns.Add(new DataGridColumn<string> { Key = "text", TypedAccessor = textAccessor, Width = GridLength.Fixed(5) });

        using var driver = new TerminalAppTestDriver(grid, TerminalHostKind.Fullscreen, new TerminalSize(5, 2));
        driver.Tick();

        var screen = new AnsiTestScreen(5, 2);
        screen.Apply(driver.Backend.GetOutText());
        var rendered = screen.GetText();

        StringAssert.Contains(rendered, "abcd…");
    }

    [TestMethod]
    public void DataGrid_Auto_Width_Considers_Cell_Content()
    {
        var textAccessor = new BindingAccessor<string>("text", o => ((TextRow)o).Text, (o, v) => ((TextRow)o).Text = v);

        var doc = new DataGridListDocument<TextRow>();
        doc.SetColumns(new[]
        {
            new DataGridColumnInfo("text", "text", typeof(string), ReadOnly: false, textAccessor),
        });

        doc.AddRow(new TextRow { Text = "VeryLongValue" });

        using var view = new DataGridDocumentView(doc);

        var grid = new DataGridControl { View = view, ShowRowAnchor = false, ShowHeader = false };
        grid.Columns.Add(new DataGridColumn<string> { Key = "text", TypedAccessor = textAccessor, Width = GridLength.Auto });

        grid.Measure(LayoutConstraints.Unbounded);
        Assert.IsGreaterThanOrEqualTo(grid.DesiredSize.Width, "VeryLongValue".Length, $"Expected autosizing to consider cell content. width={grid.DesiredSize.Width}");
    }

    [TestMethod]
    public void DataGrid_Allows_Resizing_Columns_By_Dragging_Header_Separator()
    {
        var aAccessor = new BindingAccessor<string>("a", o => ((TwoColumnRow)o).A, (o, v) => ((TwoColumnRow)o).A = v);
        var bAccessor = new BindingAccessor<string>("b", o => ((TwoColumnRow)o).B, (o, v) => ((TwoColumnRow)o).B = v);

        var doc = new DataGridListDocument<TwoColumnRow>();
        doc.SetColumns(new[]
        {
            new DataGridColumnInfo("a", "A", typeof(string), ReadOnly: false, aAccessor),
            new DataGridColumnInfo("b", "B", typeof(string), ReadOnly: false, bAccessor),
        });
        doc.AddRow(new TwoColumnRow { A = "a", B = "b" });

        using var view = new DataGridDocumentView(doc);

        var colA = new DataGridColumn<string> { Key = "a", TypedAccessor = aAccessor, Width = GridLength.Fixed(4) };
        var colB = new DataGridColumn<string> { Key = "b", TypedAccessor = bAccessor, Width = GridLength.Fixed(4) };

        var grid = new DataGridControl { View = view };
        grid.Columns.Add(colA);
        grid.Columns.Add(colB);

        using var driver = new TerminalAppTestDriver(grid, TerminalHostKind.Fullscreen, new TerminalSize(20, 5));
        driver.Tick();

        var separatorX = grid.Bounds.X + grid.RowAnchorWidth + 4;
        var headerY = grid.Bounds.Y;

        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Down, Button = TerminalMouseButton.Left, X = separatorX, Y = headerY });
        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Drag, Button = TerminalMouseButton.Left, X = separatorX + 3, Y = headerY });
        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Up, Button = TerminalMouseButton.Left, X = separatorX + 3, Y = headerY });
        driver.Tick();

        Assert.AreEqual(GridUnitType.Fixed, colA.Width.Type);
        Assert.AreEqual(7, (int)Math.Round(colA.Width.Value));
    }

    [TestMethod]
    public void DataGrid_StringEditor_Shows_Overflow_Indicators_When_Scrolled()
    {
        var textAccessor = new BindingAccessor<string>("text", o => ((TextRow)o).Text, (o, v) => ((TextRow)o).Text = v);

        var doc = new DataGridListDocument<TextRow>();
        doc.SetColumns(new[]
        {
            new DataGridColumnInfo("text", "text", typeof(string), ReadOnly: false, textAccessor),
        });

        var row = new TextRow { Text = string.Empty };
        doc.AddRow(row);

        using var view = new DataGridDocumentView(doc);

        // Schema-only: no grid.Columns entries => uses built-in pooled string editor.
        var grid = new DataGridControl { View = view, ShowRowAnchor = false, ShowHeader = false };

        using var driver = new TerminalAppTestDriver(grid, TerminalHostKind.Fullscreen, new TerminalSize(6, 2));
        driver.Tick();
        driver.App.Focus(grid);
        driver.Tick();

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.F2 });
        driver.Tick();

        var focusedTextBox = grid.EnumerateVisualsDepthFirst().OfType<TextBox>().FirstOrDefault(t => t.HasFocus);
        Assert.IsNotNull(focusedTextBox, "Expected a focused TextBox editor after starting edit.");

        driver.Backend.PushEvent(new TerminalTextEvent { Text = "0123456789" });
        driver.Tick();
        // One extra frame: DataGridControl mirrors the editor scroll version via PrepareChildren -> bindable property,
        // which is applied through the binding-write queue.
        driver.Tick();

        var screen = new AnsiTestScreen(6, 2);
        screen.Apply(driver.Backend.GetOutText());
        var rendered = screen.GetText();

        // When the caret is forced to the right, the editor should horizontally scroll and show an overflow indicator.
        Assert.IsTrue(rendered.Contains("←", StringComparison.Ordinal) || rendered.Contains("→", StringComparison.Ordinal), $"bounds={focusedTextBox.Bounds} text=[{rendered}]");
    }

    [TestMethod]
    public void DataGrid_Schema_Number_Cell_Uses_NumberBox_Editor()
    {
        var intAccessor = new BindingAccessor<int>("n", o => ((IntRow)o).Value, (o, v) => ((IntRow)o).Value = v);

        var doc = new DataGridListDocument<IntRow>();
        doc.SetColumns(new[]
        {
            new DataGridColumnInfo("n", "n", typeof(int), ReadOnly: false, intAccessor),
        });

        var row = new IntRow { Value = 0 };
        doc.AddRow(row);

        using var view = new DataGridDocumentView(doc);

        // Schema-only: editing should still work (NumberBox<int> from default templates fallback).
        var grid = new DataGridControl { View = view };

        using var driver = new TerminalAppTestDriver(grid, TerminalHostKind.Fullscreen, new TerminalSize(20, 4));
        driver.Tick();

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.F2 });
        driver.Tick();

        driver.Backend.PushEvent(new TerminalTextEvent { Text = "123" });
        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Enter });
        driver.TickUntil(() => row.Value == 123);
    }

    [TestMethod]
    public void DataGrid_Schema_Number_Cell_Editor_Reflects_Current_Value_And_Updates_Source()
    {
        var intAccessor = new BindingAccessor<int>("n", o => ((IntRow)o).Value, (o, v) => ((IntRow)o).Value = v);

        var doc = new DataGridListDocument<IntRow>();
        doc.SetColumns(new[]
        {
            new DataGridColumnInfo("n", "n", typeof(int), ReadOnly: false, intAccessor),
        });

        var row = new IntRow { Value = 42 };
        doc.AddRow(row);

        using var view = new DataGridDocumentView(doc);

        // Schema-only: editing should use a NumberBox<int> bound to the model.
        var grid = new DataGridControl { View = view };

        using var driver = new TerminalAppTestDriver(grid, TerminalHostKind.Fullscreen, new TerminalSize(20, 4));
        driver.Tick();

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.F2 });
        driver.Tick();

        var numberBox = grid.EnumerateVisualsDepthFirst().OfType<NumberBox<int>>().FirstOrDefault(b => b.HasFocus);
        Assert.IsNotNull(numberBox, "Expected a focused NumberBox<int> editor after starting edit.");
        Assert.AreEqual("42", numberBox.Text, "Expected number editor to reflect the current cell value.");

        // Replace the full value and commit.
        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Unknown, Char = TerminalChar.CtrlA, Modifiers = TerminalModifiers.Ctrl });
        driver.Backend.PushEvent(new TerminalTextEvent { Text = "99" });
        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Enter });
        driver.TickUntil(() => row.Value == 99);
    }

    [TestMethod]
    public void DataGrid_Column_Number_Cell_Editor_Reflects_Current_Value_And_Updates_Source()
    {
        var intAccessor = new BindingAccessor<int>("n", o => ((IntRow)o).Value, (o, v) => ((IntRow)o).Value = v);

        var doc = new DataGridListDocument<IntRow>();
        doc.SetColumns(new[]
        {
            new DataGridColumnInfo("n", "n", typeof(int), ReadOnly: false, intAccessor),
        });

        var row = new IntRow { Value = 42 };
        doc.AddRow(row);

        using var view = new DataGridDocumentView(doc);

        // Column-driven (via default DataTemplates editor): should still show the current value and update the model.
        var grid = new DataGridControl { View = view };
        grid.Columns.Add(new DataGridColumn<int> { Key = "n", TypedAccessor = intAccessor, Width = GridLength.Fixed(6) });

        using var driver = new TerminalAppTestDriver(grid, TerminalHostKind.Fullscreen, new TerminalSize(20, 4));
        driver.Tick();

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.F2 });
        driver.Tick();

        var numberBox = grid.EnumerateVisualsDepthFirst().OfType<NumberBox<int>>().FirstOrDefault(b => b.HasFocus);
        Assert.IsNotNull(numberBox, "Expected a focused NumberBox<int> editor after starting edit.");
        Assert.AreEqual("42", numberBox.Text, "Expected number editor to reflect the current cell value.");

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Unknown, Char = TerminalChar.CtrlA, Modifiers = TerminalModifiers.Ctrl });
        driver.Backend.PushEvent(new TerminalTextEvent { Text = "99" });
        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Enter });
        driver.TickUntil(() => row.Value == 99);
    }

    [TestMethod]
    public void DataGrid_Allows_Resizing_Columns_By_Dragging_Body_Separator()
    {
        var aAccessor = new BindingAccessor<string>("a", o => ((TwoColumnRow)o).A, (o, v) => ((TwoColumnRow)o).A = v);
        var bAccessor = new BindingAccessor<string>("b", o => ((TwoColumnRow)o).B, (o, v) => ((TwoColumnRow)o).B = v);

        var doc = new DataGridListDocument<TwoColumnRow>();
        doc.SetColumns(new[]
        {
            new DataGridColumnInfo("a", "A", typeof(string), ReadOnly: false, aAccessor),
            new DataGridColumnInfo("b", "B", typeof(string), ReadOnly: false, bAccessor),
        });
        doc.AddRow(new TwoColumnRow { A = "a", B = "b" });
        doc.AddRow(new TwoColumnRow { A = "a2", B = "b2" });

        using var view = new DataGridDocumentView(doc);

        var colA = new DataGridColumn<string> { Key = "a", TypedAccessor = aAccessor, Width = GridLength.Fixed(4) };
        var colB = new DataGridColumn<string> { Key = "b", TypedAccessor = bAccessor, Width = GridLength.Fixed(4) };

        var grid = new DataGridControl { View = view };
        grid.Columns.Add(colA);
        grid.Columns.Add(colB);

        using var driver = new TerminalAppTestDriver(grid, TerminalHostKind.Fullscreen, new TerminalSize(20, 6));
        driver.Tick();

        var separatorX = grid.Bounds.X + grid.RowAnchorWidth + 4;
        var bodyY = grid.Bounds.Y + 2; // inside the body (below header)

        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Down, Button = TerminalMouseButton.Left, X = separatorX, Y = bodyY });
        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Drag, Button = TerminalMouseButton.Left, X = separatorX + 3, Y = bodyY });
        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Up, Button = TerminalMouseButton.Left, X = separatorX + 3, Y = bodyY });
        driver.Tick();

        Assert.AreEqual(GridUnitType.Fixed, colA.Width.Type);
        Assert.AreEqual(7, (int)Math.Round(colA.Width.Value));
    }

    [TestMethod]
    public void DataGrid_Allows_Resizing_Last_Column_By_Dragging_Trailing_Separator()
    {
        var aAccessor = new BindingAccessor<string>("a", o => ((TwoColumnRow)o).A, (o, v) => ((TwoColumnRow)o).A = v);
        var bAccessor = new BindingAccessor<string>("b", o => ((TwoColumnRow)o).B, (o, v) => ((TwoColumnRow)o).B = v);

        var doc = new DataGridListDocument<TwoColumnRow>();
        doc.SetColumns(new[]
        {
            new DataGridColumnInfo("a", "A", typeof(string), ReadOnly: false, aAccessor),
            new DataGridColumnInfo("b", "B", typeof(string), ReadOnly: false, bAccessor),
        });
        doc.AddRow(new TwoColumnRow { A = "a", B = "b" });

        using var view = new DataGridDocumentView(doc);

        var colA = new DataGridColumn<string> { Key = "a", TypedAccessor = aAccessor, Width = GridLength.Fixed(4) };
        var colB = new DataGridColumn<string> { Key = "b", TypedAccessor = bAccessor, Width = GridLength.Fixed(4) };

        var grid = new DataGridControl { View = view };
        grid.Columns.Add(colA);
        grid.Columns.Add(colB);

        using var driver = new TerminalAppTestDriver(grid, TerminalHostKind.Fullscreen, new TerminalSize(20, 5));
        driver.Tick();

        // Trailing boundary is after both columns (including column spacing).
        var trailingX = grid.Bounds.X + grid.RowAnchorWidth + 4 + 1 + 4;
        var headerY = grid.Bounds.Y;

        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Down, Button = TerminalMouseButton.Left, X = trailingX, Y = headerY });
        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Drag, Button = TerminalMouseButton.Left, X = trailingX + 2, Y = headerY });
        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Up, Button = TerminalMouseButton.Left, X = trailingX + 2, Y = headerY });
        driver.Tick();

        Assert.AreEqual(GridUnitType.Fixed, colB.Width.Type);
        Assert.AreEqual(6, (int)Math.Round(colB.Width.Value));
    }

    [TestMethod]
    public void DataGrid_CloseSearch_Clears_Query()
    {
        var aAccessor = new BindingAccessor<string>("a", o => ((TwoColumnRow)o).A, (o, v) => ((TwoColumnRow)o).A = v);

        var doc = new DataGridListDocument<TwoColumnRow>();
        doc.SetColumns(new[]
        {
            new DataGridColumnInfo("a", "A", typeof(string), ReadOnly: false, aAccessor),
        });
        doc.AddRow(new TwoColumnRow { A = "match" });

        using var view = new DataGridDocumentView(doc);

        var grid = new DataGridControl { View = view };
        grid.SearchQuery = new SearchQuery("match", CaseSensitive: false, WholeWord: false, UseRegex: false);

        using var driver = new TerminalAppTestDriver(grid, TerminalHostKind.Fullscreen, new TerminalSize(20, 4));
        driver.Tick();

        grid.CloseSearch();
        driver.Tick();

        Assert.IsTrue(string.IsNullOrEmpty(grid.SearchQuery.Text), "Expected closing search to clear the search highlight query.");
    }

    [TestMethod]
    public void DataGrid_Toggling_Filter_Row_Does_Not_Throw()
    {
        var aAccessor = new BindingAccessor<string>("a", o => ((TwoColumnRow)o).A, (o, v) => ((TwoColumnRow)o).A = v);

        var doc = new DataGridListDocument<TwoColumnRow>();
        doc.SetColumns(new[]
        {
            new DataGridColumnInfo("a", "A", typeof(string), ReadOnly: false, aAccessor),
        });
        doc.AddRow(new TwoColumnRow { A = "a" });

        using var view = new DataGridDocumentView(doc);

        var grid = new DataGridControl { View = view };
        using var driver = new TerminalAppTestDriver(grid, TerminalHostKind.Fullscreen, new TerminalSize(20, 6));
        driver.Tick();
        driver.App.Focus(grid);
        driver.Tick();

        // Ctrl+Shift+F toggles the filter row.
        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Unknown, Char = TerminalChar.CtrlF, Modifiers = TerminalModifiers.Ctrl | TerminalModifiers.Shift });
        driver.Tick();
    }

    [TestMethod]
    public void DataGrid_Registers_CommandBar_Commands()
    {
        var grid = new DataGridControl();

        static Command Find(Visual v, string id)
        {
            var cmd = v.Commands.FirstOrDefault(c => string.Equals(c.Id, id, StringComparison.Ordinal));
            Assert.IsNotNull(cmd, $"Expected command '{id}' to be registered.");
            return cmd;
        }

        var find = Find(grid, "DataGrid.Find");
        Assert.AreEqual(CommandPresentation.CommandBar, find.Presentation);
        Assert.AreEqual(new KeyGesture(TerminalChar.CtrlF, TerminalModifiers.Ctrl), find.Gesture);

        var toggleFilter = Find(grid, "DataGrid.ToggleFilterRow");
        Assert.AreEqual(CommandPresentation.CommandBar, toggleFilter.Presentation);
        Assert.AreEqual(new KeyGesture(TerminalChar.CtrlF, TerminalModifiers.Ctrl | TerminalModifiers.Shift), toggleFilter.Gesture);

        var selectAll = Find(grid, "DataGrid.SelectAll");
        Assert.AreEqual(CommandPresentation.CommandBar, selectAll.Presentation);
        Assert.AreEqual(new KeyGesture(TerminalChar.CtrlA, TerminalModifiers.Ctrl), selectAll.Gesture);

        var copy = Find(grid, "DataGrid.Copy");
        Assert.AreEqual(CommandPresentation.CommandBar, copy.Presentation);
        Assert.AreEqual(new KeyGesture(TerminalChar.CtrlC, TerminalModifiers.Ctrl), copy.Gesture);
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

    private sealed class IntRow
    {
        public int Value { get; set; }
    }
}
