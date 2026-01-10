// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI.Styling;

public sealed record LinkStyle : IStyle<LinkStyle>
{
    public static LinkStyle Default { get; } = new();

    public static StyleKey<LinkStyle> Key { get; } = new("LinkStyle", Default);

    public CellStyle? Normal { get; init; }
    public CellStyle? Hovered { get; init; }
    public CellStyle? Focused { get; init; }
    public CellStyle? Disabled { get; init; }

    public CellStyle Resolve(Theme theme, bool enabled, bool focused, bool hovered)
    {
        if (!enabled)
        {
            if (Disabled is { } d)
            {
                return d;
            }

            var disabled = theme.ForegroundTextStyle() | TextStyle.Dim;
            if (theme.Disabled is { } c)
            {
                disabled = disabled.WithForeground(c);
            }
            return disabled;
        }

        var baseStyle = theme.ForegroundTextStyle() | TextStyle.Underline;
        if (theme.Accent is { } accent)
        {
            baseStyle = baseStyle.WithForeground(accent);
        }

        if (focused)
        {
            return Focused ?? (baseStyle | TextStyle.Bold);
        }

        if (hovered)
        {
            return Hovered ?? (baseStyle | TextStyle.Bold);
        }

        return Normal ?? baseStyle;
    }
}

