// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Rendering;

namespace XenoAtom.Terminal.UI.Styling;

public sealed class StatusBarStyle
{
    public static StatusBarStyle Default { get; } = new();

    public static EnvironmentKey<StatusBarStyle> Key { get; } = new("StatusBarStyle", Default);

    public XenoAtom.Ansi.AnsiColor? Background { get; init; }
    public XenoAtom.Ansi.AnsiColor? Foreground { get; init; }

    public Cell Resolve(Theme theme)
    {
        var style = Cell.None;
        var fg = Foreground ?? theme.Foreground;
        var bg = Background ?? theme.SurfaceAlt ?? theme.Surface ?? theme.Background;

        if (fg is { } f) style = style.WithForeground(f);
        if (bg is { } b) style = style.WithBackground(b);
        style |= TextStyle.Bold;
        return style;
    }
}
