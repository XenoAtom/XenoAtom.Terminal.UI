// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI;

public sealed class Theme
{
    public static Theme Default { get; } = new Theme
    {
        Foreground = null, // terminal default
        Background = null, // terminal default
        Border = new Rgb24(0xA0, 0xA0, 0xA0),
        FocusBorder = new Rgb24(0x2D, 0x7D, 0xFF),
        Accent = new Rgb24(0x2D, 0x7D, 0xFF),
        Selection = new Rgb24(0x2D, 0x7D, 0xFF),
        Disabled = new Rgb24(0x80, 0x80, 0x80),
    };

    public static EnvironmentKey<Theme> Key { get; } = new("Theme", Default);

    public Rgb24? Foreground { get; init; }

    public Rgb24? Background { get; init; }

    public Rgb24? Border { get; init; }

    public Rgb24? FocusBorder { get; init; }

    public Rgb24? Accent { get; init; }

    public Rgb24? Selection { get; init; }

    public Rgb24? Disabled { get; init; }

    public CellStyle BorderStyle(bool focused)
    {
        var color = focused ? FocusBorder : Border;
        var style = CellStyle.None;
        if (color is { } c)
        {
            style = style.WithForeground(c);
        }
        return style;
    }

    public CellStyle SelectionStyle()
    {
        var style = CellStyle.None;
        if (Selection is { } c)
        {
            style = style.WithBackground(c);
        }
        style |= CellStyle.Bold;
        return style;
    }
}
