// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Collections;

namespace XenoAtom.Terminal.UI.Controls;

public sealed partial class MenuItem
{
    public MenuItem(Visual header, Action? action = null)
    {
        Header = header;
        Action = action;
        Items = new BindableList<MenuItem>(this, "MenuItemList");
        IsEnabled = true;
    }

    public static MenuItem CreateSeparator()
        => new MenuItem(new TextBlock(string.Empty))
        {
            IsSeparator = true,
            IsEnabled = false,
        };

    public BindableList<MenuItem> Items { get; }

    public Visual Header { get; }

    [Bindable]
    public partial bool IsSeparator { get; set; }

    [Bindable]
    public partial bool IsEnabled { get; set; }

    [Bindable]
    public partial Visual? Icon { get; set; }

    [Bindable]
    public partial Visual? Shortcut { get; set; }

    [Bindable]
    public partial Action? Action { get; set; }
}
