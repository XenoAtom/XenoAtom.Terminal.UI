// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI.Controls;

/// <summary>
/// Represents a grid cell that hosts a single child within a grid.
/// </summary>
public sealed partial class GridCell : ContentVisual
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GridCell"/> class.
    /// </summary>
    public GridCell()
    {
        HorizontalAlignment = Align.Stretch;
        VerticalAlignment = Align.Stretch;
        RowSpan = 1;
        ColumnSpan = 1;
    }

    /// <summary>
    /// Gets or sets the row index.
    /// </summary>
    [Bindable]
    public partial int Row { get; set; }

    /// <summary>
    /// Gets or sets the column index.
    /// </summary>
    [Bindable]
    public partial int Column { get; set; }

    /// <summary>
    /// Gets or sets the row span.
    /// </summary>
    [Bindable]
    public partial int RowSpan { get; set; }

    /// <summary>
    /// Gets or sets the column span.
    /// </summary>
    [Bindable]
    public partial int ColumnSpan { get; set; }

    partial void OnRowChanging(ref int value) => value = Math.Max(0, value);

    partial void OnColumnChanging(ref int value) => value = Math.Max(0, value);

    partial void OnRowSpanChanging(ref int value) => value = Math.Max(1, value);

    partial void OnColumnSpanChanging(ref int value) => value = Math.Max(1, value);
}
