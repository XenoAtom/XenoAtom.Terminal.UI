// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Ansi;

namespace XenoAtom.Terminal.UI.Styling;

public sealed class Theme : IStyle<Theme>
{
    public static Theme Default { get; } = FromPalette(Ansi16Palette.RootLoops);

    public static Theme Terminal { get; } = FromPalette(Ansi16Palette.Terminal);

    public static StyleKey<Theme> Key { get; } = new("Theme", Default);

    public static Theme FromPalette(Ansi16Palette palette)
    {
        ArgumentNullException.ThrowIfNull(palette);

        return new Theme
        {
            Foreground = palette.Foreground,
            Background = palette.Background,
            Surface = palette.Black,
            SurfaceAlt = palette.BrightBlack,
            Border = palette.BrightBlack,
            FocusBorder = palette.CursorColor,
            Accent = palette.Purple,
            Selection = palette.SelectionBackground,
            Disabled = palette.BrightBlack,
            Primary = palette.Blue,
            Success = palette.Green,
            Warning = palette.Yellow,
            Error = palette.Red,
            Muted = palette.White,
            Lines = LineGlyphs.Single,
            ScrollBars = ScrollBarGlyphs.Default,
        };
    }

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
