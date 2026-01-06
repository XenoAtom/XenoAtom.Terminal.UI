// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Geometry;

namespace XenoAtom.Terminal.UI.Styling;

public sealed record TextBoxStyle
{
    public static TextBoxStyle Default { get; } = new();

    public static EnvironmentKey<TextBoxStyle> Key { get; } = new("TextBoxStyle", Default);

    public Thickness Padding { get; init; } = new(1, 0, 1, 0);

    public XenoAtom.Ansi.AnsiColor? Border { get; init; }
    public XenoAtom.Ansi.AnsiColor? FocusBorder { get; init; }
    public XenoAtom.Ansi.AnsiColor? Selection { get; init; }
    public XenoAtom.Ansi.AnsiColor? Background { get; init; }
    public XenoAtom.Ansi.AnsiColor? Placeholder { get; init; }

    public CellStyle BorderStyle(Theme theme, bool focused)
    {
        var color = focused ? (FocusBorder ?? theme.FocusBorder) : (Border ?? theme.Border);
        var style = CellStyle.None;
        if (color is { } c)
        {
            style = style.WithForeground(c);
        }
        return style;
    }

    public CellStyle SelectionStyle(Theme theme)
    {
        var style = CellStyle.None;
        var color = Selection ?? theme.Selection;
        if (color is { } c)
        {
            style = style.WithBackground(c);
        }
        style |= TextStyle.Bold;
        return style;
    }

    public CellStyle BackgroundStyle(Theme theme)
    {
        var style = CellStyle.None;
        if (theme.Foreground is { } fg) style = style.WithForeground(fg);
        var bg = Background ?? theme.SurfaceAlt ?? theme.Surface ?? theme.Background;
        if (bg is { } b) style = style.WithBackground(b);
        return style;
    }

    public CellStyle PlaceholderStyle(Theme theme)
    {
        var style = BackgroundStyle(theme);
        var fg = Placeholder ?? theme.Muted ?? theme.Foreground;
        if (fg is { } c) style = style.WithForeground(c);
        return style;
    }
}
