// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Text;

namespace XenoAtom.Terminal.UI.Styling;

public sealed record ProgressBarStyle
{
    public static ProgressBarStyle Thin { get; } = new()
    {
        Variant = ProgressBarVariant.Thin,
        FillGlyph = new Rune(0x2583), // ▃
        TrackGlyph = new Rune(0x2581), // ▁
        ShowFrame = false,
        ShowPercentage = true,
    };

    public static ProgressBarStyle Solid { get; } = new()
    {
        Variant = ProgressBarVariant.Solid,
        FillGlyph = new Rune(0x2588), // █
        TrackGlyph = new Rune(' '),
        ShowFrame = false,
        ShowPercentage = true,
    };

    public static ProgressBarStyle Segmented { get; } = new()
    {
        Variant = ProgressBarVariant.Segmented,
        FillGlyph = new Rune(0x2588), // █
        TrackGlyph = new Rune(' '),
        ShowFrame = false,
        ShowPercentage = true,
    };

    public static ProgressBarStyle Bracketed { get; } = new()
    {
        Variant = ProgressBarVariant.Bracketed,
        FillGlyph = new Rune(0x2588), // █
        TrackGlyph = new Rune(0x2591), // ░
        ShowFrame = true,
        ShowPercentage = true,
    };

    public static ProgressBarStyle Default { get; } = Thin;

    public static EnvironmentKey<ProgressBarStyle> Key { get; } = new("ProgressBarStyle", Default);

    public ProgressBarVariant Variant { get; init; } = ProgressBarVariant.Thin;

    public bool ShowPercentage { get; init; } = true;

    public bool ShowFrame { get; init; }

    public Rune FrameLeftGlyph { get; init; } = new('[');

    public Rune FrameRightGlyph { get; init; } = new(']');

    public Rune FillGlyph { get; init; } = new(0x2588);
    public Rune TrackGlyph { get; init; } = new(0x2591);

    public CellStyle? Filled { get; init; }
    public CellStyle? Unfilled { get; init; }
    public CellStyle? Border { get; init; }

    public CellStyle ResolveBorder(Theme theme) => Border ?? (theme.BorderStyle(focused: false) | TextStyle.Dim);

    public CellStyle ResolveFilled(Theme theme)
    {
        if (Filled is { } filled)
        {
            return filled;
        }

        if (Variant == ProgressBarVariant.Thin || Variant == ProgressBarVariant.Segmented)
        {
            var fg = theme.Primary ?? theme.FocusBorder ?? theme.Foreground;
            return fg is { } c ? (CellStyle.None.WithForeground(c) | TextStyle.Bold) : (CellStyle.None | TextStyle.Bold);
        }

        if (Variant == ProgressBarVariant.Solid || Variant == ProgressBarVariant.Shaded)
        {
            var bg = theme.Primary ?? theme.Selection;
            return bg is { } c ? (CellStyle.None.WithBackground(c) | TextStyle.Bold) : (CellStyle.None | TextStyle.Bold);
        }

        return theme.SelectionStyle();
    }

    public CellStyle ResolveUnfilled(Theme theme)
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
            return bg is { } c ? (CellStyle.None.WithBackground(c) | TextStyle.Dim) : (CellStyle.None | TextStyle.Dim);
        }

        return theme.BorderStyle(focused: false) | TextStyle.Dim;
    }
}
