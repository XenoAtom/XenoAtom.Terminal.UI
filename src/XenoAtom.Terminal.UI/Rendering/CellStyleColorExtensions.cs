// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI;

public static class CellStyleColorExtensions
{
    // 0 = default; otherwise rgb24 packed value + 1 (so 0x000000 becomes 1)
    private const int BitsPerColor = 25;
    private const int ForegroundShift = 8;
    private const int BackgroundShift = ForegroundShift + BitsPerColor;

    private const ulong ColorMask = (1ul << BitsPerColor) - 1ul;
    private const ulong ForegroundMask = ColorMask << ForegroundShift;
    private const ulong BackgroundMask = ColorMask << BackgroundShift;

    public static CellStyle WithForeground(this CellStyle style, Rgb24 color)
    {
        var encoded = Encode(color.Packed);
        var value = (ulong)style;
        value = (value & ~ForegroundMask) | ((encoded & ColorMask) << ForegroundShift);
        return (CellStyle)value;
    }

    public static CellStyle WithBackground(this CellStyle style, Rgb24 color)
    {
        var encoded = Encode(color.Packed);
        var value = (ulong)style;
        value = (value & ~BackgroundMask) | ((encoded & ColorMask) << BackgroundShift);
        return (CellStyle)value;
    }

    public static CellStyle ClearForeground(this CellStyle style)
    {
        var value = (ulong)style;
        value &= ~ForegroundMask;
        return (CellStyle)value;
    }

    public static CellStyle ClearBackground(this CellStyle style)
    {
        var value = (ulong)style;
        value &= ~BackgroundMask;
        return (CellStyle)value;
    }

    public static bool TryGetForeground(this CellStyle style, out Rgb24 color)
    {
        var value = ((ulong)style & ForegroundMask) >> ForegroundShift;
        if (value == 0)
        {
            color = default;
            return false;
        }

        color = Rgb24.FromPacked(Decode(value));
        return true;
    }

    public static bool TryGetBackground(this CellStyle style, out Rgb24 color)
    {
        var value = ((ulong)style & BackgroundMask) >> BackgroundShift;
        if (value == 0)
        {
            color = default;
            return false;
        }

        color = Rgb24.FromPacked(Decode(value));
        return true;
    }

    private static ulong Encode(uint packedRgb) => (ulong)packedRgb + 1ul;
    private static uint Decode(ulong encoded) => (uint)(encoded - 1ul);
}

