// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Rendering;

namespace XenoAtom.Terminal.UI.Styling;

public sealed class ListBoxStyle
{
    public static ListBoxStyle Default { get; } = new();

    public static EnvironmentKey<ListBoxStyle> Key { get; } = new("ListBoxStyle", Default);

    public char MarkerGlyph { get; init; } = '▸';

    public Cell? Item { get; init; }
    public Cell? SelectedFocused { get; init; }
    public Cell? SelectedUnfocused { get; init; }
    public Cell? Disabled { get; init; }

    public Cell ResolveItemStyle(Theme theme, bool enabled, bool selected, bool focused)
    {
        var baseStyle = theme.SurfaceStyle();

        if (!enabled)
        {
            return Disabled ?? (baseStyle | TextStyle.Dim);
        }

        if (!selected)
        {
            return Item ?? baseStyle;
        }

        if (focused)
        {
            return SelectedFocused ?? theme.SelectionStyle();
        }

        return SelectedUnfocused ?? (Cell.None | TextStyle.Bold | theme.BorderStyle(focused: false));
    }
}
