// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Text;

namespace XenoAtom.Terminal.UI.Styling;

public sealed record ListBoxStyle
{
    public static ListBoxStyle Default { get; } = new();

    public static EnvironmentKey<ListBoxStyle> Key { get; } = new("ListBoxStyle", Default);

    public bool ShowBorder { get; init; }

    public Rune MarkerGlyph { get; init; } = new('>');

    public CellStyle? Item { get; init; }
    public CellStyle? SelectedFocused { get; init; }
    public CellStyle? SelectedUnfocused { get; init; }
    public CellStyle? Disabled { get; init; }

    public CellStyle ResolveItemStyle(Theme theme, bool enabled, bool selected, bool focused)
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

        return SelectedUnfocused ?? (CellStyle.None | TextStyle.Bold | theme.BorderStyle(focused: false));
    }
}

