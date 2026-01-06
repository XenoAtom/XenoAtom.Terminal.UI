// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Ansi;

namespace XenoAtom.Terminal.UI.Styling;

/// <summary>
/// Packed per-cell style (foreground/background + decorations).
/// </summary>
/// <remarks>
/// This type is a lightweight, value-type container optimized for cell buffers.
/// It encodes colors as 24-bit RGB (assuming truecolor is available).
/// </remarks>
public readonly struct CellStyle : IEquatable<CellStyle>
{
    // Layout:
    // - Bits [0..7]   : TextStyle flags (matches AnsiDecorations bit positions)
    // - Bit  [8]      : Continuation (wide glyph trailing cell)
    // - Bits [14..38] : Foreground encoded RGB (25 bits, 0=unset, otherwise packedRgb+1)
    // - Bits [39..63] : Background encoded RGB (25 bits, 0=unset, otherwise packedRgb+1)
    private const int BitsPerColor = 25;
    private const int ContinuationBit = 8;

    private const int ForegroundShift = 14;
    private const int BackgroundShift = ForegroundShift + BitsPerColor;

    private const ulong TextStyleMask = 0xFFul;
    private const ulong ContinuationMask = 1ul << ContinuationBit;

    private const ulong ColorMask = (1ul << BitsPerColor) - 1ul;
    private const ulong ForegroundMask = ColorMask << ForegroundShift;
    private const ulong BackgroundMask = ColorMask << BackgroundShift;

    internal readonly ulong Value;

    internal CellStyle(ulong value) => Value = value;

    public static CellStyle None => default;

    public TextStyle TextStyle => (TextStyle)(Value & TextStyleMask);

    internal bool IsContinuation => (Value & ContinuationMask) != 0;

    internal CellStyle WithoutContinuation() => new(Value & ~ContinuationMask);

    internal CellStyle WithContinuation()
        => new((Value & ~ContinuationMask) | ContinuationMask);

    internal CellStyle MergeUnspecified(CellStyle under)
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

        return new CellStyle(value);
    }

    public CellStyle WithTextStyle(TextStyle style)
        => new((Value & ~TextStyleMask) | ((ulong)style & TextStyleMask));

    public CellStyle AddTextStyle(TextStyle style)
        => new(Value | ((ulong)style & TextStyleMask));

    public CellStyle RemoveTextStyle(TextStyle style)
        => new(Value & ~((ulong)style & TextStyleMask));

    public CellStyle ClearForeground()
        => new(Value & ~ForegroundMask);

    public CellStyle ClearBackground()
        => new(Value & ~BackgroundMask);

    public CellStyle WithForeground(AnsiColor color)
    {
        if (!TryGetRgb(color, out var packedRgb))
        {
            return ClearForeground();
        }

        var encoded = Encode(packedRgb);
        var value = Value;
        value = (value & ~ForegroundMask) | ((encoded & ColorMask) << ForegroundShift);
        return new CellStyle(value);
    }

    public CellStyle WithBackground(AnsiColor color)
    {
        if (!TryGetRgb(color, out var packedRgb))
        {
            return ClearBackground();
        }

        var encoded = Encode(packedRgb);
        var value = Value;
        value = (value & ~BackgroundMask) | ((encoded & ColorMask) << BackgroundShift);
        return new CellStyle(value);
    }

    public bool TryGetForeground(out AnsiColor color)
    {
        var encoded = (Value & ForegroundMask) >> ForegroundShift;
        if (encoded == 0)
        {
            color = default;
            return false;
        }

        color = DecodeToAnsiColor(encoded);
        return true;
    }

    public bool TryGetBackground(out AnsiColor color)
    {
        var encoded = (Value & BackgroundMask) >> BackgroundShift;
        if (encoded == 0)
        {
            color = default;
            return false;
        }

        color = DecodeToAnsiColor(encoded);
        return true;
    }

    public static CellStyle operator |(CellStyle a, CellStyle b) => new(a.Value | b.Value);

    public static CellStyle operator |(CellStyle a, TextStyle style) => a.AddTextStyle(style);

    public static CellStyle operator &(CellStyle a, TextStyle style) => new(a.Value & (ulong)style);

    public bool Equals(CellStyle other) => Value == other.Value;

    public override bool Equals(object? obj) => obj is CellStyle other && Equals(other);

    public override int GetHashCode() => Value.GetHashCode();

    public static bool operator ==(CellStyle left, CellStyle right) => left.Equals(right);

    public static bool operator !=(CellStyle left, CellStyle right) => !left.Equals(right);

    internal AnsiDecorations ToAnsiDecorations()
        => (AnsiDecorations)((int)TextStyle);

    private static bool TryGetRgb(AnsiColor color, out uint packedRgb)
    {
        switch (color.Kind)
        {
            case AnsiColorKind.Default:
                packedRgb = 0;
                return false;
            case AnsiColorKind.Rgb:
                packedRgb = (uint)((color.R << 16) | (color.G << 8) | color.B);
                return true;
            case AnsiColorKind.Basic16:
            {
                var (r, g, b) = AnsiPalettes.GetBasic16Rgb(color.Index);
                packedRgb = (uint)((r << 16) | (g << 8) | b);
                return true;
            }
            case AnsiColorKind.Indexed256:
            {
                var (r, g, b) = AnsiPalettes.GetXterm256Rgb(color.Index);
                packedRgb = (uint)((r << 16) | (g << 8) | b);
                return true;
            }
            default:
                packedRgb = 0;
                return false;
        }
    }

    private static ulong Encode(uint packedRgb) => (ulong)packedRgb + 1ul;

    private static AnsiColor DecodeToAnsiColor(ulong encoded)
    {
        var packed = (uint)(encoded - 1ul);
        var r = (byte)((packed >> 16) & 0xFF);
        var g = (byte)((packed >> 8) & 0xFF);
        var b = (byte)(packed & 0xFF);
        return AnsiColor.Rgb(r, g, b);
    }
}

