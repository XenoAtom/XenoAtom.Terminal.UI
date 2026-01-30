// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Geometry;

namespace XenoAtom.Terminal.UI.Styling;

/// <summary>
/// Defines styling for <see cref="Controls.DataGrid"/>.
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
    /// Resolves the base cell style.
    /// </summary>
    public Style ResolveCellStyle(Theme theme) => CellStyle ?? theme.ForegroundTextStyle();

    /// <summary>
    /// Resolves the header style.
    /// </summary>
    public Style ResolveHeaderStyle(Theme theme)
    {
        var style = HeaderStyle ?? (ResolveCellStyle(theme) | TextStyle.Bold);
        if (theme.Border is { } bg && HeaderStyle is null)
        {
            style = style.WithBackground(bg);
        }
        return style;
    }

    /// <summary>
    /// Resolves the selection style.
    /// </summary>
    public Style ResolveSelectionStyle(Theme theme, bool focused)
    {
        if (focused)
        {
            return SelectedFocused ?? theme.SelectionStyle();
        }

        return SelectedUnfocused ?? (Style.None | TextStyle.Bold | theme.BorderStyle(focused: false));
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

        // Default: use Accent as background when available.
        var style = Style.None;
        if (theme.Accent is { } c)
        {
            style = style.WithBackground(c);
        }
        return style;
    }
}

