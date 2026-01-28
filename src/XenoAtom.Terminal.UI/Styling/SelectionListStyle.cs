// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Text;

namespace XenoAtom.Terminal.UI.Styling;

/// <summary>
/// Defines rendering and theming options for a <see cref="Controls.SelectionList{T}"/>.
/// </summary>
public sealed record SelectionListStyle : IStyle<SelectionListStyle>
{
    /// <summary>
    /// Gets the default selection list style.
    /// </summary>
    public static SelectionListStyle Default { get; } = new();

    /// <summary>
    /// Gets the environment key used to resolve a <see cref="SelectionListStyle"/>.
    /// </summary>
    public static StyleKey<SelectionListStyle> Key { get; } = new("SelectionListStyle", Default);

    /// <summary>
    /// Gets the number of spaces between the checkbox glyph and the item content.
    /// </summary>
    public int SpaceBetweenGlyphAndText { get; init; } = 2;

    /// <summary>
    /// Gets the glyph used to indicate the currently focused row.
    /// </summary>
    public Rune FocusMarkerGlyph { get; init; } = new('→');

    /// <summary>
    /// Gets the glyph used for checked items.
    /// </summary>
    public Rune CheckedGlyph { get; init; } = new(0x2611); // ☑

    /// <summary>
    /// Gets the glyph used for unchecked items.
    /// </summary>
    public Rune UncheckedGlyph { get; init; } = new(0x2610); // ☐

    /// <summary>
    /// Gets the optional style for a normal item.
    /// </summary>
    public Style? Item { get; init; }

    /// <summary>
    /// Gets the optional style for a selected item when focused.
    /// </summary>
    public Style? SelectedFocused { get; init; }

    /// <summary>
    /// Gets the optional style for a selected item when unfocused.
    /// </summary>
    public Style? SelectedUnfocused { get; init; }

    /// <summary>
    /// Gets the optional style for disabled items.
    /// </summary>
    public Style? Disabled { get; init; }

    /// <summary>
    /// Resolves the style for an item given its state.
    /// </summary>
    /// <param name="theme">The current theme.</param>
    /// <param name="enabled">Whether the item is enabled.</param>
    /// <param name="selected">Whether the item is selected.</param>
    /// <param name="focused">Whether the list is focused.</param>
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
