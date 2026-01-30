// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI.DataGrid;

/// <summary>
/// Represents an immutable snapshot of a data grid document.
/// </summary>
public interface IDataGridSnapshot
{
    /// <summary>
    /// Gets the snapshot version.
    /// </summary>
    int Version { get; }

    /// <summary>
    /// Gets the number of rows.
    /// </summary>
    int RowCount { get; }

    /// <summary>
    /// Gets the number of columns.
    /// </summary>
    int ColumnCount { get; }

    /// <summary>
    /// Gets column metadata for the specified index.
    /// </summary>
    /// <param name="columnIndex">The column index.</param>
    DataGridColumnInfo GetColumn(int columnIndex);

    /// <summary>
    /// Gets the row model object for a row index.
    /// </summary>
    /// <param name="rowIndex">The row index.</param>
    object GetRowModel(int rowIndex);
}

