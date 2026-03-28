// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Linq;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.DataGrid;
using XenoAtom.Terminal.UI.Hosting;
using XenoAtom.Terminal.UI.Layout;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class DataGridSortingTests
{
    [TestMethod]
    public void DataGrid_Programmatic_Sort_Uses_Custom_Comparer()
    {
        var nameAccessor = new BindingAccessor<string>("name", o => ((SortRow)o).Name, (o, v) => ((SortRow)o).Name = v);

        var doc = new DataGridListDocument<SortRow>();
        doc.SetColumns(new[]
        {
            new DataGridColumnInfo("name", "Name", typeof(string), ReadOnly: false, nameAccessor),
        });

        doc.AddRow(new SortRow { Name = "bbb" });
        doc.AddRow(new SortRow { Name = "a" });
        doc.AddRow(new SortRow { Name = "cc" });

        using var view = new DataGridDocumentView(doc);

        var grid = new DataGridControl { View = view };
        grid.Columns.Add(new DataGridColumn<string>
        {
            Key = "name",
            TypedValueAccessor = nameAccessor,
            Width = GridLength.Auto,
            Sortable = true,
            SortComparer = Comparer<string>.Create(static (left, right) =>
            {
                var length = left.Length.CompareTo(right.Length);
                return length != 0 ? length : string.Compare(left, right, StringComparison.Ordinal);
            }),
        });

        Assert.IsTrue(grid.TrySetColumnSortDirection("name", DataGridSortDirection.Ascending));

        var orderedNames = Enumerable.Range(0, view.CurrentSnapshot.RowCount)
            .Select(i => ((SortRow)view.CurrentSnapshot.GetRowModel(i)).Name)
            .ToArray();

        CollectionAssert.AreEqual(new[] { "a", "cc", "bbb" }, orderedNames);
    }

    [TestMethod]
    public void DataGrid_Additive_Sorts_Are_Stable()
    {
        var groupAccessor = new BindingAccessor<string>("group", o => ((GroupedSortRow)o).Group, (o, v) => ((GroupedSortRow)o).Group = v);
        var nameAccessor = new BindingAccessor<string>("name", o => ((GroupedSortRow)o).Name, (o, v) => ((GroupedSortRow)o).Name = v);
        var sequenceAccessor = new BindingAccessor<int>("sequence", o => ((GroupedSortRow)o).Sequence, (o, v) => ((GroupedSortRow)o).Sequence = v);

        var doc = new DataGridListDocument<GroupedSortRow>();
        doc.SetColumns(new[]
        {
            new DataGridColumnInfo("group", "Group", typeof(string), ReadOnly: false, groupAccessor),
            new DataGridColumnInfo("name", "Name", typeof(string), ReadOnly: false, nameAccessor),
            new DataGridColumnInfo("sequence", "Sequence", typeof(int), ReadOnly: true, sequenceAccessor),
        });

        doc.AddRow(new GroupedSortRow { Group = "B", Name = "x", Sequence = 0 });
        doc.AddRow(new GroupedSortRow { Group = "A", Name = "x", Sequence = 1 });
        doc.AddRow(new GroupedSortRow { Group = "A", Name = "x", Sequence = 2 });
        doc.AddRow(new GroupedSortRow { Group = "A", Name = "y", Sequence = 3 });
        doc.AddRow(new GroupedSortRow { Group = "B", Name = "x", Sequence = 4 });

        using var view = new DataGridDocumentView(doc);

        var grid = new DataGridControl { View = view };
        grid.Columns.Add(new DataGridColumn<string> { Key = "group", TypedValueAccessor = groupAccessor, Width = GridLength.Auto, Sortable = true });
        grid.Columns.Add(new DataGridColumn<string> { Key = "name", TypedValueAccessor = nameAccessor, Width = GridLength.Auto, Sortable = true });
        grid.Columns.Add(new DataGridColumn<int> { Key = "sequence", TypedValueAccessor = sequenceAccessor, Width = GridLength.Auto });

        Assert.IsTrue(grid.TrySetColumnSortDirection("group", DataGridSortDirection.Ascending));
        Assert.IsTrue(grid.TrySetColumnSortDirection("name", DataGridSortDirection.Ascending, additive: true));

        var orderedSequence = Enumerable.Range(0, view.CurrentSnapshot.RowCount)
            .Select(i => ((GroupedSortRow)view.CurrentSnapshot.GetRowModel(i)).Sequence)
            .ToArray();

        CollectionAssert.AreEqual(new[] { 1, 2, 3, 0, 4 }, orderedSequence);
    }

    [TestMethod]
    public void DataGrid_Header_Sort_Button_Cycles_Directions_On_Click()
    {
        var nameAccessor = new BindingAccessor<string>("name", o => ((SortRow)o).Name, (o, v) => ((SortRow)o).Name = v);

        var doc = new DataGridListDocument<SortRow>();
        doc.SetColumns(new[]
        {
            new DataGridColumnInfo("name", "Name", typeof(string), ReadOnly: false, nameAccessor),
        });

        doc.AddRow(new SortRow { Name = "b" });
        doc.AddRow(new SortRow { Name = "c" });
        doc.AddRow(new SortRow { Name = "a" });

        using var view = new DataGridDocumentView(doc);

        var grid = new DataGridControl { View = view, ShowRowAnchor = false };
        grid.Columns.Add(new DataGridColumn<string>
        {
            Key = "name",
            TypedValueAccessor = nameAccessor,
            Width = GridLength.Fixed(6),
            Sortable = true,
        });

        using var driver = new TerminalAppTestDriver(grid, TerminalHostKind.Fullscreen, new TerminalSize(8, 5));
        driver.Tick();

        static string GetRenderedText(TerminalAppTestDriver currentDriver)
        {
            var screen = new AnsiTestScreen(8, 5);
            screen.Apply(currentDriver.Backend.GetOutText());
            return screen.GetText();
        }

        StringAssert.Contains(GetRenderedText(driver), "□");

        var sortButtonX = grid.Bounds.X + 5;
        var sortButtonY = grid.Bounds.Y;

        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Down, Button = TerminalMouseButton.Left, X = sortButtonX, Y = sortButtonY });
        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Up, Button = TerminalMouseButton.Left, X = sortButtonX, Y = sortButtonY });
        driver.Tick();

        Assert.AreEqual(DataGridSortDirection.Descending, grid.GetColumnSortDirection("name"));
        StringAssert.Contains(GetRenderedText(driver), "↓");
        Assert.AreEqual("c", ((SortRow)view.CurrentSnapshot.GetRowModel(0)).Name);

        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Down, Button = TerminalMouseButton.Left, X = sortButtonX, Y = sortButtonY });
        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Up, Button = TerminalMouseButton.Left, X = sortButtonX, Y = sortButtonY });
        driver.Tick();

        Assert.AreEqual(DataGridSortDirection.Ascending, grid.GetColumnSortDirection("name"));
        StringAssert.Contains(GetRenderedText(driver), "↑");
        Assert.AreEqual("a", ((SortRow)view.CurrentSnapshot.GetRowModel(0)).Name);
    }

    [TestMethod]
    public void DataGrid_Header_Sort_Button_Uses_Ctrl_Click_For_Additive_Sort()
    {
        var groupAccessor = new BindingAccessor<string>("group", o => ((GroupedSortRow)o).Group, (o, v) => ((GroupedSortRow)o).Group = v);
        var nameAccessor = new BindingAccessor<string>("name", o => ((GroupedSortRow)o).Name, (o, v) => ((GroupedSortRow)o).Name = v);

        var doc = new DataGridListDocument<GroupedSortRow>();
        doc.SetColumns(new[]
        {
            new DataGridColumnInfo("group", "Group", typeof(string), ReadOnly: false, groupAccessor),
            new DataGridColumnInfo("name", "Name", typeof(string), ReadOnly: false, nameAccessor),
        });

        doc.AddRow(new GroupedSortRow { Group = "B", Name = "a", Sequence = 0 });
        doc.AddRow(new GroupedSortRow { Group = "A", Name = "b", Sequence = 1 });
        doc.AddRow(new GroupedSortRow { Group = "A", Name = "a", Sequence = 2 });

        using var view = new DataGridDocumentView(doc);

        var grid = new DataGridControl { View = view, ShowRowAnchor = false };
        grid.Columns.Add(new DataGridColumn<string> { Key = "group", TypedValueAccessor = groupAccessor, Width = GridLength.Fixed(7), Sortable = true });
        grid.Columns.Add(new DataGridColumn<string> { Key = "name", TypedValueAccessor = nameAccessor, Width = GridLength.Fixed(6), Sortable = true });

        using var driver = new TerminalAppTestDriver(grid, TerminalHostKind.Fullscreen, new TerminalSize(16, 5));
        driver.Tick();

        var groupSortButtonX = grid.Bounds.X + 6;
        var nameSortButtonX = grid.Bounds.X + 13;
        var sortButtonY = grid.Bounds.Y;

        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Down, Button = TerminalMouseButton.Left, X = groupSortButtonX, Y = sortButtonY });
        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Up, Button = TerminalMouseButton.Left, X = groupSortButtonX, Y = sortButtonY });
        driver.Tick();

        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Down, Button = TerminalMouseButton.Left, X = nameSortButtonX, Y = sortButtonY, Modifiers = TerminalModifiers.Ctrl });
        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Up, Button = TerminalMouseButton.Left, X = nameSortButtonX, Y = sortButtonY, Modifiers = TerminalModifiers.Ctrl });
        driver.Tick();

        var sorts = grid.SortDescriptions.ToArray();
        CollectionAssert.AreEqual(
            new[]
            {
                new DataGridSortDescription("group", DataGridSortDirection.Descending),
                new DataGridSortDescription("name", DataGridSortDirection.Descending),
            },
            sorts);
    }

    private sealed class SortRow
    {
        public string Name { get; set; } = string.Empty;
    }

    private sealed class GroupedSortRow
    {
        public string Group { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int Sequence { get; set; }
    }
}
