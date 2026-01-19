// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Text;

namespace XenoAtom.Terminal.UI.Styling;

/// <summary>
/// Defines styling for <see cref="Controls.ListBox"/>.
/// </summary>
public sealed record ListBoxStyle : IStyle<ListBoxStyle>
{
    /// <summary>
    /// Gets the default list box style.
    /// </summary>
    public static ListBoxStyle Default { get; } = new();

    /// <summary>
    /// Gets the style key for list boxes.
    /// </summary>
    public static StyleKey<ListBoxStyle> Key { get; } = new("ListBoxStyle", Default);

    /// <summary>
    /// Gets the glyph used to mark the selected item.
    /// </summary>
    public Rune MarkerGlyph { get; init; } = new('→');

    /// <summary>
    /// Gets the normal item style.
    /// </summary>
    public Style? Item { get; init; }

    /// <summary>
    /// Gets the selected item style when focused.
    /// </summary>
    public Style? SelectedFocused { get; init; }

    /// <summary>
    /// Gets the selected item style when unfocused.
    /// </summary>
    public Style? SelectedUnfocused { get; init; }

    /// <summary>
    /// Gets the disabled item style.
    /// </summary>
    public Style? Disabled { get; init; }

    /// <summary>
    /// Resolves the item style for the given state.
    /// </summary>
    /// <param name="theme">The current theme.</param>
    /// <param name="enabled">Whether the control is enabled.</param>
    /// <param name="selected">Whether the item is selected.</param>
    /// <param name="focused">Whether the control is focused.</param>
    /// <returns>The resolved cell style.</returns>
    public Style ResolveItemStyle(Theme theme, bool enabled, bool selected, bool focused)
    {
        var baseStyle = theme.ForegroundTextStyle();

        if (!enabled)
        {
            if (Disabled is { } d)
            {
                return d;
            }

            var disabled = baseStyle | TextStyle.Dim;
            if (theme.Disabled is { } c)
            {
                disabled = disabled.WithForeground(c);
            }
            return disabled;
        }

        if (!selected)
        {
            return Item ?? baseStyle;
        }

        if (focused)
        {
            if (SelectedFocused is { } selectedFocused)
            {
                return selectedFocused;
            }

            var selectedStyle = baseStyle | TextStyle.Bold;
            if (theme.FocusBorder is { } c)
            {
                selectedStyle = selectedStyle.WithForeground(c);
            }
            return selectedStyle;
        }

        return SelectedUnfocused ?? (Style.None | TextStyle.Bold | theme.BorderStyle(focused: false));
    }
}
