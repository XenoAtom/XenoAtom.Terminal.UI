// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Text;

namespace XenoAtom.Terminal.UI.Styling;

public sealed record SplitterStyle : IStyle<SplitterStyle>
{
    public static SplitterStyle Default { get; } = new();

    public static StyleKey<SplitterStyle> Key { get; } = new("SplitterStyle", Default);

    public Rune HorizontalGlyph { get; init; } = new(0x2500); // ─

    public Rune VerticalGlyph { get; init; } = new(0x2502); // │

    public CellStyle? BarStyle { get; init; }
    public CellStyle? HoverStyle { get; init; }
    public CellStyle? FocusStyle { get; init; }
    public CellStyle? DragStyle { get; init; }
    public CellStyle? DisabledStyle { get; init; }

    public CellStyle Resolve(Theme theme, bool enabled, bool focused, bool hovered, bool dragging)
    {
        if (!enabled)
        {
            if (DisabledStyle is { } d)
            {
                return d;
            }

            var dim = theme.BorderStyle(focused: false) | TextStyle.Dim;
            if (theme.Disabled is { } c)
            {
                dim = dim.WithForeground(c);
            }
            return dim;
        }

        if (dragging)
        {
            return DragStyle ?? (theme.BorderStyle(focused: true) | TextStyle.Bold);
        }

        if (hovered)
        {
            return HoverStyle ?? (theme.BorderStyle(focused: true) | TextStyle.Bold);
        }

        if (focused)
        {
            return FocusStyle ?? (theme.BorderStyle(focused: true));
        }

        return BarStyle ?? theme.BorderStyle(focused: false);
    }
}

