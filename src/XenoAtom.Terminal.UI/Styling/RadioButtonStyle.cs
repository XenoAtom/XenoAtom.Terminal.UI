// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Text;

namespace XenoAtom.Terminal.UI.Styling;

public sealed record RadioButtonStyle : IStyle<RadioButtonStyle>
{
    public static RadioButtonStyle Default { get; } = new();

    public static StyleKey<RadioButtonStyle> Key { get; } = new("RadioButtonStyle", Default);

    public Rune CheckedGlyph { get; init; } = new(0x25C9);
    public Rune UncheckedGlyph { get; init; } = new(0x25CB);

    public CellStyle? Normal { get; init; }
    public CellStyle? Hovered { get; init; }
    public CellStyle? Focused { get; init; }
    public CellStyle? Disabled { get; init; }

    public CellStyle Resolve(Theme theme, bool enabled, bool focused, bool hovered)
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

        if (focused)
        {
            if (Focused is { } f)
            {
                return f;
            }

            var focusedStyle = baseStyle | TextStyle.Bold;
            if (theme.FocusBorder is { } c)
            {
                focusedStyle = focusedStyle.WithForeground(c);
            }
            return focusedStyle;
        }

        if (hovered)
        {
            if (Hovered is { } h)
            {
                return h;
            }

            var style = baseStyle;
            if (theme.Accent is { } selection)
            {
                style = style.WithForeground(selection);
            }
            return style;
        }

        return Normal ?? baseStyle;
    }
}
