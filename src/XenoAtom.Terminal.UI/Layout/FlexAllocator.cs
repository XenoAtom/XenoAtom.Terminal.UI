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
        int sum = 0;
        try
        {
            checked
            {
                for (var i = 0; i < count; i++)
                {
                    var n = Math.Max(0, natural[i]);
                    result[i] = n;
                    sum += n;
                }
            }
        }
        catch (OverflowException ex)
        {
            throw new LayoutException("Flex allocation overflow while summing natural sizes.", ex);
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
        try
        {
            checked
            {
                for (var i = 0; i < count; i++)
                {
                    totalGrow += Math.Max(0, grow[i]);
                }
            }
        }
        catch (OverflowException ex)
        {
            throw new LayoutException("Flex allocation overflow while summing grow weights.", ex);
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
            try
            {
                share = checked((extra * g) / totalGrow);
            }
            catch (OverflowException ex)
            {
                throw new LayoutException("Flex allocation overflow while computing grow share.", ex);
            }

            var current = result[i];
            var maxI = max[i];

            var cap = LayoutConstants.IsInfinite(maxI) ? int.MaxValue : Math.Max(0, maxI - current);
            var add = Math.Min(Math.Max(0, share), cap);
            if (add <= 0)
            {
                continue;
            }

            try
            {
                result[i] = LayoutConstants.ClampFinite(checked(current + add));
                distributed = checked(distributed + add);
            }
            catch (OverflowException ex)
            {
                throw new LayoutException("Flex allocation overflow while applying grow share.", ex);
            }
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

            try
            {
                result[i] = LayoutConstants.ClampFinite(checked(current + 1));
            }
            catch (OverflowException ex)
            {
                throw new LayoutException("Flex allocation overflow while distributing grow remainder.", ex);
            }
            remaining--;
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
        try
        {
            checked
            {
                for (var i = 0; i < count; i++)
                {
                    totalShrink += Math.Max(0, shrink[i]);
                }
            }
        }
        catch (OverflowException ex)
        {
            throw new LayoutException("Flex allocation overflow while summing shrink weights.", ex);
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
            try
            {
                share = checked((deficit * s) / totalShrink);
            }
            catch (OverflowException ex)
            {
                throw new LayoutException("Flex allocation overflow while computing shrink share.", ex);
            }

            var current = result[i];
            var minI = Math.Max(0, min[i]);
            var cap = current - minI;
            var sub = Math.Min(Math.Max(0, cap), Math.Max(0, share));
            if (sub <= 0)
            {
                continue;
            }

            result[i] = current - sub;
            try
            {
                removed = checked(removed + sub);
            }
            catch (OverflowException ex)
            {
                throw new LayoutException("Flex allocation overflow while applying shrink share.", ex);
            }
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
