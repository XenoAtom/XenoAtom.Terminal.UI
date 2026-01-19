// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Geometry;

namespace XenoAtom.Terminal.UI.Styling;

/// <summary>
/// Defines rendering and theming options for a <see cref="Controls.Table"/>.
/// </summary>
public sealed record TableStyle : IStyle<TableStyle>
{
    /// <summary>
    /// Gets the default table style.
    /// </summary>
    public static TableStyle Default { get; } = new();

    /// <summary>
    /// Gets the environment key used to resolve a <see cref="TableStyle"/>.
    /// </summary>
    public static StyleKey<TableStyle> Key { get; } = new("TableStyle", Default);

    /// <summary>
    /// Gets a minimal table style without borders or separators.
    /// </summary>
    public static TableStyle Minimal { get; } = Default with
    {
        ShowOuterBorder = false,
        ShowVerticalLines = false,
        ShowRowSeparators = false,
    };

    /// <summary>
    /// Gets a grid table style with outer border and separators.
    /// </summary>
    public static TableStyle Grid { get; } = Default with
    {
        ShowOuterBorder = true,
        ShowVerticalLines = true,
        ShowRowSeparators = true,
    };

    /// <summary>
    /// Gets a rounded-corners grid table style.
    /// </summary>
    public static TableStyle RoundedGrid { get; } = Grid with
    {
        Glyphs = LineGlyphs.Rounded,
    };

    /// <summary>
    /// Gets a double-line grid table style.
    /// </summary>
    public static TableStyle DoubleGrid { get; } = Grid with
    {
        Glyphs = LineGlyphs.Double,
    };

    /// <summary>
    /// Gets a value indicating whether to render an outer border.
    /// </summary>
    public bool ShowOuterBorder { get; init; } = true;

    /// <summary>
    /// Gets a value indicating whether to render vertical column separators.
    /// </summary>
    public bool ShowVerticalLines { get; init; } = true;

    /// <summary>
    /// Gets a value indicating whether to render row separators between each row.
    /// </summary>
    public bool ShowRowSeparators { get; init; }

    /// <summary>
    /// Gets a value indicating whether to render a separator between the header and the body.
    /// </summary>
    public bool ShowHeaderSeparator { get; init; } = true;

    /// <summary>
    /// Gets the padding applied inside each cell.
    /// </summary>
    public Thickness CellPadding { get; init; } = new(1, 0, 1, 0);

    /// <summary>
    /// Gets the optional line glyph set used for borders and separators.
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
    /// Gets the optional border/separator style.
    /// </summary>
    public Style? BorderStyle { get; init; }

    /// <summary>
    /// Resolves the base cell style for the provided <paramref name="theme"/>.
    /// </summary>
    public Style ResolveCellStyle(Theme theme)
        => CellStyle ?? theme.BaseTextStyle();

    /// <summary>
    /// Resolves the header cell style for the provided <paramref name="theme"/>.
    /// </summary>
    public Style ResolveHeaderStyle(Theme theme)
        => HeaderStyle ?? (ResolveCellStyle(theme) | TextStyle.Bold);

    /// <summary>
    /// Resolves the border/separator style for the provided <paramref name="theme"/>.
    /// </summary>
    /// <param name="theme">The current theme.</param>
    /// <param name="focused">Whether the table is focused.</param>
    public Style ResolveBorderStyle(Theme theme, bool focused)
        => BorderStyle ?? theme.BorderStyle(focused);
}
