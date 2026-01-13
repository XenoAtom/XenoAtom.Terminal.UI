// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI.Controls;

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

    public static Grid Cell(this Grid grid, Visual content, int row, int column, int rowSpan = 1, int columnSpan = 1)
    {
        ArgumentNullException.ThrowIfNull(grid);
        ArgumentNullException.ThrowIfNull(content);
        grid.VerifyAccess();

        var cell = new GridCell
        {
            Content = content,
            Row = row,
            Column = column,
            RowSpan = rowSpan,
            ColumnSpan = columnSpan,
        };

        grid.Cells.Add(cell);
        return grid;
    }

    public static Grid Cell(this Grid grid, GridCell cell)
    {
        ArgumentNullException.ThrowIfNull(grid);
        ArgumentNullException.ThrowIfNull(cell);
        grid.VerifyAccess();
        grid.Cells.Add(cell);
        return grid;
    }
}
