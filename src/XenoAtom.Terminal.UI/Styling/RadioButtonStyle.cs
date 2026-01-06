// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Text;

namespace XenoAtom.Terminal.UI.Styling;

public sealed record RadioButtonStyle
{
    public static RadioButtonStyle Default { get; } = new();

    public static EnvironmentKey<RadioButtonStyle> Key { get; } = new("RadioButtonStyle", Default);

    public Rune CheckedGlyph { get; init; } = new(0x25C9);
    public Rune UncheckedGlyph { get; init; } = new(0x25CB);

    public CellStyle? Normal { get; init; }
    public CellStyle? Hovered { get; init; }
    public CellStyle? Focused { get; init; }
    public CellStyle? Disabled { get; init; }

    public CellStyle Resolve(Theme theme, bool enabled, bool focused, bool hovered)
    {
        var baseStyle = theme.SurfaceStyle();

        if (!enabled)
        {
            return Disabled ?? (baseStyle | TextStyle.Dim);
        }

        if (focused)
        {
            return Focused ?? theme.SelectionStyle();
        }

        if (hovered)
        {
            if (Hovered is { } h)
            {
                return h;
            }

            var style = baseStyle;
            if (theme.Selection is { } selection)
            {
                style = style.WithBackground(selection);
            }
            return style;
        }

        return Normal ?? baseStyle;
    }
}

