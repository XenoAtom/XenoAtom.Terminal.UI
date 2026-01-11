// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI;

internal static class LayoutConstants
{
    /// <summary>
    /// Sentinel used by the layout system to represent an unbounded (infinite) measure constraint.
    /// </summary>
    public const int Infinite = int.MaxValue;

    public const int MaxFinite = int.MaxValue - 1;

    /// <summary>
    /// Returns true when <paramref name="value"/> should be treated as unbounded.
    /// </summary>
    public static bool IsInfinite(int value)
        => value == Infinite;

    public static int ClampFinite(long value)
    {
        if (value <= 0)
        {
            return 0;
        }

        return value >= MaxFinite ? MaxFinite : (int)value;
    }

    public static int ClampOrInfinite(long value)
    {
        if (value <= 0)
        {
            return 0;
        }

        return value >= Infinite ? Infinite : (int)value;
    }
}

