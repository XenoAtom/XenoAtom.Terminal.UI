// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI.Controls;

public sealed class MenuItem
{
    public MenuItem(Visual header, Action? action = null)
    {
        Header = header ?? throw new ArgumentNullException(nameof(header));
        Action = action;
        Items = new List<MenuItem>();
    }

    public static MenuItem CreateSeparator()
        => new MenuItem(new TextBlock(string.Empty))
        {
            IsSeparator = true,
            IsEnabled = false,
        };

    public bool IsSeparator { get; init; }

    public bool IsEnabled { get; init; } = true;

    public Visual Header { get; init; }

    public Visual? Icon { get; init; }

    public Visual? Shortcut { get; init; }

    public Action? Action { get; init; }

    public List<MenuItem> Items { get; }
}
