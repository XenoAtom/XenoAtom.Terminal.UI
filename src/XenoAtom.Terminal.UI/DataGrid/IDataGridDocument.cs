// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI.DataGrid;

/// <summary>
/// Represents a mutable tabular document that provides snapshots and change notifications.
/// </summary>
public interface IDataGridDocument
{
    /// <summary>
    /// Gets the current snapshot of the document.
    /// </summary>
    IDataGridSnapshot CurrentSnapshot { get; }

    /// <summary>
    /// Gets the current version number for the document.
    /// </summary>
    int Version { get; }

    /// <summary>
    /// Begins a batch update scope.
    /// </summary>
    IDisposable BeginUpdate();

    /// <summary>
    /// Inserts a row model at the specified index.
    /// </summary>
    /// <param name="rowIndex">The index at which to insert the row model.</param>
    /// <param name="rowModel">The row model to insert.</param>
    void InsertRow(int rowIndex, object rowModel);

    /// <summary>
    /// Replaces the row model at the specified index.
    /// </summary>
    /// <param name="rowIndex">The index of the row model to replace.</param>
    /// <param name="rowModel">The new row model instance.</param>
    void ReplaceRow(int rowIndex, object rowModel);

    /// <summary>
    /// Removes one or more rows from the document.
    /// </summary>
    /// <param name="rowIndex">The index of the first row to remove.</param>
    /// <param name="count">The number of rows to remove.</param>
    void RemoveRows(int rowIndex, int count);

    /// <summary>
    /// Occurs when the document structure or schema changes (rows/columns/schema/reset).
    /// </summary>
    event EventHandler<DataGridDocumentChangedEventArgs> Changed;
}

