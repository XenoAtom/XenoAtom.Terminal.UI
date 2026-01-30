// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI.DataGrid;

/// <summary>
/// Provides a projection of an <see cref="IDataGridDocument"/> (e.g. sorting, filtering, searching).
/// </summary>
public interface IDataGridView
{
    /// <summary>
    /// Gets the underlying document.
    /// </summary>
    IDataGridDocument Document { get; }

    /// <summary>
    /// Gets the current snapshot for the view projection.
    /// </summary>
    IDataGridViewSnapshot CurrentSnapshot { get; }

    /// <summary>
    /// Occurs when the view projection changes.
    /// </summary>
    event EventHandler<DataGridViewChangedEventArgs> Changed;
}

