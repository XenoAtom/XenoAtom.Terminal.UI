// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Runtime.CompilerServices;

namespace XenoAtom.Terminal.UI.Controls;

public sealed partial class Grid
{
    private readonly record struct GridPlacement(int Row, int Column, int RowSpan, int ColumnSpan)
    {
        public GridPlacement WithRow(int row) => this with { Row = row };
        public GridPlacement WithColumn(int column) => this with { Column = column };
        public GridPlacement WithRowSpan(int rowSpan) => this with { RowSpan = rowSpan };
        public GridPlacement WithColumnSpan(int columnSpan) => this with { ColumnSpan = columnSpan };
    }

    private sealed class PlacementHolder
    {
        public GridPlacement Placement;
    }

    private static readonly ConditionalWeakTable<Visual, PlacementHolder> Placements = new();

    public static void SetRow(Visual visual, int row) => UpdatePlacement(visual, p => p.WithRow(row));

    public static int GetRow(Visual visual) => GetPlacement(visual).Row;

    public static void SetColumn(Visual visual, int column) => UpdatePlacement(visual, p => p.WithColumn(column));

    public static int GetColumn(Visual visual) => GetPlacement(visual).Column;

    public static void SetRowSpan(Visual visual, int rowSpan) => UpdatePlacement(visual, p => p.WithRowSpan(rowSpan));

    public static int GetRowSpan(Visual visual) => GetPlacement(visual).RowSpan;

    public static void SetColumnSpan(Visual visual, int columnSpan) => UpdatePlacement(visual, p => p.WithColumnSpan(columnSpan));

    public static int GetColumnSpan(Visual visual) => GetPlacement(visual).ColumnSpan;

    private static GridPlacement GetPlacement(Visual visual)
    {
        ArgumentNullException.ThrowIfNull(visual);
        if (Placements.TryGetValue(visual, out var holder))
        {
            return holder.Placement;
        }

        return new GridPlacement(0, 0, 1, 1);
    }

    private static void UpdatePlacement(Visual visual, Func<GridPlacement, GridPlacement> update)
    {
        ArgumentNullException.ThrowIfNull(visual);
        ArgumentNullException.ThrowIfNull(update);

        var holder = Placements.GetValue(visual, _ => new PlacementHolder { Placement = new GridPlacement(0, 0, 1, 1) });
        var before = holder.Placement;
        var after = update(before);

        after = after with
        {
            Row = Math.Max(0, after.Row),
            Column = Math.Max(0, after.Column),
            RowSpan = Math.Max(1, after.RowSpan),
            ColumnSpan = Math.Max(1, after.ColumnSpan),
        };

        if (before.Equals(after))
        {
            return;
        }

        holder.Placement = after;

        // Placement affects layout, so ensure the parent (usually a Grid) is re-measured.
        visual.Parent?.MarkMeasureDirty();
    }

    private static GridPlacement GetPlacementForLayout(Visual child, int maxRows, int maxCols)
    {
        var p = GetPlacement(child);
        var row = Math.Clamp(p.Row, 0, Math.Max(0, maxRows - 1));
        var col = Math.Clamp(p.Column, 0, Math.Max(0, maxCols - 1));
        var rowSpan = Math.Clamp(p.RowSpan, 1, Math.Max(1, maxRows - row));
        var colSpan = Math.Clamp(p.ColumnSpan, 1, Math.Max(1, maxCols - col));
        return p with { Row = row, Column = col, RowSpan = rowSpan, ColumnSpan = colSpan };
    }
}

public static partial class GridExtensions
{
    public static Grid Rows(this Grid grid, params RowDefinition[] rows)
    {
        ArgumentNullException.ThrowIfNull(grid);
        ArgumentNullException.ThrowIfNull(rows);
        grid.VerifyAccess();
        grid.RowDefinitions.Clear();
        grid.RowDefinitions.AddRange(rows);
        return grid;
    }

    public static Grid Columns(this Grid grid, params ColumnDefinition[] columns)
    {
        ArgumentNullException.ThrowIfNull(grid);
        ArgumentNullException.ThrowIfNull(columns);
        grid.VerifyAccess();
        grid.ColumnDefinitions.Clear();
        grid.ColumnDefinitions.AddRange(columns);
        return grid;
    }

    public static T Row<T>(this T visual, int row) where T : Visual
    {
        Grid.SetRow(visual, row);
        return visual;
    }

    public static T Column<T>(this T visual, int column) where T : Visual
    {
        Grid.SetColumn(visual, column);
        return visual;
    }

    public static T RowSpan<T>(this T visual, int rowSpan) where T : Visual
    {
        Grid.SetRowSpan(visual, rowSpan);
        return visual;
    }

    public static T ColumnSpan<T>(this T visual, int columnSpan) where T : Visual
    {
        Grid.SetColumnSpan(visual, columnSpan);
        return visual;
    }
}
