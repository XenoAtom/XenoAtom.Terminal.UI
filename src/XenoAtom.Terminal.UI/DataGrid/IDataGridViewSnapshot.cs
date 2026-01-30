// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI.DataGrid;

/// <summary>
/// Represents a stable snapshot of a view projection.
/// </summary>
public interface IDataGridViewSnapshot
{
    /// <summary>
    /// Gets the view snapshot version.
    /// </summary>
    int Version { get; }

    /// <summary>
    /// Gets the number of rows in view coordinates.
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
    /// Maps a view row index to a document row index.
    /// </summary>
    /// <param name="viewRowIndex">The view row index.</param>
    int MapRowToDocument(int viewRowIndex);

    /// <summary>
    /// Gets the row model object for a view row index.
    /// </summary>
    /// <param name="viewRowIndex">The view row index.</param>
    object GetRowModel(int viewRowIndex);
}

