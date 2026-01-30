// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI.DataGrid;

/// <summary>
/// Provides change information for <see cref="IDataGridDocument.Changed"/>.
/// </summary>
public sealed class DataGridDocumentChangedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the previous version number.
    /// </summary>
    public required int OldVersion { get; init; }

    /// <summary>
    /// Gets the new version number.
    /// </summary>
    public required int NewVersion { get; init; }

    /// <summary>
    /// Gets the kind of change.
    /// </summary>
    public required DataGridChangeKind Kind { get; init; }

    /// <summary>
    /// Gets the affected row index, or <c>-1</c> if not applicable.
    /// </summary>
    public int RowIndex { get; init; } = -1;

    /// <summary>
    /// Gets the number of affected rows.
    /// </summary>
    public int RowCount { get; init; }

    /// <summary>
    /// Gets the affected column index, or <c>-1</c> if not applicable.
    /// </summary>
    public int ColumnIndex { get; init; } = -1;

    /// <summary>
    /// Gets the number of affected columns.
    /// </summary>
    public int ColumnCount { get; init; }
}

