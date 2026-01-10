// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Text;

namespace XenoAtom.Terminal.UI.Styling;

public sealed record SelectionListStyle : IStyle<SelectionListStyle>
{
    public static SelectionListStyle Default { get; } = new();

    public static StyleKey<SelectionListStyle> Key { get; } = new("SelectionListStyle", Default);

    public bool ShowBorder { get; init; }

    public int SpaceBetweenGlyphAndText { get; init; } = 2;

    public Rune FocusMarkerGlyph { get; init; } = new('→');

    public Rune CheckedGlyph { get; init; } = new(0x2611); // ☑

    public Rune UncheckedGlyph { get; init; } = new(0x2610); // ☐

    public CellStyle? Item { get; init; }
    public CellStyle? SelectedFocused { get; init; }
    public CellStyle? SelectedUnfocused { get; init; }
    public CellStyle? Disabled { get; init; }

    public CellStyle ResolveItemStyle(Theme theme, bool enabled, bool selected, bool focused)
    {
        var baseStyle = theme.ForegroundTextStyle();

        if (!enabled)
        {
            if (Disabled is { } d)
            {
                return d;
            }

            var disabled = baseStyle | TextStyle.Dim;
            if (theme.Disabled is { } c)
            {
                disabled = disabled.WithForeground(c);
            }
            return disabled;
        }

        if (!selected)
        {
            return Item ?? baseStyle;
        }

        if (focused)
        {
            if (SelectedFocused is { } selectedFocused)
            {
                return selectedFocused;
            }

            var selectedStyle = baseStyle | TextStyle.Bold;
            if (theme.FocusBorder is { } c)
            {
                selectedStyle = selectedStyle.WithForeground(c);
            }
            return selectedStyle;
        }

        return SelectedUnfocused ?? (CellStyle.None | TextStyle.Bold | theme.BorderStyle(focused: false));
    }
}
