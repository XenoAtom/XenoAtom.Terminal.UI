// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI.Styling;

/// <summary>
/// Defines rendering and theming options for a <see cref="Controls.Group"/>.
/// </summary>
public sealed record GroupStyle : IStyle<GroupStyle>
{
    /// <summary>
    /// Gets the default group style.
    /// </summary>
    public static GroupStyle Default { get; } = new();

    /// <summary>
    /// Gets a predefined group style using single-line glyphs.
    /// </summary>
    public static GroupStyle Single { get; } = Default with { Glyphs = LineGlyphs.Single };

    /// <summary>
    /// Gets a predefined group style using rounded corners.
    /// </summary>
    public static GroupStyle Rounded { get; } = Default with { Glyphs = LineGlyphs.Rounded };

    /// <summary>
    /// Gets a predefined group style using double-line glyphs.
    /// </summary>
    public static GroupStyle Double { get; } = Default with { Glyphs = LineGlyphs.Double };

    /// <summary>
    /// Gets a predefined group style using heavy glyphs.
    /// </summary>
    public static GroupStyle Heavy { get; } = Default with { Glyphs = LineGlyphs.Heavy };

    /// <summary>
    /// Gets a predefined group style using ASCII glyphs.
    /// </summary>
    public static GroupStyle Ascii { get; } = Default with { Glyphs = LineGlyphs.Ascii };

    /// <summary>
    /// Gets a predefined group style using dashed glyphs.
    /// </summary>
    public static GroupStyle Dashed { get; } = Default with { Glyphs = LineGlyphs.Dashed };

    /// <summary>
    /// Gets the environment key used to resolve a <see cref="GroupStyle"/>.
    /// </summary>
    public static StyleKey<GroupStyle> Key { get; } = new("GroupStyle", Default);

    /// <summary>
    /// Gets the optional line glyph set used to draw the group border.
    /// </summary>
    /// <remarks>
    /// When <c>null</c>, the group uses <see cref="Theme.Lines"/>.
    /// </remarks>
    public LineGlyphs? Glyphs { get; init; }

    /// <summary>
    /// Gets the optional border style used when the group does not contain focus.
    /// </summary>
    public CellStyle? BorderCellStyle { get; init; }

    /// <summary>
    /// Gets the optional border style used when focus is within the group subtree.
    /// </summary>
    public CellStyle? FocusedBorderCellStyle { get; init; }

    /// <summary>
    /// Gets the optional style used to clear the label area (so labels can "cut" the border).
    /// </summary>
    /// <remarks>
    /// The style is applied to spaces written behind label visuals, not to the label content itself.
    /// </remarks>
    public CellStyle? LabelBackgroundStyle { get; init; }

    /// <summary>
    /// Gets the optional background style used to clear the group surface.
    /// </summary>
    /// <remarks>
    /// When set, the group fills its bounds with spaces using this style before drawing border glyphs.
    /// </remarks>
    public CellStyle? BackgroundStyle { get; init; }

    /// <summary>
    /// Resolves the border cell style for the provided <paramref name="theme"/> and focus state.
    /// </summary>
    /// <param name="theme">The current theme.</param>
    /// <param name="focusedWithin">Whether focus is within the group subtree.</param>
    public CellStyle ResolveBorderStyle(Theme theme, bool focusedWithin)
        => focusedWithin ? (FocusedBorderCellStyle ?? theme.BorderStyle(focused: true)) : (BorderCellStyle ?? theme.BorderStyle(focused: false));

    /// <summary>
    /// Resolves the label background style.
    /// </summary>
    public CellStyle ResolveLabelBackgroundStyle()
        => LabelBackgroundStyle ?? (CellStyle.None | TextStyle.Bold);

    /// <summary>
    /// Resolves the background style used to clear the group surface.
    /// </summary>
    public CellStyle? ResolveBackgroundStyle()
        => BackgroundStyle;
}

