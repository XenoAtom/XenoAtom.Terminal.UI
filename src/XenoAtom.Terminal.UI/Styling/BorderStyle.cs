// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI.Styling;

/// <summary>
/// Defines rendering and theming options for a <see cref="Controls.Border"/>.
/// </summary>
public sealed record BorderStyle : IStyle<BorderStyle>
{
    /// <summary>
    /// Gets the default border style.
    /// </summary>
    public static BorderStyle Default { get; } = new();

    /// <summary>
    /// Gets a predefined border style using single-line glyphs.
    /// </summary>
    public static BorderStyle Single { get; } = Default with { Glyphs = LineGlyphs.Single };

    /// <summary>
    /// Gets a predefined border style using rounded corners.
    /// </summary>
    public static BorderStyle Rounded { get; } = Default with { Glyphs = LineGlyphs.Rounded };

    /// <summary>
    /// Gets a predefined border style using double-line glyphs.
    /// </summary>
    public static BorderStyle Double { get; } = Default with { Glyphs = LineGlyphs.Double };

    /// <summary>
    /// Gets a predefined border style using heavy glyphs.
    /// </summary>
    public static BorderStyle Heavy { get; } = Default with { Glyphs = LineGlyphs.Heavy };

    /// <summary>
    /// Gets a predefined border style using ASCII glyphs.
    /// </summary>
    public static BorderStyle Ascii { get; } = Default with { Glyphs = LineGlyphs.Ascii };

    /// <summary>
    /// Gets a predefined border style using ASCII heavy glyphs.
    /// </summary>
    public static BorderStyle AsciiHeavy { get; } = Default with { Glyphs = LineGlyphs.AsciiHeavy };

    /// <summary>
    /// Gets a predefined border style using dashed glyphs.
    /// </summary>
    public static BorderStyle Dashed { get; } = Default with { Glyphs = LineGlyphs.Dashed };

    /// <summary>
    /// Gets the environment key used to resolve a <see cref="BorderStyle"/>.
    /// </summary>
    public static StyleKey<BorderStyle> Key { get; } = new("BorderStyle", Default);

    /// <summary>
    /// Gets the optional line glyph set used to draw the border.
    /// </summary>
    /// <remarks>
    /// When <c>null</c>, the border uses <see cref="Theme.Lines"/>.
    /// </remarks>
    public LineGlyphs? Glyphs { get; init; }

    /// <summary>
    /// Gets a value indicating whether the border should use the focused border style when the focus is within the border subtree.
    /// </summary>
    public bool HighlightOnFocusWithin { get; init; } = true;

    /// <summary>
    /// Gets the optional border style used when not focused.
    /// </summary>
    public Style? BorderCellStyle { get; init; }

    /// <summary>
    /// Gets the optional border style used when focus is within the border subtree.
    /// </summary>
    public Style? FocusedBorderCellStyle { get; init; }

    /// <summary>
    /// Gets the optional background style used to clear the border surface.
    /// </summary>
    /// <remarks>
    /// This style is applied to the full bounds of the border before drawing the glyphs, so it can be used to fill
    /// the interior background if desired. When <c>null</c>, the border clears using <see cref="Style.None"/>.
    /// </remarks>
    public Style? BackgroundStyle { get; init; }

    /// <summary>
    /// Resolves the border cell style for the provided <paramref name="theme"/> and focus state.
    /// </summary>
    /// <param name="theme">The current theme.</param>
    /// <param name="focusedWithin">Whether focus is within the border subtree.</param>
    public Style ResolveBorderStyle(Theme theme, bool focusedWithin)
        => focusedWithin ? (FocusedBorderCellStyle ?? theme.BorderStyle(focused: true)) : (BorderCellStyle ?? theme.BorderStyle(focused: false));

    /// <summary>
    /// Resolves the background style used to clear the border surface.
    /// </summary>
    public Style ResolveBackgroundStyle() => BackgroundStyle ?? Style.None;
}

