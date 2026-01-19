// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Geometry;

namespace XenoAtom.Terminal.UI.Styling;

/// <summary>
/// Defines rendering and theming options for a <see cref="Controls.MenuBar"/>.
/// </summary>
public sealed record MenuBarStyle : IStyle<MenuBarStyle>
{
    /// <summary>
    /// Gets the default menu bar style.
    /// </summary>
    public static MenuBarStyle Default { get; } = new();

    /// <summary>
    /// Gets the environment key used to resolve a <see cref="MenuBarStyle"/>.
    /// </summary>
    public static StyleKey<MenuBarStyle> Key { get; } = new("MenuBarStyle", Default);

    /// <summary>
    /// Gets the padding applied around the bar.
    /// </summary>
    public Thickness Padding { get; init; } = new(Left: 1, Top: 0, Right: 1, Bottom: 0);

    /// <summary>
    /// Gets the padding applied around each top-level item.
    /// </summary>
    public Thickness ItemPadding { get; init; } = new(Left: 2, Top: 0, Right: 2, Bottom: 0);

    /// <summary>
    /// Gets the number of spaces between items.
    /// </summary>
    public int ItemSpacing { get; init; } = 0;

    /// <summary>
    /// Gets the optional style used for the bar background.
    /// </summary>
    public Style? BarStyle { get; init; }
    
    /// <summary>
    /// Gets the optional style used for normal items.
    /// </summary>
    public Style? ItemStyle { get; init; }
    
    /// <summary>
    /// Gets the optional style used for hovered items.
    /// </summary>
    public Style? ItemHoverStyle { get; init; }
    
    /// <summary>
    /// Gets the optional style used for open items.
    /// </summary>
    public Style? ItemOpenStyle { get; init; }
    
    /// <summary>
    /// Gets the optional style used for selected items.
    /// </summary>
    public Style? ItemSelectedStyle { get; init; }
    
    /// <summary>
    /// Gets the optional style used for disabled items.
    /// </summary>
    public Style? ItemDisabledStyle { get; init; }

    /// <summary>
    /// Resolves the bar style for the provided <paramref name="theme"/>.
    /// </summary>
    public Style ResolveBarStyle(Theme theme)
    {
        if (BarStyle is { } s)
        {
            return s;
        }

        var style = theme.ForegroundTextStyle();
        if (theme.SurfaceAlt is { } bg)
        {
            style = style.WithBackground(bg);
        }
        else if (theme.Surface is { } bg2)
        {
            style = style.WithBackground(bg2);
        }

        return style;
    }

    /// <summary>
    /// Resolves the item style for the provided state.
    /// </summary>
    /// <param name="theme">The current theme.</param>
    /// <param name="enabled">Whether the item is enabled.</param>
    /// <param name="open">Whether the item is open.</param>
    /// <param name="selected">Whether the item is selected.</param>
    /// <param name="hovered">Whether the item is hovered.</param>
    public Style ResolveItemStyle(Theme theme, bool enabled, bool open, bool selected, bool hovered)
    {
        if (!enabled)
        {
            var disabled = theme.ForegroundTextStyle() | TextStyle.Dim;
            if (theme.Disabled is { } c)
            {
                disabled = disabled.WithForeground(c);
            }
            return ItemDisabledStyle ?? disabled;
        }

        if (open)
        {
            return ItemOpenStyle ?? (theme.BorderStyle(focused: true) | TextStyle.Bold);
        }

        if (selected)
        {
            return ItemSelectedStyle ?? (theme.BorderStyle(focused: true) | TextStyle.Bold);
        }

        if (hovered)
        {
            return ItemHoverStyle ?? (theme.BorderStyle(focused: true) | TextStyle.Bold);
        }

        return ItemStyle ?? theme.ForegroundTextStyle();
    }
}
