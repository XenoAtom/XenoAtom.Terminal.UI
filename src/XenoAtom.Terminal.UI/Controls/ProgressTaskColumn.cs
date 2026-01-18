// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI.Controls;

/// <summary>
/// Describes a column displayed by <see cref="ProgressTaskGroup"/>.
/// </summary>
/// <param name="CellFactory">The factory used to create a cell visual for a task.</param>
public sealed record ProgressTaskColumn(Func<ProgressTask, Visual> CellFactory)
{
    /// <summary>
    /// Gets the optional column identifier.
    /// </summary>
    /// <remarks>
    /// This value is informational and can be used to identify columns when building layouts.
    /// </remarks>
    public string? Id { get; init; }

    /// <summary>
    /// Gets the column width.
    /// </summary>
    public GridLength Width { get; init; } = GridLength.Auto;

    /// <summary>
    /// Gets the minimum width in cells.
    /// </summary>
    public int MinWidth { get; init; }

    /// <summary>
    /// Gets the maximum width in cells.
    /// </summary>
    public int MaxWidth { get; init; } = int.MaxValue;

    internal Visual CreateCell(ProgressTask task)
    {
        ArgumentNullException.ThrowIfNull(task);
        var cell = CellFactory(task) ?? throw new InvalidOperationException("A progress task column factory returned null.");
        return cell;
    }
}
