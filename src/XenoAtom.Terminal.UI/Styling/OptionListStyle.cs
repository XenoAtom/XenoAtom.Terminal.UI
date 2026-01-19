// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Text;
using XenoAtom.Ansi;
using XenoAtom.Terminal.UI;

namespace XenoAtom.Terminal.UI.Styling;

/// <summary>
/// Defines rendering and theming options for an <see cref="Controls.OptionList"/>.
/// </summary>
public sealed record OptionListStyle : IStyle<OptionListStyle>
{
    /// <summary>
    /// Gets the default option list style.
    /// </summary>
    public static OptionListStyle Default { get; } = new();

    /// <summary>
    /// Gets the environment key used to resolve an <see cref="OptionListStyle"/>.
    /// </summary>
    public static StyleKey<OptionListStyle> Key { get; } = new("OptionListStyle", Default);

    /// <summary>
    /// Gets the number of spaces between the marker glyph and the item content.
    /// </summary>
    public int SpaceBetweenGlyphAndText { get; init; } = 1;

    /// <summary>
    /// Gets the number of spaces between the content and the shortcut label.
    /// </summary>
    public int SpaceBetweenContentAndShortcut { get; init; } = 2;

    /// <summary>
    /// Gets the indentation (in columns) used for the description lines.
    /// </summary>
    public int DescriptionIndent { get; init; } = 2;

    /// <summary>
    /// Gets the glyph used to mark the selected item.
    /// </summary>
    public Rune MarkerGlyph { get; init; } = new('→');

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
    /// Gets the optional style for a hovered item.
    /// </summary>
    public Style? Hovered { get; init; }

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
    /// <param name="hovered">Whether the item is hovered.</param>
    public Style ResolveItemStyle(Theme theme, bool enabled, bool selected, bool focused, bool hovered)
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
            var normal = Item ?? baseStyle;
            if (hovered)
            {
                return Hovered ?? ResolveDefaultHovered(theme, normal);
            }
            return normal;
        }

        if (focused)
        {
            if (SelectedFocused is { } selectedFocused)
            {
                return selectedFocused;
            }

            var selectedStyle = Style.None.WithForeground(theme.Accent ?? Colors.TerminalBlue) | TextStyle.Bold;
            if (theme.FocusBorder is { } c)
            {
                selectedStyle = selectedStyle.WithForeground(c);
            }
            return selectedStyle;
        }

        var style = theme.ForegroundTextStyle();
        if (theme.Accent is { } accent)
        {
            style = style.WithForeground(accent);
        }

        return SelectedUnfocused ?? (style | TextStyle.Bold);
        //return SelectedUnfocused ?? (CellStyle.None | TextStyle.Bold | theme.BorderStyle(focused: false));
    }

    private static Style ResolveDefaultHovered(Theme theme, Style normal)
    {
        if ((theme.ControlFillHover ?? theme.SurfaceAlt) is { } bg)
        {
            return normal.WithBackground(bg);
        }

        return normal | TextStyle.Bold;
    }
}
