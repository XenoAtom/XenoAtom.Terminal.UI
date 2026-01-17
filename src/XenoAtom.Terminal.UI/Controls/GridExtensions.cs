// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI.Controls;

/// <summary>
/// Provides fluent helpers for configuring grid layouts.
/// </summary>
public static partial class GridExtensions
{
    /// <summary>
    /// Sets the row definitions for a grid.
    /// </summary>
    /// <param name="grid">The target grid.</param>
    /// <param name="rows">The row definitions.</param>
    /// <returns>The grid instance.</returns>
    public static Grid Rows(this Grid grid, params RowDefinition[] rows)
    {
        ArgumentNullException.ThrowIfNull(grid);
        ArgumentNullException.ThrowIfNull(rows);
        grid.VerifyAccess();
        grid.RowDefinitions.Clear();
        grid.RowDefinitions.AddRange(rows);
        return grid;
    }

    /// <summary>
    /// Sets the column definitions for a grid.
    /// </summary>
    /// <param name="grid">The target grid.</param>
    /// <param name="columns">The column definitions.</param>
    /// <returns>The grid instance.</returns>
    public static Grid Columns(this Grid grid, params ColumnDefinition[] columns)
    {
        ArgumentNullException.ThrowIfNull(grid);
        ArgumentNullException.ThrowIfNull(columns);
        grid.VerifyAccess();
        grid.ColumnDefinitions.Clear();
        grid.ColumnDefinitions.AddRange(columns);
        return grid;
    }

    /// <summary>
    /// Adds a grid cell with the specified placement.
    /// </summary>
    /// <param name="grid">The target grid.</param>
    /// <param name="content">The cell content.</param>
    /// <param name="row">The row index.</param>
    /// <param name="column">The column index.</param>
    /// <param name="rowSpan">The row span.</param>
    /// <param name="columnSpan">The column span.</param>
    /// <returns>The grid instance.</returns>
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

    /// <summary>
    /// Adds a preconfigured grid cell.
    /// </summary>
    /// <param name="grid">The target grid.</param>
    /// <param name="cell">The grid cell to add.</param>
    /// <returns>The grid instance.</returns>
    public static Grid Cell(this Grid grid, GridCell cell)
    {
        ArgumentNullException.ThrowIfNull(grid);
        ArgumentNullException.ThrowIfNull(cell);
        grid.VerifyAccess();
        grid.Cells.Add(cell);
        return grid;
    }
}
