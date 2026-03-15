// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI.Styling;

/// <summary>
/// Defines rendering and theming options for a <see cref="Controls.Dialog"/>.
/// </summary>
public sealed record DialogStyle : IStyle<DialogStyle>
{
    /// <summary>
    /// Gets the default dialog style.
    /// </summary>
    public static DialogStyle Default { get; } = new();

    /// <summary>
    /// Gets a predefined dialog style using single-line glyphs.
    /// </summary>
    public static DialogStyle Single { get; } = Default with { Glyphs = LineGlyphs.Single };

    /// <summary>
    /// Gets a predefined dialog style using rounded glyphs.
    /// </summary>
    public static DialogStyle Rounded { get; } = Default with { Glyphs = LineGlyphs.Rounded };

    /// <summary>
    /// Gets a predefined dialog style using double-line glyphs.
    /// </summary>
    public static DialogStyle Double { get; } = Default with { Glyphs = LineGlyphs.Double };

    /// <summary>
    /// Gets a predefined dialog style using heavy glyphs.
    /// </summary>
    public static DialogStyle Heavy { get; } = Default with { Glyphs = LineGlyphs.Heavy };

    /// <summary>
    /// Gets a predefined dialog style using ASCII glyphs.
    /// </summary>
    public static DialogStyle Ascii { get; } = Default with { Glyphs = LineGlyphs.Ascii };

    /// <summary>
    /// Gets a predefined dialog style using dashed glyphs.
    /// </summary>
    public static DialogStyle Dashed { get; } = Default with { Glyphs = LineGlyphs.Dashed };

    /// <summary>
    /// Gets the environment key used to resolve a <see cref="DialogStyle"/>.
    /// </summary>
    public static StyleKey<DialogStyle> Key { get; } = new("DialogStyle", Default);

    /// <summary>
    /// Gets the optional line glyph set used to draw the dialog border.
    /// </summary>
    public LineGlyphs? Glyphs { get; init; }

    /// <summary>
    /// Gets the optional surface style used to clear the dialog background.
    /// </summary>
    public Style? SurfaceStyle { get; init; }

    /// <summary>
    /// Gets the optional border style used when the dialog does not contain focus.
    /// </summary>
    public Style? BorderCellStyle { get; init; }

    /// <summary>
    /// Gets the optional border style used when focus is within the dialog subtree.
    /// </summary>
    public Style? FocusedBorderCellStyle { get; init; }

    /// <summary>
    /// Gets the optional style used to clear the label areas cut into the border.
    /// </summary>
    public Style? LabelBackgroundStyle { get; init; }

    /// <summary>
    /// Gets the optional style used to highlight a resize handle on hover or drag.
    /// </summary>
    public Style? ResizeHandleHoverStyle { get; init; }

    /// <summary>
    /// Resolves the surface style for the given theme.
    /// </summary>
    /// <param name="theme">The current theme.</param>
    public Style ResolveSurfaceStyle(Theme theme)
    {
        if (SurfaceStyle is { } surface)
        {
            return surface.WithTextStyle(surface.TextStyle);
        }

        var style = theme.ForegroundTextStyle();
        style = style.WithTextStyle(style.TextStyle);
        if (theme.PopupSurface is { } popup)
        {
            return style.WithBackground(popup);
        }

        if (theme.SurfaceAlt is { } surfaceAlt)
        {
            return style.WithBackground(surfaceAlt);
        }

        if (theme.Surface is { } surfaceBg)
        {
            return style.WithBackground(surfaceBg);
        }

        return style;
    }

    /// <summary>
    /// Resolves the border style for the given theme and focus state.
    /// </summary>
    /// <param name="theme">The current theme.</param>
    /// <param name="focusedWithin">Whether focus is within the dialog subtree.</param>
    public Style ResolveBorderStyle(Theme theme, bool focusedWithin)
    {
        var style = focusedWithin ? (FocusedBorderCellStyle ?? theme.BorderStyle(focused: true)) : (BorderCellStyle ?? theme.BorderStyle(focused: false));
        style = style.WithTextStyle(style.TextStyle);

        if (!style.TryGetBackground(out _))
        {
            var surface = ResolveSurfaceStyle(theme);
            if (surface.TryGetBackground(out var background))
            {
                style = style.WithBackground(background);
            }
        }

        return style;
    }

    /// <summary>
    /// Resolves the label background style.
    /// </summary>
    public Style ResolveLabelBackgroundStyle()
    {
        var style = LabelBackgroundStyle ?? (Style.None | TextStyle.Bold);
        return style.WithTextStyle(style.TextStyle);
    }

    /// <summary>
    /// Resolves the hover style used for resize handles.
    /// </summary>
    /// <param name="theme">The current theme.</param>
    /// <param name="focusedWithin">Whether focus is within the dialog subtree.</param>
    public Style ResolveResizeHandleHoverStyle(Theme theme, bool focusedWithin)
    {
        if (ResizeHandleHoverStyle is { } hover)
        {
            return hover.WithTextStyle(hover.TextStyle);
        }

        var style = ResolveBorderStyle(theme, focusedWithin);
        if (theme.FocusBorder is { } focusBorder)
        {
            style = style.WithBackground(focusBorder.WithAlpha(0x30));
        }
        else if (theme.SurfaceAlt is { } surfaceAlt)
        {
            style = style.WithBackground(surfaceAlt);
        }
        else if (theme.Surface is { } surface)
        {
            style = style.WithBackground(surface);
        }

        return style;
    }
}
