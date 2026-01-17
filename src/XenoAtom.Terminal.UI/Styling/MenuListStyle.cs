// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Text;
using XenoAtom.Terminal.UI.Geometry;

namespace XenoAtom.Terminal.UI.Styling;

/// <summary>
/// Defines rendering and theming options for a menu list (typically shown in a popup).
/// </summary>
public sealed record MenuListStyle : IStyle<MenuListStyle>
{
    /// <summary>
    /// Gets the default menu list style.
    /// </summary>
    public static MenuListStyle Default { get; } = new();

    /// <summary>
    /// Gets the environment key used to resolve a <see cref="MenuListStyle"/>.
    /// </summary>
    public static StyleKey<MenuListStyle> Key { get; } = new("MenuListStyle", Default);

    /// <summary>
    /// Gets the padding around the menu list.
    /// </summary>
    public Thickness Padding { get; init; } = new(Left: 1, Top: 1, Right: 1, Bottom: 1);

    /// <summary>
    /// Gets the number of spaces between an icon and the item label.
    /// </summary>
    public int SpaceBetweenIconAndText { get; init; } = 1;

    /// <summary>
    /// Gets the number of spaces between the label and the shortcut text.
    /// </summary>
    public int SpaceBetweenTextAndShortcut { get; init; } = 2;

    /// <summary>
    /// Gets the glyph used to indicate a submenu item.
    /// </summary>
    public Rune SubmenuGlyph { get; init; } = new('›');

    /// <summary>
    /// Gets the optional style used for normal items.
    /// </summary>
    public CellStyle? ItemStyle { get; init; }
    
    /// <summary>
    /// Gets the optional style used for selected items.
    /// </summary>
    public CellStyle? SelectedStyle { get; init; }
    
    /// <summary>
    /// Gets the optional style used for hovered items.
    /// </summary>
    public CellStyle? HoveredStyle { get; init; }
    
    /// <summary>
    /// Gets the optional style used for disabled items.
    /// </summary>
    public CellStyle? DisabledStyle { get; init; }
    
    /// <summary>
    /// Gets the optional style used for separators.
    /// </summary>
    public CellStyle? SeparatorStyle { get; init; }

    /// <summary>
    /// Resolves an item style for the provided state.
    /// </summary>
    /// <param name="theme">The current theme.</param>
    /// <param name="enabled">Whether the item is enabled.</param>
    /// <param name="selected">Whether the item is selected.</param>
    /// <param name="hovered">Whether the item is hovered.</param>
    public CellStyle ResolveItemStyle(Theme theme, bool enabled, bool selected, bool hovered)
    {
        var baseStyle = theme.ForegroundTextStyle();

        if (!enabled)
        {
            var disabled = baseStyle | TextStyle.Dim;
            if (theme.Disabled is { } c)
            {
                disabled = disabled.WithForeground(c);
            }
            return DisabledStyle ?? disabled;
        }

        if (selected)
        {
            if (SelectedStyle is { } s)
            {
                return s;
            }

            var style = baseStyle | TextStyle.Bold;
            if (theme.SurfaceAlt is { } bg)
            {
                style = style.WithBackground(bg);
            }
            return style;
        }

        if (hovered)
        {
            return HoveredStyle ?? (baseStyle | TextStyle.Bold);
        }

        return ItemStyle ?? baseStyle;
    }

    /// <summary>
    /// Resolves the separator style for the provided <paramref name="theme"/>.
    /// </summary>
    public CellStyle ResolveSeparatorStyle(Theme theme)
    {
        if (SeparatorStyle is { } s)
        {
            return s;
        }

        var style = theme.ForegroundTextStyle() | TextStyle.Dim;
        if (theme.Disabled is { } c)
        {
            style = style.WithForeground(c);
        }
        return style;
    }
}
