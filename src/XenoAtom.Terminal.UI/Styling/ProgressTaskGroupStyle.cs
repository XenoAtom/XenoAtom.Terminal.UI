// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Controls;

namespace XenoAtom.Terminal.UI.Styling;

/// <summary>
/// Defines layout defaults for a <see cref="ProgressTaskGroup"/>.
/// </summary>
public sealed record ProgressTaskGroupStyle : IStyle<ProgressTaskGroupStyle>
{
    private static readonly IReadOnlyList<ProgressTaskColumn> DefaultColumnsList =
    [
        ProgressTaskColumns.Label(),
        ProgressTaskColumns.Bar(),
        ProgressTaskColumns.Percentage(),
    ];

    /// <summary>
    /// Gets the default style for progress task groups.
    /// </summary>
    public static ProgressTaskGroupStyle Default { get; } = new();

    /// <summary>
    /// Gets the environment key used to resolve a <see cref="ProgressTaskGroupStyle"/>.
    /// </summary>
    public static StyleKey<ProgressTaskGroupStyle> Key { get; } = new("ProgressTaskGroupStyle", Default);

    /// <summary>
    /// Gets the default columns used when <see cref="ProgressTaskGroup.Columns"/> is empty.
    /// </summary>
    public IReadOnlyList<ProgressTaskColumn> DefaultColumns { get; init; } = DefaultColumnsList;

    /// <summary>
    /// Gets the spacing in cells inserted between columns.
    /// </summary>
    public int ColumnSpacing { get; init; } = 2;

    /// <summary>
    /// Gets the spacing in cells inserted between rows.
    /// </summary>
    public int RowSpacing { get; init; }
}

