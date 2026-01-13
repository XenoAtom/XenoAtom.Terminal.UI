// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI.Controls;

public sealed partial class GridCell : ContentVisual
{
    public GridCell()
    {
        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Stretch;
        RowSpan = 1;
        ColumnSpan = 1;
    }

    [Bindable]
    public partial int Row { get; set; }

    [Bindable]
    public partial int Column { get; set; }

    [Bindable]
    public partial int RowSpan { get; set; }

    [Bindable]
    public partial int ColumnSpan { get; set; }

    partial void OnRowChanging(ref int value) => value = Math.Max(0, value);

    partial void OnColumnChanging(ref int value) => value = Math.Max(0, value);

    partial void OnRowSpanChanging(ref int value) => value = Math.Max(1, value);

    partial void OnColumnSpanChanging(ref int value) => value = Math.Max(1, value);
}

