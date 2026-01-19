// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Ansi;

namespace XenoAtom.Terminal.UI;

/// <summary>
/// Represents a terminal color value that can be encoded/decoded via SGR parameters.
/// </summary>
/// <remarks>
/// This type models the common ANSI/VT color forms used by modern terminals:
/// <list type="bullet">
/// <item><description>Default foreground/background (SGR 39/49)</description></item>
/// <item><description>Basic 16-color palette indices (SGR 30–37/90–97 and 40–47/100–107)</description></item>
/// <item><description>256-color indexed palette (SGR 38;5;n / 48;5;n)</description></item>
/// <item><description>Truecolor (24-bit RGB) (SGR 38;2;r;g;b / 48;2;r;g;b)</description></item>
/// <item><description>Truecolor + Alpha (32-bit RGBA). This is a pseudo terminal color used by this library to simulate color blending.</description></item>
/// </list>
/// </remarks>
public readonly record struct Color
{
    /// <summary>
    /// The terminal default color.
    /// </summary>
    public static readonly Color Default = default;

    // Layout:
    // - bits 0..7   : ColorKind
    // - bits 8..15  : palette index (for Indexed kinds)
    // - bits 16..31 : reserved (must be 0 for standalone Color values)
    // - bits 32..39 : R
    // - bits 40..47 : G
    // - bits 48..55 : B
    // - bits 56..63 : A (for RgbA)
    //
    // The upper 32 bits can be read as a packed 0xAABBGGRR (aka "ABGR" in hex, little-endian bytes are RGBA):
    // (uint)(_value >> 32)
    private readonly ulong _value;

    private Color(ulong value) => _value = value;

    /// <summary>
    /// Gets the kind of this color value.
    /// </summary>
    public ColorKind Kind => (ColorKind)(byte)_value;

    /// <summary>
    /// Gets the palette index for <see cref="ColorKind.Basic16"/> or <see cref="ColorKind.Indexed256"/>.
    /// </summary>
    public byte Index => Kind is ColorKind.Basic16 or ColorKind.Indexed256 ? (byte)(_value >> 8) : (byte)0;

    /// <summary>
    /// Gets the red component for <see cref="ColorKind.Rgb"/> and <see cref="ColorKind.RgbA"/>.
    /// </summary>
    public byte R => Kind is ColorKind.Rgb or ColorKind.RgbA ? (byte)(_value >> 32) : (byte)0;

    /// <summary>
    /// Gets the green component for <see cref="ColorKind.Rgb"/> and <see cref="ColorKind.RgbA"/>.
    /// </summary>
    public byte G => Kind is ColorKind.Rgb or ColorKind.RgbA ? (byte)(_value >> 40) : (byte)0;

    /// <summary>
    /// Gets the blue component for <see cref="ColorKind.Rgb"/> and <see cref="ColorKind.RgbA"/>.
    /// </summary>
    public byte B => Kind is ColorKind.Rgb or ColorKind.RgbA ? (byte)(_value >> 48) : (byte)0;

    /// <summary>
    /// Gets the alpha component for <see cref="ColorKind.RgbA"/>.
    /// </summary>
    /// <remarks>
    /// This alpha channel is a pseudo terminal feature used by this library to simulate transparency effects.
    /// For non-RGBA colors, this property returns 255 (opaque).
    /// </remarks>
    public byte A => Kind == ColorKind.RgbA ? (byte)(_value >> 56) : (byte)255;

    /// <summary>
    /// Gets the packed RGBA bytes as a 32-bit value (0xAABBGGRR).
    /// </summary>
    /// <remarks>
    /// This representation is optimized for fast extraction and SIMD conversion.
    /// </remarks>
    public uint RgbaPacked => (uint)(_value >> 32);

    /// <summary>
    /// Creates a basic 16-color palette value.
    /// </summary>
    /// <param name="index">The palette index in range [0, 15].</param>
    public static Color Basic16(int index)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(index, 0);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(index, 15);

        return new Color(PackIndexed(ColorKind.Basic16, (byte)index));
    }

    /// <summary>
    /// Creates a 256-color palette value.
    /// </summary>
    /// <param name="index">The palette index in range [0, 255].</param>
    public static Color Indexed256(int index)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(index, 0);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(index, 255);

        return new Color(PackIndexed(ColorKind.Indexed256, (byte)index));
    }

    /// <summary>
    /// Creates a truecolor RGB value.
    /// </summary>
    public static Color Rgb(byte r, byte g, byte b) => new(PackRgba(ColorKind.Rgb, r, g, b, a: 255));

    /// <summary>
    /// Creates a truecolor RGBA value.
    /// </summary>
    /// <remarks>
    /// This is a pseudo terminal color used by this library to simulate alpha blending during rendering.
    /// Most terminals do not support alpha in SGR, so the alpha is ignored when converting to ANSI.
    /// </remarks>
    public static Color RgbA(byte r, byte g, byte b, byte a) => new(PackRgba(ColorKind.RgbA, r, g, b, a));

    /// <summary>
    /// Converts a <see cref="ConsoleColor"/> to an ANSI basic-16 color.
    /// </summary>
    /// <remarks>
    /// This maps the Windows <see cref="ConsoleColor"/> values to the conventional ANSI basic-16 palette indices:
    /// <c>0..7</c> for normal colors and <c>8..15</c> for bright colors.
    /// </remarks>
    public static implicit operator Color(ConsoleColor color)
    {
        // ConsoleColor values are not in ANSI order (e.g. DarkBlue is 1). We map explicitly.
        var index = color switch
        {
            ConsoleColor.Black => 0,
            ConsoleColor.DarkRed => 1,
            ConsoleColor.DarkGreen => 2,
            ConsoleColor.DarkYellow => 3,
            ConsoleColor.DarkBlue => 4,
            ConsoleColor.DarkMagenta => 5,
            ConsoleColor.DarkCyan => 6,
            ConsoleColor.Gray => 7,
            ConsoleColor.DarkGray => 8,
            ConsoleColor.Red => 9,
            ConsoleColor.Green => 10,
            ConsoleColor.Yellow => 11,
            ConsoleColor.Blue => 12,
            ConsoleColor.Magenta => 13,
            ConsoleColor.Cyan => 14,
            ConsoleColor.White => 15,
            _ => 7,
        };

        return Basic16(index);
    }

    /// <summary>
    /// Attempts to downgrade this color to a maximum supported <see cref="AnsiColorLevel"/>.
    /// </summary>
    /// <param name="maxLevel">The maximum supported level.</param>
    /// <param name="downgraded">The resulting color value.</param>
    /// <returns><see langword="true"/> if the color could be represented at the requested level.</returns>
    /// <remarks>
    /// For example, an RGB color can be approximated as an xterm 256-color index, which can then be approximated
    /// as a basic 16-color index.
    /// </remarks>
    public bool TryDowngrade(AnsiColorLevel maxLevel, out Color downgraded)
    {
        if (((AnsiColor)this).TryDowngrade(maxLevel, out var ansiColor))
        {
            downgraded = (Color)ansiColor;
            return true;
        }

        downgraded = Default;
        return false;
    }

    internal static Color FromRaw(ulong raw) => new(raw);

    internal ulong ToRaw() => _value;

    private static ulong PackIndexed(ColorKind kind, byte index)
        => (ulong)(byte)kind | ((ulong)index << 8);

    private static ulong PackRgba(ColorKind kind, byte r, byte g, byte b, byte a)
    {
        var rgba = (uint)(r | (g << 8) | (b << 16) | (a << 24));
        return (ulong)(byte)kind | ((ulong)rgba << 32);
    }

    /// <summary>
    /// Converts an <see cref="AnsiColor"/> instance to a <see cref="Color"/> value using the appropriate color
    /// representation.
    /// </summary>
    /// <remarks>The conversion uses the <see cref="AnsiColor.Kind"/> property to determine how to map the
    /// value to a <see cref="Color"/>. If the kind is not recognized, the default color is returned.</remarks>
    /// <param name="color">The <see cref="AnsiColor"/> instance to convert.</param>
    public static implicit operator Color(AnsiColor color)
    {
        return color.Kind switch
        {
            AnsiColorKind.Default => Default,
            AnsiColorKind.Basic16 => Basic16(color.Index),
            AnsiColorKind.Indexed256 => Indexed256(color.Index),
            AnsiColorKind.Rgb => Rgb(color.R, color.G, color.B),
            _ => Default,
        };
    }

    /// <summary>
    /// Converts a <see cref="Color"/> instance to its corresponding <see cref="AnsiColor"/> representation.
    /// </summary>
    /// <remarks>The conversion maps the <see cref="ColorKind"/> of the input to the closest matching <see
    /// cref="AnsiColor"/>. For <see cref="ColorKind.Rgba"/>, the alpha channel is ignored and only the RGB components
    /// are used.</remarks>
    /// <param name="color">The <see cref="Color"/> value to convert to an <see cref="AnsiColor"/>.</param>
    public static implicit operator AnsiColor (Color color)
    {
        return color.Kind switch
        {
            ColorKind.Default => AnsiColor.Default,
            ColorKind.Basic16 => AnsiColor.Basic16(color.Index),
            ColorKind.Indexed256 => AnsiColor.Indexed256(color.Index),
            ColorKind.Rgb => AnsiColor.Rgb(color.R, color.G, color.B),
            ColorKind.RgbA => AnsiColor.Rgb(color.R, color.G, color.B),
            _ => AnsiColor.Default,
        };
    }
}

/// <summary>
/// Identifies which kind of color value is represented by an <see cref="Color" />.
/// </summary>
public enum ColorKind : byte
{
    /// <summary>The terminal default color.</summary>
    Default,
    /// <summary>One of the 16 basic palette indices (0-15).</summary>
    Basic16,
    /// <summary>One of the 256-color xterm palette indices (0-255).</summary>
    Indexed256,
    /// <summary>A 24-bit RGB color (truecolor).</summary>
    Rgb,
    /// <summary>A 32-bit RGBA color (truecolor with alpha). This is a pseudo color for blending colors in Terminal UI.</summary>
    RgbA,

    /// <summary>
    /// A 32-bit RGBA color (truecolor with alpha). This is a pseudo color for blending colors in Terminal UI.
    /// </summary>
    /// <remarks>
    /// Prefer <see cref="RgbA"/>. This member exists for backward compatibility.
    /// </remarks>
    Rgba = RgbA,
}

