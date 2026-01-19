// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.


// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Ansi;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI;

/// <summary>
/// Packed per-cell style (foreground/background + decorations).
/// </summary>
/// <remarks>
/// This type is a lightweight, value-type container optimized for cell buffers.
/// It preserves the kind of ANSI colors (default / 16 / 256 / RGB) so renderers can
/// emit the most appropriate escape sequences for the target terminal.
/// </remarks>
public readonly struct Style : IEquatable<Style>
{
    // Layout:
    // - Bits [0..7]   : TextStyle flags (matches AnsiDecorations bit positions)
    // - Bit  [8]      : Continuation (wide glyph trailing cell)
    // - Bits [9..34]  : Foreground (26 bits): [kind:2][value:24]
    // - Bits [35..60] : Background (26 bits): [kind:2][value:24]
    //
    // Color kind encoding (2 bits):
    // - 0: unset (terminal default color)
    // - 1: Basic16 index (value in [0..15])
    // - 2: Indexed256 index (value in [0..255])
    // - 3: RGB (value is packed 0xRRGGBB)
    private const int BitsPerColor = 26;
    private const int ContinuationBit = 8;

    private const int ForegroundShift = 9;
    private const int BackgroundShift = ForegroundShift + BitsPerColor;

    private const ulong TextStyleMask = 0xFFul;
    private const ulong ContinuationMask = 1ul << ContinuationBit;

    private const ulong ColorMask = (1ul << BitsPerColor) - 1ul;
    private const ulong ForegroundMask = ColorMask << ForegroundShift;
    private const ulong BackgroundMask = ColorMask << BackgroundShift;

    private const int ColorKindBits = 2;
    private const int ColorValueBits = 24;
    private const uint ColorValueMask = (1u << ColorValueBits) - 1u;

    internal readonly ulong Value;

    internal Style(ulong value) => Value = value;

    /// <summary>
    /// Gets a style with default foreground/background and no decorations.
    /// </summary>
    public static Style None => default;

    /// <summary>
    /// Gets the text style flags (decorations) for this cell.
    /// </summary>
    public TextStyle TextStyle => (TextStyle)(Value & TextStyleMask);

    internal bool IsContinuation => (Value & ContinuationMask) != 0;

    internal Style WithoutContinuation() => new(Value & ~ContinuationMask);

    internal Style WithContinuation()
        => new((Value & ~ContinuationMask) | ContinuationMask);

    internal Style MergeUnspecified(Style under)
    {
        var value = Value;
        var underValue = under.Value;

        if ((value & ForegroundMask) == 0)
        {
            value |= underValue & ForegroundMask;
        }

        if ((value & BackgroundMask) == 0)
        {
            value |= underValue & BackgroundMask;
        }

        if ((value & TextStyleMask) == 0)
        {
            value |= underValue & TextStyleMask;
        }

        return new Style(value);
    }

    /// <summary>
    /// Returns a copy with the specified text style flags (replacing any existing flags).
    /// </summary>
    public Style WithTextStyle(TextStyle style)
        => new((Value & ~TextStyleMask) | ((ulong)style & TextStyleMask));

    /// <summary>
    /// Returns a copy with the specified text style flags added.
    /// </summary>
    public Style AddTextStyle(TextStyle style)
        => new(Value | ((ulong)style & TextStyleMask));

    /// <summary>
    /// Returns a copy with the specified text style flags removed.
    /// </summary>
    public Style RemoveTextStyle(TextStyle style)
        => new(Value & ~((ulong)style & TextStyleMask));

    /// <summary>
    /// Returns a copy with the foreground cleared to terminal default.
    /// </summary>
    public Style ClearForeground()
        => new(Value & ~ForegroundMask);

    /// <summary>
    /// Returns a copy with the background cleared to terminal default.
    /// </summary>
    public Style ClearBackground()
        => new(Value & ~BackgroundMask);

    /// <summary>
    /// Returns a copy with the specified foreground color.
    /// </summary>
    public Style WithForeground(Color color)
    {
        var value = Value;
        value = (value & ~ForegroundMask) | (Encode(color) << ForegroundShift);
        return new Style(value);
    }

    /// <summary>
    /// Returns a copy with the specified background color.
    /// </summary>
    public Style WithBackground(Color color)
    {
        var value = Value;
        value = (value & ~BackgroundMask) | (Encode(color) << BackgroundShift);
        return new Style(value);
    }

    /// <summary>
    /// Tries to get the foreground color.
    /// </summary>
    /// <param name="color">When this method returns, contains the foreground color if set.</param>
    /// <returns><c>true</c> if a foreground is explicitly set; otherwise <c>false</c>.</returns>
    public bool TryGetForeground(out Color color)
    {
        var encoded = (Value & ForegroundMask) >> ForegroundShift;
        if (encoded == 0)
        {
            color = default;
            return false;
        }

        return TryDecodeToColor(encoded, out color);
    }

    /// <summary>
    /// Tries to get the background color.
    /// </summary>
    /// <param name="color">When this method returns, contains the background color if set.</param>
    /// <returns><c>true</c> if a background is explicitly set; otherwise <c>false</c>.</returns>
    public bool TryGetBackground(out Color color)
    {
        var encoded = (Value & BackgroundMask) >> BackgroundShift;
        if (encoded == 0)
        {
            color = default;
            return false;
        }

        return TryDecodeToColor(encoded, out color);
    }

    /// <summary>
    /// Combines two styles by OR-ing their packed representation.
    /// </summary>
    public static Style operator |(Style a, Style b) => new(a.Value | b.Value);

    /// <summary>
    /// Adds text style flags to a <see cref="Style"/>.
    /// </summary>
    public static Style operator |(Style a, TextStyle style) => a.AddTextStyle(style);

    /// <summary>
    /// ANDs the packed value with the provided style flags.
    /// </summary>
    public static Style operator &(Style a, TextStyle style) => new(a.Value & (ulong)style);

    /// <inheritdoc />
    public bool Equals(Style other) => Value == other.Value;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is Style other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => Value.GetHashCode();

    /// <summary>
    /// Returns whether two <see cref="Style"/> values are equal.
    /// </summary>
    public static bool operator ==(Style left, Style right) => left.Equals(right);

    /// <summary>
    /// Returns whether two <see cref="Style"/> values are not equal.
    /// </summary>
    public static bool operator !=(Style left, Style right) => !left.Equals(right);

    internal AnsiDecorations ToAnsiDecorations()
        => (AnsiDecorations)((int)TextStyle);

    private static ulong Encode(Color color)
    {
        switch (color.Kind)
        {
            case ColorKind.Default:
                return 0;
            case ColorKind.Basic16:
                return EncodeColor(kind: 1u, value: (uint)color.Index);
            case ColorKind.Indexed256:
                return EncodeColor(kind: 2u, value: (uint)color.Index);
            case ColorKind.Rgb:
                return EncodeColor(kind: 3u, value: (uint)((color.R << 16) | (color.G << 8) | color.B));
            default:
                return 0;
        }
    }

    private static ulong EncodeColor(uint kind, uint value)
    {
        if ((kind & ((1u << ColorKindBits) - 1u)) == 0)
        {
            return 0;
        }

        value &= ColorValueMask;
        return (ulong)((kind << ColorValueBits) | value);
    }

    private static bool TryDecodeToColor(ulong encoded, out Color color)
    {
        var kind = (uint)((encoded >> ColorValueBits) & 0b11);
        var value = (uint)encoded & ColorValueMask;

        switch (kind)
        {
            case 1:
                color = Color.Basic16((int)(value & 0xF));
                return true;
            case 2:
                color = Color.Indexed256((int)(value & 0xFF));
                return true;
            case 3:
            {
                var r = (byte)((value >> 16) & 0xFF);
                var g = (byte)((value >> 8) & 0xFF);
                var b = (byte)(value & 0xFF);
                color = Color.Rgb(r, g, b);
                return true;
            }
            default:
                color = default;
                return false;
        }
    }
}

