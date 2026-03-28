// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Text;
using XenoAtom.Ansi;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.DataGrid;
using XenoAtom.Terminal.UI.Geometry;

namespace XenoAtom.Terminal.UI.Styling;

/// <summary>
/// Defines styling for <see cref="DataGridControl"/>.
/// </summary>
public sealed record DataGridStyle : IStyle<DataGridStyle>
{
    /// <summary>
    /// Gets the default data grid style.
    /// </summary>
    public static DataGridStyle Default { get; } = new();

    /// <summary>
    /// Gets the environment key used to resolve a <see cref="DataGridStyle"/>.
    /// </summary>
    public static StyleKey<DataGridStyle> Key { get; } = new("DataGridStyle", Default);

    /// <summary>
    /// Gets a value indicating whether vertical column separators are rendered.
    /// </summary>
    public bool ShowVerticalLines { get; init; }

    /// <summary>
    /// Gets a value indicating whether a separator is rendered between the header and the body.
    /// </summary>
    public bool ShowHeaderSeparator { get; init; }

    /// <summary>
    /// Gets the spacing between columns when vertical lines are disabled.
    /// </summary>
    public int ColumnSpacing { get; init; } = 1;

    /// <summary>
    /// Gets the padding applied inside each cell.
    /// </summary>
    public Thickness CellPadding { get; init; } = new(0, 0, 0, 0);

    /// <summary>
    /// Gets the optional line glyph set used for separators when enabled.
    /// </summary>
    public LineGlyphs? Glyphs { get; init; }

    /// <summary>
    /// Gets the optional base cell style.
    /// </summary>
    public Style? CellStyle { get; init; }

    /// <summary>
    /// Gets the optional header cell style.
    /// </summary>
    public Style? HeaderStyle { get; init; }

    /// <summary>
    /// Gets the optional selection style when focused.
    /// </summary>
    public Style? SelectedFocused { get; init; }

    /// <summary>
    /// Gets the optional selection style when unfocused.
    /// </summary>
    public Style? SelectedUnfocused { get; init; }

    /// <summary>
    /// Gets the optional match highlight style (search).
    /// </summary>
    public Style? MatchHighlightStyle { get; init; }

    /// <summary>
    /// Gets the glyph used for an unsorted sortable column.
    /// </summary>
    public Rune SortButtonNoneGlyph { get; init; } = new('□');

    /// <summary>
    /// Gets the glyph used for an ascending sortable column.
    /// </summary>
    public Rune SortButtonAscendingGlyph { get; init; } = new('↑');

    /// <summary>
    /// Gets the glyph used for a descending sortable column.
    /// </summary>
    public Rune SortButtonDescendingGlyph { get; init; } = new('↓');

    /// <summary>
    /// Gets the optional sort button style in its normal state.
    /// </summary>
    public Style? SortButtonNormal { get; init; }

    /// <summary>
    /// Gets the optional sort button style in its hovered state.
    /// </summary>
    public Style? SortButtonHovered { get; init; }

    /// <summary>
    /// Gets the optional sort button style in its pressed state.
    /// </summary>
    public Style? SortButtonPressed { get; init; }

    /// <summary>
    /// Resolves the base cell style.
    /// </summary>
    public Style ResolveCellStyle(Theme theme) => CellStyle ?? theme.InputFillStyle(focused: false);

    /// <summary>
    /// Resolves the header style.
    /// </summary>
    public Style ResolveHeaderStyle(Theme theme)
    {
        if (HeaderStyle is { } header)
        {
            return header;
        }

        // Default header: match the tab header background (surface) and emphasize text.
        return theme.SurfaceStyle() | TextStyle.Bold;
    }

    /// <summary>
    /// Resolves the selection style.
    /// </summary>
    public Style ResolveSelectionStyle(Theme theme, bool focused)
    {
        if (focused)
        {
            if (SelectedFocused is { } focusedStyle)
            {
                return focusedStyle;
            }

            return StrengthenSelection(theme.SelectionStyle(), theme.Selection, minAlpha: 0x70);
        }

        if (SelectedUnfocused is { } unfocusedStyle)
        {
            return unfocusedStyle;
        }

        // Keep the selection visible even when focus moves away.
        return StrengthenSelection(theme.SelectionStyle() | TextStyle.Dim, theme.Selection, minAlpha: 0x40);
    }

    /// <summary>
    /// Resolves the match highlight style.
    /// </summary>
    public Style ResolveMatchHighlightStyle(Theme theme)
    {
        if (MatchHighlightStyle is { } s)
        {
            return s;
        }

        // Default: use Accent as a subtle background highlight.
        var style = Style.None;
        if (theme.Accent is { } c)
        {
            style = style.WithBackground(c.WithAlpha(0x30));
        }
        return style;
    }

    /// <summary>
    /// Resolves the sort button style.
    /// </summary>
    public Style ResolveSortButtonStyle(Theme theme, Style headerStyle, bool hovered, bool pressed)
    {
        ArgumentNullException.ThrowIfNull(theme);

        var normal = SortButtonNormal ?? headerStyle;
        if (pressed)
        {
            if (SortButtonPressed is { } pressedStyle)
            {
                return pressedStyle;
            }

            if ((theme.ControlFillPressed ?? theme.Selection) is { } pressedBackground)
            {
                normal = normal.WithBackground(pressedBackground);
            }

            return normal | TextStyle.Bold;
        }

        if (hovered)
        {
            if (SortButtonHovered is { } hoveredStyle)
            {
                return hoveredStyle;
            }

            if ((theme.ControlFillHover ?? theme.SurfaceAlt) is { } hoverBackground)
            {
                normal = normal.WithBackground(hoverBackground);
            }

            return normal | TextStyle.Bold;
        }

        return normal;
    }

    /// <summary>
    /// Resolves the sort button glyph for the provided direction.
    /// </summary>
    public Rune ResolveSortButtonGlyph(DataGridSortDirection? direction)
        => direction switch
        {
            DataGridSortDirection.Ascending => SortButtonAscendingGlyph,
            DataGridSortDirection.Descending => SortButtonDescendingGlyph,
            _ => SortButtonNoneGlyph,
        };

    private static Style StrengthenSelection(Style style, Color? color, byte minAlpha)
    {
        if (color is not { } c)
        {
            return style;
        }

        // For RGB(A) themes, bump alpha so the selection is clearly visible.
        // For non-RGB terminals this is typically a no-op.
        return style.WithBackground(c.WithAlpha(minAlpha));
    }
}
