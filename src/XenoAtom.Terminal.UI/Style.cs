// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Ansi;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI;

/// <summary>
/// Represents a packed per-cell style (foreground/background + decorations).
/// </summary>
/// <remarks>
/// This type is a lightweight, value-type container optimized for cell buffers. The foreground and background colors
/// are stored as packed 64-bit <see cref="Color"/> values to make equality comparisons very fast.
/// </remarks>
public readonly struct Style : IEquatable<Style>
{
    // This struct is intentionally 16 bytes:
    // - 8 bytes foreground color
    // - 8 bytes background color + flags
    //
    // Background's reserved bits [16..31] are used for style flags:
    // - bits 16..23: TextStyle
    // - bit  24    : Continuation (wide glyph trailing cell)
    // - bits 25..31: reserved

    private const int TextStyleShift = 16;
    private const ulong TextStyleMask = 0xFFul << TextStyleShift;

    private const int ContinuationBit = 24;
    private const ulong ContinuationMask = 1ul << ContinuationBit;

    private const int TextStyleSpecifiedBit = 25;
    private const ulong TextStyleSpecifiedMask = 1ul << TextStyleSpecifiedBit;

    // All bits except the background color payload (kind/index + RGBA bytes).
    private const ulong BackgroundFlagsMask = 0x00000000FFFF0000ul;

    // Background color is stored in bits 0..15 and 32..63 (16..31 reserved for flags).
    private const ulong BackgroundColorMask = 0xFFFFFFFF0000FFFFul;

    private readonly ulong _foreground;
    private readonly ulong _backgroundAndFlags;

    private Style(ulong foreground, ulong backgroundAndFlags)
    {
        _foreground = foreground;
        _backgroundAndFlags = backgroundAndFlags;
    }

    /// <summary>
    /// Gets a style with default foreground/background and no decorations.
    /// </summary>
    public static Style None => default;

    /// <summary>
    /// Gets the text style flags (decorations) for this cell.
    /// </summary>
    public TextStyle TextStyle => (TextStyle)((_backgroundAndFlags & TextStyleMask) >> TextStyleShift);

    private bool HasTextStyleSpecified => ((_backgroundAndFlags & TextStyleSpecifiedMask) != 0) || TextStyle != 0;

    internal bool IsContinuation => (_backgroundAndFlags & ContinuationMask) != 0;

    internal Style WithoutContinuation()
        => new(_foreground, _backgroundAndFlags & ~ContinuationMask);

    internal Style WithContinuation()
        => new(_foreground, _backgroundAndFlags | ContinuationMask);

    private bool HasForeground => (byte)_foreground != 0;

    private bool HasBackground => (byte)(_backgroundAndFlags & 0xFF) != 0;

    private ulong BackgroundColorRaw => _backgroundAndFlags & BackgroundColorMask;

    private ulong BackgroundFlagsRaw => _backgroundAndFlags & BackgroundFlagsMask;

    /// <summary>
    /// Merges any unspecified components of this style from <paramref name="under"/>.
    /// </summary>
    /// <remarks>
    /// This is used when rendering a cell buffer in layers: callers often write text with a foreground style without
    /// specifying a background, expecting the previously filled background to remain.
    /// </remarks>
    internal Style MergeUnspecified(Style under)
    {
        var fg = _foreground;
        if (!HasForeground)
        {
            fg = under._foreground;
        }

        var bg = BackgroundColorRaw;
        if (!HasBackground)
        {
            bg = under.BackgroundColorRaw;
        }

        var textStyle = (byte)TextStyle;
        if (!HasTextStyleSpecified)
        {
            textStyle = (byte)under.TextStyle;
        }

        var flags = ((ulong)textStyle << TextStyleShift) | (BackgroundFlagsRaw & (ContinuationMask | TextStyleSpecifiedMask));
        return new Style(fg, bg | flags);
    }

    /// <summary>
    /// Returns a copy with the specified text style flags (replacing any existing flags).
    /// </summary>
    public Style WithTextStyle(TextStyle style)
        => new(_foreground, (_backgroundAndFlags & ~TextStyleMask) | ((ulong)(byte)style << TextStyleShift) | TextStyleSpecifiedMask);

    /// <summary>
    /// Returns a copy with the specified text style flags added.
    /// </summary>
    public Style AddTextStyle(TextStyle style)
    {
        if (style == 0)
        {
            return this;
        }

        return new(_foreground, (_backgroundAndFlags | ((ulong)(byte)style << TextStyleShift)) | TextStyleSpecifiedMask);
    }

    /// <summary>
    /// Returns a copy with the specified text style flags removed.
    /// </summary>
    public Style RemoveTextStyle(TextStyle style)
    {
        if (style == 0)
        {
            return this;
        }

        return new(_foreground, (_backgroundAndFlags & ~((ulong)(byte)style << TextStyleShift)) | TextStyleSpecifiedMask);
    }

    /// <summary>
    /// Returns a copy with the foreground cleared to the terminal default (unspecified).
    /// </summary>
    public Style ClearForeground()
        => new(0, _backgroundAndFlags);

    /// <summary>
    /// Returns a copy with the background cleared to the terminal default (unspecified).
    /// </summary>
    public Style ClearBackground()
        => new(_foreground, BackgroundFlagsRaw);

    /// <summary>
    /// Returns a copy with the specified foreground color.
    /// </summary>
    public Style WithForeground(Color color)
        => new(color.ToRaw(), _backgroundAndFlags);

    /// <summary>
    /// Returns a copy with the specified background color.
    /// </summary>
    public Style WithBackground(Color color)
        => new(_foreground, (color.ToRaw() & BackgroundColorMask) | BackgroundFlagsRaw);

    /// <summary>
    /// Tries to get the foreground color.
    /// </summary>
    /// <param name="color">When this method returns, contains the foreground color if set.</param>
    /// <returns><see langword="true"/> if a foreground is explicitly set; otherwise <see langword="false"/>.</returns>
    public bool TryGetForeground(out Color color)
    {
        if (!HasForeground)
        {
            color = default;
            return false;
        }

        color = Color.FromRaw(_foreground);
        return true;
    }

    /// <summary>
    /// Tries to get the background color.
    /// </summary>
    /// <param name="color">When this method returns, contains the background color if set.</param>
    /// <returns><see langword="true"/> if a background is explicitly set; otherwise <see langword="false"/>.</returns>
    public bool TryGetBackground(out Color color)
    {
        if (!HasBackground)
        {
            color = default;
            return false;
        }

        color = Color.FromRaw(BackgroundColorRaw);
        return true;
    }

    /// <summary>
    /// Combines two styles.
    /// </summary>
    /// <remarks>
    /// This operator is intended for building composite styles. The decorations are combined via OR, while foreground
    /// and background colors behave like overrides: if <paramref name="b"/> specifies a color, it replaces the
    /// corresponding color from <paramref name="a"/>.
    /// </remarks>
    public static Style operator |(Style a, Style b)
    {
        var fg = b.HasForeground ? b._foreground : a._foreground;
        var bg = b.HasBackground ? b.BackgroundColorRaw : a.BackgroundColorRaw;

        var textStyle = (TextStyle)((byte)a.TextStyle | (byte)b.TextStyle);
        var flags = ((ulong)(byte)textStyle << TextStyleShift);

        if (a.IsContinuation || b.IsContinuation)
        {
            flags |= ContinuationMask;
        }

        if ((a._backgroundAndFlags & TextStyleSpecifiedMask) != 0 || (b._backgroundAndFlags & TextStyleSpecifiedMask) != 0)
        {
            flags |= TextStyleSpecifiedMask;
        }

        return new Style(fg, bg | flags);
    }

    /// <summary>
    /// Adds text style flags to a <see cref="Style"/>.
    /// </summary>
    public static Style operator |(Style a, TextStyle style) => a.AddTextStyle(style);

    /// <summary>
    /// ANDs the current text style flags with the provided style flags.
    /// </summary>
    public static Style operator &(Style a, TextStyle style) => a.WithTextStyle(a.TextStyle & style);

    /// <inheritdoc />
    public bool Equals(Style other) => _foreground == other._foreground && _backgroundAndFlags == other._backgroundAndFlags;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is Style other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(_foreground, _backgroundAndFlags);

    /// <summary>
    /// Returns whether two <see cref="Style"/> values are equal.
    /// </summary>
    public static bool operator ==(Style left, Style right) => left.Equals(right);

    /// <summary>
    /// Returns whether two <see cref="Style"/> values are not equal.
    /// </summary>
    public static bool operator !=(Style left, Style right) => !left.Equals(right);

    internal AnsiDecorations ToAnsiDecorations()
        => (AnsiDecorations)(int)TextStyle;

    internal Color GetForegroundOrDefault()
        => HasForeground ? Color.FromRaw(_foreground) : Color.Default;

    internal Color GetBackgroundOrDefault()
        => HasBackground ? Color.FromRaw(BackgroundColorRaw) : Color.Default;
}

