// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Text;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Geometry;

namespace XenoAtom.Terminal.UI.Styling;

/// <summary>
/// Defines rendering and theming options for a <see cref="Controls.Select{T}"/>.
/// </summary>
public sealed record SelectStyle : IStyle<SelectStyle>
{
    /// <summary>
    /// Gets the default select style.
    /// </summary>
    public static SelectStyle Default { get; } = new()
    {
        PopupTemplateFactory = visual => new Border(visual).Stretch(),
    };

    /// <summary>
    /// Gets a predefined style that displays the selection without a border.
    /// </summary>
    public static SelectStyle NoBorder { get; } = new();

    /// <summary>
    /// Gets the environment key used to resolve a <see cref="SelectStyle"/>.
    /// </summary>
    public static StyleKey<SelectStyle> Key { get; } = new("SelectStyle", Default);

    /// <summary>
    /// Gets the padding applied around the select content.
    /// </summary>
    public Thickness Padding { get; init; } = new(Left: 1, Top: 0, Right: 2, Bottom: 0);

    /// <summary>
    /// Gets the glyph used to render the dropdown arrow.
    /// </summary>
    public Rune ArrowGlyph { get; init; } = new('▾');

    /// <summary>
    /// Gets the optional style used for the normal state.
    /// </summary>
    public Style? NormalStyle { get; init; }

    /// <summary>
    /// Gets the optional style used for the hovered state.
    /// </summary>
    public Style? HoverStyle { get; init; }

    /// <summary>
    /// Gets the optional style used for the focused state.
    /// </summary>
    public Style? FocusedStyle { get; init; }

    /// <summary>
    /// Gets the optional style used for the disabled state.
    /// </summary>
    public Style? DisabledStyle { get; init; }

    /// <summary>
    /// Gets the factory function used to create a border visual for a given popup visual element.
    /// </summary>
    public Func<Visual, Visual?>? PopupTemplateFactory { get; init; }

    /// <summary>
    /// Resolves the select style for the provided state.
    /// </summary>
    /// <param name="theme">The current theme.</param>
    /// <param name="enabled">Whether the control is enabled.</param>
    /// <param name="focused">Whether the control is focused.</param>
    /// <param name="hovered">Whether the control is hovered.</param>
    public Style ResolveStyle(Theme theme, bool enabled, bool focused, bool hovered)
    {
        ArgumentNullException.ThrowIfNull(theme);

        var normal = NormalStyle ?? theme.ForegroundTextStyle();

        if (!enabled && DisabledStyle is { } disabledStyle)
        {
            return disabledStyle;
        }

        if (!enabled)
        {
            var resolvedDisabled = normal | TextStyle.Dim;
            if (theme.Disabled is { } c)
            {
                resolvedDisabled = resolvedDisabled.WithForeground(c);
            }
            return resolvedDisabled;
        }

        if (focused && FocusedStyle is { } focus)
        {
            return focus;
        }

        if (focused)
        {
            return ResolveDefaultFocused(theme, normal);
        }

        if (hovered && HoverStyle is { } hover)
        {
            return hover;
        }

        return normal;
    }

    private static Style ResolveDefaultFocused(Theme theme, Style normal)
    {
        var focused = normal | TextStyle.Bold;
        if ((theme.FocusBorder ?? theme.Accent ?? theme.Primary) is { } c)
        {
            focused = focused.WithForeground(c);
        }

        return focused;
    }
}
