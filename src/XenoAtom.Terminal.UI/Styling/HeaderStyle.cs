// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI.Styling;

public sealed record HeaderStyle : IStyle<HeaderStyle>
{
    public static HeaderStyle Default { get; } = new();

    public static StyleKey<HeaderStyle> Key { get; } = new("HeaderStyle", Default);

    public XenoAtom.Ansi.AnsiColor? Background { get; init; }
    public XenoAtom.Ansi.AnsiColor? Foreground { get; init; }

    public CellStyle Resolve(Theme theme)
    {
        var style = CellStyle.None;
        var fg = Foreground ?? theme.Foreground;
        var bg = Background ?? theme.SurfaceAlt;

        if (fg is { } f) style = style.WithForeground(f);
        if (bg is { } b) style = style.WithBackground(b);
        style |= TextStyle.Bold;
        return style;
    }
}

