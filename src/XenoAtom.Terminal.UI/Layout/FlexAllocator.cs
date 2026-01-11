// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI.Layout;

internal static class FlexAllocator
{
    public static void Allocate(int available, ReadOnlySpan<int> min, ReadOnlySpan<int> natural, ReadOnlySpan<int> max, ReadOnlySpan<int> grow, ReadOnlySpan<int> shrink, Span<int> result)
    {
        if (min.Length != natural.Length || min.Length != max.Length || min.Length != grow.Length || min.Length != shrink.Length || min.Length != result.Length)
        {
            throw new ArgumentException("All spans must have the same length.");
        }

        available = Math.Max(0, available);

        var count = result.Length;
        long sum = 0;
        for (var i = 0; i < count; i++)
        {
            var n = Math.Max(0, natural[i]);
            result[i] = n;
            sum += n;
        }

        if (sum == available)
        {
            return;
        }

        if (sum < available)
        {
            Grow(available - sum, min, max, grow, result);
            return;
        }

        Shrink(sum - available, min, shrink, result);
    }

    private static void Grow(long extra, ReadOnlySpan<int> min, ReadOnlySpan<int> max, ReadOnlySpan<int> grow, Span<int> result)
    {
        _ = min;
        if (extra <= 0)
        {
            return;
        }

        var count = result.Length;
        var totalGrow = 0;
        for (var i = 0; i < count; i++)
        {
            totalGrow += Math.Max(0, grow[i]);
        }

        if (totalGrow <= 0)
        {
            return;
        }

        long distributed = 0;
        for (var i = 0; i < count; i++)
        {
            var g = Math.Max(0, grow[i]);
            if (g <= 0)
            {
                continue;
            }

            var share = (extra * g) / totalGrow;
            if (share <= 0)
            {
                continue;
            }

            var current = result[i];
            var maxI = max[i];

            var cap = LayoutConstants.IsInfinite(maxI) ? long.MaxValue : Math.Max(0, maxI) - current;
            var add = Math.Min(share, cap);
            if (add <= 0)
            {
                continue;
            }

            result[i] = LayoutConstants.ClampFinite((long)current + add);
            distributed += add;
        }

        var remaining = extra - distributed;
        if (remaining <= 0)
        {
            return;
        }

        for (var i = 0; i < count && remaining > 0; i++)
        {
            if (grow[i] <= 0)
            {
                continue;
            }

            var current = result[i];
            var maxI = max[i];
            if (!LayoutConstants.IsInfinite(maxI) && current >= maxI)
            {
                continue;
            }

            result[i] = LayoutConstants.ClampFinite((long)current + 1);
            remaining--;
        }
    }

    private static void Shrink(long deficit, ReadOnlySpan<int> min, ReadOnlySpan<int> shrink, Span<int> result)
    {
        if (deficit <= 0)
        {
            return;
        }

        var count = result.Length;
        var totalShrink = 0;
        for (var i = 0; i < count; i++)
        {
            totalShrink += Math.Max(0, shrink[i]);
        }

        if (totalShrink <= 0)
        {
            return;
        }

        long removed = 0;
        for (var i = 0; i < count; i++)
        {
            var s = Math.Max(0, shrink[i]);
            if (s <= 0)
            {
                continue;
            }

            var share = (deficit * s) / totalShrink;
            if (share <= 0)
            {
                continue;
            }

            var current = result[i];
            var minI = Math.Max(0, min[i]);
            var cap = current - minI;
            var sub = Math.Min((long)cap, share);
            if (sub <= 0)
            {
                continue;
            }

            result[i] = current - (int)sub;
            removed += sub;
        }

        var remaining = deficit - removed;
        if (remaining <= 0)
        {
            return;
        }

        for (var i = 0; i < count && remaining > 0; i++)
        {
            if (shrink[i] <= 0)
            {
                continue;
            }

            var current = result[i];
            var minI = Math.Max(0, min[i]);
            if (current <= minI)
            {
                continue;
            }

            result[i] = current - 1;
            remaining--;
        }
    }
}

