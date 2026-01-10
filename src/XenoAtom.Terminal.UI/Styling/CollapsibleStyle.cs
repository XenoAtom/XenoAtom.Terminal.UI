// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Text;

namespace XenoAtom.Terminal.UI.Styling;

public sealed record CollapsibleStyle : IStyle<CollapsibleStyle>
{
    public static CollapsibleStyle Default { get; } = new();

    public static StyleKey<CollapsibleStyle> Key { get; } = new("CollapsibleStyle", Default);

    public int SpaceBetweenGlyphAndHeader { get; init; } = 1;

    public int ContentSpacing { get; init; }

    public Rune ExpandedGlyph { get; init; } = new('▾');

    public Rune CollapsedGlyph { get; init; } = new('▸');

    public CellStyle? Header { get; init; }
    public CellStyle? HeaderHovered { get; init; }
    public CellStyle? HeaderPressed { get; init; }
    public CellStyle? HeaderFocused { get; init; }
    public CellStyle? HeaderDisabled { get; init; }

    public CellStyle ResolveHeader(Theme theme, bool enabled, bool focused, bool hovered, bool pressed)
    {
        ArgumentNullException.ThrowIfNull(theme);

        var normal = Header ?? ResolveDefaultHeader(theme);

        if (!enabled)
        {
            if (HeaderDisabled is { } disabled)
            {
                return disabled | TextStyle.Dim;
            }

            if (theme.Disabled is { } disabledFg)
            {
                normal = normal.WithForeground(disabledFg);
            }

            return normal | TextStyle.Dim;
        }

        if (pressed)
        {
            return HeaderPressed ?? ResolveDefaultPressed(theme, normal);
        }

        var style = normal;
        if (hovered)
        {
            style = HeaderHovered ?? ResolveDefaultHovered(theme, style);
        }

        if (focused)
        {
            style = HeaderFocused ?? ResolveDefaultFocused(theme, style);
        }

        return style;
    }

    private static CellStyle ResolveDefaultHeader(Theme theme)
        => theme.ForegroundTextStyle() | TextStyle.Bold;

    private static CellStyle ResolveDefaultHovered(Theme theme, CellStyle normal)
    {
        if (theme.SurfaceAlt is { } bg)
        {
            return normal.WithBackground(bg);
        }

        return normal;
    }

    private static CellStyle ResolveDefaultPressed(Theme theme, CellStyle normal)
    {
        if (theme.Selection is { } bg)
        {
            return normal.WithBackground(bg) | TextStyle.Bold;
        }

        return normal | TextStyle.Bold;
    }

    private static CellStyle ResolveDefaultFocused(Theme theme, CellStyle normal)
    {
        if (theme.FocusBorder is { } fg)
        {
            normal = normal.WithForeground(fg);
        }

        return normal | TextStyle.Underline;
    }
}
