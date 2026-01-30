// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI.DataGrid;

/// <summary>
/// Describes coarse categories of changes raised by <see cref="IDataGridDocument"/> and <see cref="IDataGridView"/>.
/// </summary>
[Flags]
public enum DataGridChangeKind
{
    /// <summary>
    /// No changes.
    /// </summary>
    None = 0,

    /// <summary>
    /// Rows were inserted, removed, or replaced.
    /// </summary>
    Rows = 1 << 0,

    /// <summary>
    /// Columns were inserted, removed, or moved.
    /// </summary>
    Columns = 1 << 1,

    /// <summary>
    /// Column metadata changed (header text, read-only state, accessors, etc.).
    /// </summary>
    Schema = 1 << 2,

    /// <summary>
    /// Projection settings changed (sorting, filtering, or search).
    /// </summary>
    Projection = 1 << 3,

    /// <summary>
    /// Large change; consumers should drop caches and re-measure.
    /// </summary>
    Reset = 1 << 4,
}

