// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI;

public sealed class ListBoxStyle
{
    public static ListBoxStyle Default { get; } = new();

    public static EnvironmentKey<ListBoxStyle> Key { get; } = new("ListBoxStyle", Default);

    public CellStyle? Item { get; init; }
    public CellStyle? SelectedFocused { get; init; }
    public CellStyle? SelectedUnfocused { get; init; }
    public CellStyle? Disabled { get; init; }

    public CellStyle ResolveItemStyle(Theme theme, bool enabled, bool selected, bool focused)
    {
        if (!enabled)
        {
            return Disabled ?? CellStyle.Dim;
        }

        if (!selected)
        {
            return Item ?? CellStyle.None;
        }

        if (focused)
        {
            return SelectedFocused ?? theme.SelectionStyle();
        }

        return SelectedUnfocused ?? (CellStyle.Bold | theme.BorderStyle(focused: false));
    }
}
