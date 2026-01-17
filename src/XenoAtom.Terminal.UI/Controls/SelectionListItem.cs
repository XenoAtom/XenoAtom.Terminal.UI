// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI.Controls;

/// <summary>
/// Represents an item in a <see cref="SelectionList"/>.
/// </summary>
public sealed partial class SelectionListItem : ContentVisual
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SelectionListItem"/> class.
    /// </summary>
    public SelectionListItem()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SelectionListItem"/> class with content.
    /// </summary>
    /// <param name="content">The item content.</param>
    /// <param name="isChecked">Initial checked state.</param>
    public SelectionListItem(Visual content, bool isChecked = false)
    {
        this.Content(content);
        this.IsChecked(isChecked);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the item is checked.
    /// </summary>
    [Bindable]
    public partial bool IsChecked { get; set; }
}
