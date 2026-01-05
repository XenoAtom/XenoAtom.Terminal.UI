// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI;

public static class CellStyleExtensions
{
    private const int BitsPerColor = 5; // 0 = default, 1..16 = basic16 index+1
    private const int ForegroundShift = 8;
    private const int BackgroundShift = ForegroundShift + BitsPerColor;
    private const uint ColorMask = (1u << BitsPerColor) - 1u;

    private const uint ForegroundMask = ColorMask << ForegroundShift;
    private const uint BackgroundMask = ColorMask << BackgroundShift;

    public static CellStyle WithForegroundBasic16(this CellStyle style, int index)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(index, 0);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(index, 15);

        var value = (uint)style;
        value = (value & ~ForegroundMask) | (((uint)(index + 1) & ColorMask) << ForegroundShift);
        return (CellStyle)value;
    }

    public static CellStyle WithBackgroundBasic16(this CellStyle style, int index)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(index, 0);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(index, 15);

        var value = (uint)style;
        value = (value & ~BackgroundMask) | (((uint)(index + 1) & ColorMask) << BackgroundShift);
        return (CellStyle)value;
    }

    public static CellStyle ClearForeground(this CellStyle style)
    {
        var value = (uint)style;
        value &= ~ForegroundMask;
        return (CellStyle)value;
    }

    public static CellStyle ClearBackground(this CellStyle style)
    {
        var value = (uint)style;
        value &= ~BackgroundMask;
        return (CellStyle)value;
    }

    public static bool TryGetForegroundBasic16(this CellStyle style, out int index)
    {
        var value = ((uint)style & ForegroundMask) >> ForegroundShift;
        if (value == 0)
        {
            index = -1;
            return false;
        }

        index = (int)value - 1;
        return true;
    }

    public static bool TryGetBackgroundBasic16(this CellStyle style, out int index)
    {
        var value = ((uint)style & BackgroundMask) >> BackgroundShift;
        if (value == 0)
        {
            index = -1;
            return false;
        }

        index = (int)value - 1;
        return true;
    }
}

