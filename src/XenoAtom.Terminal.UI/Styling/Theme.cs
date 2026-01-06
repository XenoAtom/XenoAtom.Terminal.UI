// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Rendering;

namespace XenoAtom.Terminal.UI.Styling;

public sealed class Theme
{
    public static Theme Default { get; } = new Theme
    {
        Foreground = new Rgb24(0xE5, 0xE7, 0xEB), // slate-200
        Background = new Rgb24(0x0B, 0x12, 0x20), // deep slate
        Surface = new Rgb24(0x10, 0x1A, 0x2D),
        SurfaceAlt = new Rgb24(0x18, 0x24, 0x3A),
        Border = new Rgb24(0x2A, 0x3A, 0x55),
        FocusBorder = new Rgb24(0x60, 0xA5, 0xFA), // blue-400
        Accent = new Rgb24(0xA7, 0x8B, 0xFA), // violet-400
        Selection = new Rgb24(0x2D, 0x7D, 0xFF),
        Disabled = new Rgb24(0x64, 0x74, 0x8B), // slate-500
        Primary = new Rgb24(0xA7, 0x8B, 0xFA),
        Success = new Rgb24(0x34, 0xD3, 0x99),
        Warning = new Rgb24(0xFB, 0xBF, 0x24),
        Error = new Rgb24(0xFB, 0x71, 0x85),
        Muted = new Rgb24(0x94, 0xA3, 0xB8),
        Lines = LineGlyphs.Single,
        ScrollBars = ScrollBarGlyphs.Default,
    };

    public static EnvironmentKey<Theme> Key { get; } = new("Theme", Default);

    public Rgb24? Foreground { get; init; }

    public Rgb24? Background { get; init; }

    public Rgb24? Surface { get; init; }

    public Rgb24? SurfaceAlt { get; init; }

    public Rgb24? Border { get; init; }

    public Rgb24? FocusBorder { get; init; }

    public Rgb24? Accent { get; init; }

    public Rgb24? Selection { get; init; }

    public Rgb24? Disabled { get; init; }

    public Rgb24? Primary { get; init; }

    public Rgb24? Success { get; init; }

    public Rgb24? Warning { get; init; }

    public Rgb24? Error { get; init; }

    public Rgb24? Muted { get; init; }

    public LineGlyphs Lines { get; init; } = LineGlyphs.Single;

    public ScrollBarGlyphs ScrollBars { get; init; } = ScrollBarGlyphs.Default;

    public CellStyle BaseTextStyle()
    {
        var style = CellStyle.None;
        if (Foreground is { } fg)
        {
            style = style.WithForeground(fg);
        }
        if (Background is { } bg)
        {
            style = style.WithBackground(bg);
        }
        return style;
    }

    public CellStyle SurfaceStyle()
    {
        var style = CellStyle.None;
        if (Foreground is { } fg)
        {
            style = style.WithForeground(fg);
        }
        if (Surface is { } bg)
        {
            style = style.WithBackground(bg);
        }
        return style;
    }

    public CellStyle MutedTextStyle()
    {
        var style = BaseTextStyle();
        if (Muted is { } m)
        {
            style = style.WithForeground(m);
        }
        return style;
    }

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
