// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Geometry;

namespace XenoAtom.Terminal.UI.Styling;

public sealed record TabControlStyle : IStyle<TabControlStyle>
{
    public static TabControlStyle Default { get; } = new();

    public static StyleKey<TabControlStyle> Key { get; } = new("TabControlStyle", Default);

    public Thickness TabPadding { get; init; } = new(Left: 2, Top: 0, Right: 2, Bottom: 0);

    public CellStyle? StripStyle { get; init; }
    public CellStyle? TabStyle { get; init; }
    public CellStyle? TabHoveredStyle { get; init; }
    public CellStyle? TabSelectedStyle { get; init; }
    public CellStyle? TabDisabledStyle { get; init; }

    public CellStyle ResolveStripStyle(Theme theme) => StripStyle ?? theme.ForegroundTextStyle();

    public CellStyle ResolveTabStyle(Theme theme, bool enabled, bool selected, bool hovered)
    {
        if (!enabled)
        {
            var disabled = theme.ForegroundTextStyle() | TextStyle.Dim;
            if (theme.Disabled is { } c)
            {
                disabled = disabled.WithForeground(c);
            }
            return TabDisabledStyle ?? disabled;
        }

        if (selected)
        {
            return TabSelectedStyle ?? (theme.BorderStyle(focused: true) | TextStyle.Bold);
        }

        if (hovered)
        {
            return TabHoveredStyle ?? (theme.BorderStyle(focused: true) | TextStyle.Bold);
        }

        return TabStyle ?? theme.ForegroundTextStyle();
    }
}
