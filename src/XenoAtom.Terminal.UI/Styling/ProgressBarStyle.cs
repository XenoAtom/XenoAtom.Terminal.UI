// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Text;

namespace XenoAtom.Terminal.UI.Styling;

/// <summary>
/// Defines the rendering and theming of a <see cref="Controls.ProgressBar"/>.
/// </summary>
public sealed record ProgressBarStyle : IStyle<ProgressBarStyle>
{
    /// <summary>
    /// Gets a thin progress bar style (default).
    /// </summary>
    public static ProgressBarStyle Thin { get; } = new()
    {
        Variant = ProgressBarVariant.Thin,
        FillGlyph = new Rune(0x2583), // ▃
        TrackGlyph = new Rune(0x2581), // ▁
        ShowFrame = false,
    };

    /// <summary>
    /// Gets a solid progress bar style.
    /// </summary>
    public static ProgressBarStyle Solid { get; } = new()
    {
        Variant = ProgressBarVariant.Solid,
        FillGlyph = new Rune(0x2588), // █
        TrackGlyph = new Rune(' '),
        ShowFrame = false,
    };

    /// <summary>
    /// Gets a segmented progress bar style.
    /// </summary>
    public static ProgressBarStyle Segmented { get; } = new()
    {
        Variant = ProgressBarVariant.Segmented,
        FillGlyph = new Rune(0x2588), // █
        TrackGlyph = new Rune(' '),
        ShowFrame = false,
    };

    /// <summary>
    /// Gets a shaded progress bar style.
    /// </summary>
    public static ProgressBarStyle Shaded { get; } = new()
    {
        Variant = ProgressBarVariant.Shaded,
        FillGlyph = new Rune(0x2593), // ▓
        TrackGlyph = new Rune(0x2591), // ░
        ShowFrame = false,
    };

    /// <summary>
    /// Gets a bracketed progress bar style (with frame).
    /// </summary>
    public static ProgressBarStyle Bracketed { get; } = new()
    {
        Variant = ProgressBarVariant.Bracketed,
        FillGlyph = new Rune(0x2588), // █
        TrackGlyph = new Rune(0x2591), // ░
        ShowFrame = true,
    };

    /// <summary>
    /// Gets the default progress bar style.
    /// </summary>
    public static ProgressBarStyle Default { get; } = Thin;

    /// <summary>
    /// Gets the environment key used to resolve a <see cref="ProgressBarStyle"/>.
    /// </summary>
    public static StyleKey<ProgressBarStyle> Key { get; } = new("ProgressBarStyle", Default);

    /// <summary>
    /// Gets the progress bar variant.
    /// </summary>
    public ProgressBarVariant Variant { get; init; } = ProgressBarVariant.Thin;

    /// <summary>
    /// Gets a value indicating whether to render a frame around the bar.
    /// </summary>
    public bool ShowFrame { get; init; }

    /// <summary>
    /// Gets the left frame glyph.
    /// </summary>
    public Rune FrameLeftGlyph { get; init; } = new('[');

    /// <summary>
    /// Gets the right frame glyph.
    /// </summary>
    public Rune FrameRightGlyph { get; init; } = new(']');

    /// <summary>
    /// Gets the glyph used for the filled portion.
    /// </summary>
    public Rune FillGlyph { get; init; } = new(0x2588);

    /// <summary>
    /// Gets the glyph used for the unfilled (track) portion.
    /// </summary>
    public Rune TrackGlyph { get; init; } = new(0x2591);

    /// <summary>
    /// Gets the optional cell style for the filled portion.
    /// </summary>
    public Style? Filled { get; init; }

    /// <summary>
    /// Gets the optional cell style for the unfilled (track) portion.
    /// </summary>
    public Style? Unfilled { get; init; }

    /// <summary>
    /// Gets the optional cell style for the frame/border.
    /// </summary>
    public Style? Border { get; init; }

    /// <summary>
    /// Resolves the border style for this progress bar using the provided <paramref name="theme"/>.
    /// </summary>
    public Style ResolveBorder(Theme theme) => Border ?? (theme.BorderStyle(focused: false) | TextStyle.Dim);

    /// <summary>
    /// Resolves the filled style for this progress bar using the provided <paramref name="theme"/>.
    /// </summary>
    public Style ResolveFilled(Theme theme)
    {
        if (Filled is { } filled)
        {
            return filled;
        }

        if (Variant == ProgressBarVariant.Thin || Variant == ProgressBarVariant.Segmented)
        {
            var fg = theme.Primary ?? theme.FocusBorder ?? theme.Foreground;
            return fg is { } c ? (Style.None.WithForeground(c) | TextStyle.Bold) : (Style.None | TextStyle.Bold);
        }

        if (Variant == ProgressBarVariant.Solid || Variant == ProgressBarVariant.Shaded)
        {
            var bg = theme.Primary ?? theme.Selection;
            return bg is { } c ? (Style.None.WithBackground(c) | TextStyle.Bold) : (Style.None | TextStyle.Bold);
        }

        return theme.SelectionStyle();
    }

    /// <summary>
    /// Resolves the unfilled (track) style for this progress bar using the provided <paramref name="theme"/>.
    /// </summary>
    public Style ResolveUnfilled(Theme theme)
    {
        if (Unfilled is { } unfilled)
        {
            return unfilled;
        }

        if (Variant == ProgressBarVariant.Thin || Variant == ProgressBarVariant.Segmented)
        {
            return theme.BorderStyle(focused: false) | TextStyle.Dim;
        }

        if (Variant == ProgressBarVariant.Solid || Variant == ProgressBarVariant.Shaded)
        {
            var bg = theme.Border;
            return bg is { } c ? (Style.None.WithBackground(c) | TextStyle.Dim) : (Style.None | TextStyle.Dim);
        }

        return theme.BorderStyle(focused: false) | TextStyle.Dim;
    }
}
