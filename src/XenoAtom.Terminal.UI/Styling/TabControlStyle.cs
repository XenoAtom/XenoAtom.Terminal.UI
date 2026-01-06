// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Rendering;

namespace XenoAtom.Terminal.UI.Styling;

public sealed class TabControlStyle
{
    public static TabControlStyle Default { get; } = new();

    public static EnvironmentKey<TabControlStyle> Key { get; } = new("TabControlStyle", Default);

    public Thickness TabPadding { get; init; } = new(Left: 2, Top: 0, Right: 2, Bottom: 0);

    public Cell? StripStyle { get; init; }
    public Cell? TabStyle { get; init; }
    public Cell? TabHoveredStyle { get; init; }
    public Cell? TabSelectedStyle { get; init; }
    public Cell? TabDisabledStyle { get; init; }

    public Cell ResolveStripStyle(Theme theme) => StripStyle ?? theme.SurfaceStyle();

    public Cell ResolveTabStyle(Theme theme, bool enabled, bool selected, bool hovered)
    {
        if (!enabled)
        {
            return TabDisabledStyle ?? (theme.SurfaceStyle() | TextStyle.Dim);
        }

        if (selected)
        {
            if (TabSelectedStyle is { } selectedStyle)
            {
                return selectedStyle;
            }

            var style = theme.SurfaceStyle();
            if (theme.SurfaceAlt is { } bg)
            {
                style = style.WithBackground(bg);
            }
            return style | TextStyle.Bold;
        }

        if (hovered)
        {
            if (TabHoveredStyle is { } hoveredStyle)
            {
                return hoveredStyle;
            }

            var style = theme.SurfaceStyle();
            if (theme.Selection is { } bg)
            {
                style = style.WithBackground(bg);
            }
            return style;
        }

        return TabStyle ?? theme.SurfaceStyle();
    }
}
