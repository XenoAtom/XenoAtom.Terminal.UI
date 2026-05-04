// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Text;
using XenoAtom.Terminal.UI.Controls;
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
    public static MenuListStyle Default { get; } = new()
    {
        PopupTemplateFactory = visual => new Group { Content = visual },
    };

    /// <summary>
    /// Gets a predefined style that does not apply a popup template (no border wrapper).
    /// </summary>
    public static MenuListStyle NoBorder { get; } = new();

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
    public Style? ItemStyle { get; init; }

    /// <summary>
    /// Gets the optional style used for selected items.
    /// </summary>
    public Style? SelectedStyle { get; init; }

    /// <summary>
    /// Gets the optional style used for hovered items.
    /// </summary>
    public Style? HoveredStyle { get; init; }

    /// <summary>
    /// Gets the optional style used for disabled items.
    /// </summary>
    public Style? DisabledStyle { get; init; }

    /// <summary>
    /// Gets the optional style used for separators.
    /// </summary>
    public Style? SeparatorStyle { get; init; }

    /// <summary>
    /// Gets the factory function used to create an optional template wrapper for the menu list when it is shown in a popup.
    /// </summary>
    /// <remarks>
    /// The returned visual is typically used to draw a border or other chrome around the menu list.
    /// </remarks>
    public Func<Visual, Visual?>? PopupTemplateFactory { get; init; }

    /// <summary>
    /// Resolves an item style for the provided state.
    /// </summary>
    /// <param name="theme">The current theme.</param>
    /// <param name="enabled">Whether the item is enabled.</param>
    /// <param name="selected">Whether the item is selected.</param>
    /// <param name="hovered">Whether the item is hovered.</param>
    public Style ResolveItemStyle(Theme theme, bool enabled, bool selected, bool hovered)
    {
        ArgumentNullException.ThrowIfNull(theme);

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
            return SelectedStyle ?? ResolveDefaultSelected(theme, baseStyle);
        }

        if (hovered)
        {
            return HoveredStyle ?? (baseStyle | TextStyle.Bold);
        }

        return ItemStyle ?? baseStyle;
    }

    private static Style ResolveDefaultSelected(Theme theme, Style baseStyle)
    {
        var style = baseStyle | TextStyle.Bold;
        if ((theme.FocusBorder ?? theme.Accent ?? theme.Primary) is { } c)
        {
            style = style.WithForeground(c);
        }

        if (theme.SurfaceAlt is { } bg)
        {
            style = style.WithBackground(bg);
        }

        return style;
    }

    /// <summary>
    /// Resolves the separator style for the provided <paramref name="theme"/>.
    /// </summary>
    public Style ResolveSeparatorStyle(Theme theme)
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
