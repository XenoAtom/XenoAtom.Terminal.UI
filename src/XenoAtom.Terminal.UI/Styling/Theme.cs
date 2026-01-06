// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Ansi;

namespace XenoAtom.Terminal.UI.Styling;

public sealed class Theme
{
    public static Theme Default { get; } = new Theme
    {
        Foreground = AnsiColor.Rgb(0xE5, 0xE7, 0xEB), // slate-200
        Background = AnsiColor.Rgb(0x0B, 0x12, 0x20), // deep slate
        Surface = AnsiColor.Rgb(0x10, 0x1A, 0x2D),
        SurfaceAlt = AnsiColor.Rgb(0x18, 0x24, 0x3A),
        Border = AnsiColor.Rgb(0x2A, 0x3A, 0x55),
        FocusBorder = AnsiColor.Rgb(0x60, 0xA5, 0xFA), // blue-400
        Accent = AnsiColor.Rgb(0xA7, 0x8B, 0xFA), // violet-400
        Selection = AnsiColor.Rgb(0x2D, 0x7D, 0xFF),
        Disabled = AnsiColor.Rgb(0x64, 0x74, 0x8B), // slate-500
        Primary = AnsiColor.Rgb(0xA7, 0x8B, 0xFA),
        Success = AnsiColor.Rgb(0x34, 0xD3, 0x99),
        Warning = AnsiColor.Rgb(0xFB, 0xBF, 0x24),
        Error = AnsiColor.Rgb(0xFB, 0x71, 0x85),
        Muted = AnsiColor.Rgb(0x94, 0xA3, 0xB8),
        Lines = LineGlyphs.Single,
        ScrollBars = ScrollBarGlyphs.Default,
    };

    public static EnvironmentKey<Theme> Key { get; } = new("Theme", Default);

    public AnsiColor? Foreground { get; init; }

    public AnsiColor? Background { get; init; }

    public AnsiColor? Surface { get; init; }

    public AnsiColor? SurfaceAlt { get; init; }

    public AnsiColor? Border { get; init; }

    public AnsiColor? FocusBorder { get; init; }

    public AnsiColor? Accent { get; init; }

    public AnsiColor? Selection { get; init; }

    public AnsiColor? Disabled { get; init; }

    public AnsiColor? Primary { get; init; }

    public AnsiColor? Success { get; init; }

    public AnsiColor? Warning { get; init; }

    public AnsiColor? Error { get; init; }

    public AnsiColor? Muted { get; init; }

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

    public CellStyle ForegroundTextStyle()
    {
        var style = CellStyle.None;
        if (Foreground is { } fg)
        {
            style = style.WithForeground(fg);
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
        style |= TextStyle.Bold;
        return style;
    }
}
