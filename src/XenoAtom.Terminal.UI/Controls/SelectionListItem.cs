// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI.Controls;

public sealed partial class SelectionListItem : ContentVisual
{
    public SelectionListItem()
    {
    }

    public SelectionListItem(Visual content, bool isChecked = false)
    {
        this.Content(content);
        this.IsChecked(isChecked);
    }

    [Bindable]
    public partial bool IsChecked { get; set; }
}

