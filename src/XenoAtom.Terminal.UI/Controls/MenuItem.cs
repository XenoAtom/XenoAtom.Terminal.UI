// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Collections;

namespace XenoAtom.Terminal.UI.Controls;

/// <summary>
/// Represents a menu item with optional submenu items.
/// </summary>
public sealed partial class MenuItem
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MenuItem"/> class.
    /// </summary>
    /// <param name="header">The header content.</param>
    /// <param name="action">The action to invoke.</param>
    public MenuItem(Visual header, Action? action = null)
    {
        Header = header;
        Action = action;
        Items = new BindableList<MenuItem>(this, "MenuItemList");
        IsEnabled = true;
    }

    /// <summary>
    /// Creates a separator menu item.
    /// </summary>
    /// <returns>The separator menu item.</returns>
    public static MenuItem CreateSeparator()
        => new MenuItem(new TextBlock(string.Empty))
        {
            IsSeparator = true,
            IsEnabled = false,
        };

    /// <summary>
    /// Gets the submenu items.
    /// </summary>
    public BindableList<MenuItem> Items { get; }

    /// <summary>
    /// Gets the header content.
    /// </summary>
    public Visual Header { get; }

    /// <summary>
    /// Gets or sets a value indicating whether this item is a separator.
    /// </summary>
    [Bindable]
    public partial bool IsSeparator { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the item is enabled.
    /// </summary>
    [Bindable]
    public partial bool IsEnabled { get; set; }

    /// <summary>
    /// Gets or sets the icon visual.
    /// </summary>
    [Bindable]
    public partial Visual? Icon { get; set; }

    /// <summary>
    /// Gets or sets the shortcut visual.
    /// </summary>
    [Bindable]
    public partial Visual? Shortcut { get; set; }

    /// <summary>
    /// Gets or sets the action to invoke when activated.
    /// </summary>
    [Bindable]
    public partial Action? Action { get; set; }
}
