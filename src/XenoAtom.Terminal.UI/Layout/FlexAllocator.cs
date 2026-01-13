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
        var sum = 0;
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
            Grow(available - sum, max, grow, result);
            return;
        }

        Shrink(sum - available, min, shrink, result);
    }

    private static void Grow(int extra, ReadOnlySpan<int> max, ReadOnlySpan<int> grow, Span<int> result)
    {
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

        var distributed = 0;
        for (var i = 0; i < count; i++)
        {
            var g = Math.Max(0, grow[i]);
            if (g <= 0)
            {
                continue;
            }

            int share;
            share = (extra * g) / totalGrow;

            var current = result[i];
            var maxI = max[i];

            var cap = LayoutConstants.IsInfinite(maxI) ? int.MaxValue : Math.Max(0, maxI - current);
            var add = Math.Min(Math.Max(0, share), cap);
            if (add <= 0)
            {
                continue;
            }

            result[i] = LayoutConstants.ClampFinite(current + add);
            distributed += add;
        }

        var remaining = extra - distributed;
        if (remaining <= 0)
        {
            return;
        }

        while (remaining > 0)
        {
            var progressed = false;

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

                result[i] = LayoutConstants.ClampFinite(current + 1);

                remaining--;
                progressed = true;
            }

            if (!progressed)
            {
                break;
            }
        }
    }

    private static void Shrink(int deficit, ReadOnlySpan<int> min, ReadOnlySpan<int> shrink, Span<int> result)
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

        var removed = 0;
        for (var i = 0; i < count; i++)
        {
            var s = Math.Max(0, shrink[i]);
            if (s <= 0)
            {
                continue;
            }

            int share;
            share = (deficit * s) / totalShrink;

            var current = result[i];
            var minI = Math.Max(0, min[i]);
            var cap = current - minI;
            var sub = Math.Min(Math.Max(0, cap), Math.Max(0, share));
            if (sub <= 0)
            {
                continue;
            }

            result[i] = current - sub;
            removed += sub;
        }

        var remaining = deficit - removed;
        if (remaining <= 0)
        {
            return;
        }

        while (remaining > 0)
        {
            var progressed = false;

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
                progressed = true;
            }

            if (!progressed)
            {
                break;
            }
        }
    }
}
