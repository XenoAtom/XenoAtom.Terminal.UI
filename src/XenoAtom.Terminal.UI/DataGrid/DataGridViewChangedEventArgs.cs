// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI.DataGrid;

/// <summary>
/// Provides change information for <see cref="IDataGridView.Changed"/>.
/// </summary>
public sealed class DataGridViewChangedEventArgs : EventArgs
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
}

