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

    /// <summary>
    /// Returns true when <paramref name="value"/> should be treated as unbounded.
    /// </summary>
    public static bool IsInfinite(int value)
        // We treat values extremely close to int.MaxValue as unbounded so that internal arithmetic
        // (e.g. subtracting borders/padding) doesn't accidentally turn "infinite" into a huge finite number.
        => value >= int.MaxValue - 1024;
}

