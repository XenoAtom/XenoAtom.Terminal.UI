// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Buffers;
using System.ComponentModel;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Layout;

namespace XenoAtom.Terminal.UI.Controls;

/// <summary>
/// Base class for wrapping stack panels.
/// </summary>
/// <remarks>
/// This type exists to share implementation between <see cref="WrapHStack"/> and <see cref="WrapVStack"/>.
/// Prefer using the concrete controls directly.
/// </remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
public abstract partial class WrapStackBase : Panel
{
    private readonly List<Run> _runs = new();
    private int _lastRunMain = -1;
    private int _lastChildrenVersion = -1;
    private int _lastSpacing = -1;
    private int _lastRunSpacing = -1;
    private WrapJustify _lastJustify;
    private WrapMeasureMode _lastMeasureMode;
    private LayoutConstraints _lastMeasureConstraints;

    /// <summary>
    /// Initializes a new instance of the <see cref="WrapStackBase"/> class.
    /// </summary>
    protected WrapStackBase()
    {
        Justify = WrapJustify.Start;
        MeasureMode = WrapMeasureMode.ConstrainToRun;
    }

    /// <summary>
    /// Gets or sets the spacing between items in the same run (row/column).
    /// </summary>
    [Bindable]
    public partial int Spacing { get; set; }

    /// <summary>
    /// Gets or sets the spacing between runs.
    /// </summary>
    [Bindable]
    public partial int RunSpacing { get; set; }

    /// <summary>
    /// Gets or sets the justification of items along the main axis within each run.
    /// </summary>
    [Bindable]
    public partial WrapJustify Justify { get; set; }

    /// <summary>
    /// Gets or sets how children are measured along the main axis.
    /// </summary>
    [Bindable]
    public partial WrapMeasureMode MeasureMode { get; set; }

    /// <summary>
    /// Gets a value indicating whether the main axis is horizontal (wrap into rows) or vertical (wrap into columns).
    /// </summary>
    protected abstract bool IsHorizontal { get; }

    /// <inheritdoc />
    protected override SizeHints MeasureCore(in LayoutConstraints constraints)
    {
        _lastMeasureConstraints = constraints;

        var spacing = Math.Max(0, Spacing);
        var runSpacing = Math.Max(0, RunSpacing);

        var maxMain = IsHorizontal ? constraints.MaxWidth : constraints.MaxHeight;
        var childMaxMain = MeasureMode == WrapMeasureMode.Unconstrained ? LayoutConstants.Infinite : maxMain;

        var childCount = Children.Count;
        if (childCount == 0)
        {
            _runs.Clear();
            _lastRunMain = maxMain;
            _lastChildrenVersion = Children.Version;
            _lastSpacing = spacing;
            _lastRunSpacing = runSpacing;
            _lastJustify = Justify;
            _lastMeasureMode = MeasureMode;
            return SizeHints.Fixed(Size.Zero);
        }

        for (var i = 0; i < childCount; i++)
        {
            var child = Children[i];
            var childConstraints = IsHorizontal
                ? new LayoutConstraints(0, childMaxMain, constraints.MinHeight, constraints.MaxHeight)
                : new LayoutConstraints(constraints.MinWidth, constraints.MaxWidth, 0, childMaxMain);
            child.Measure(childConstraints);
        }

        BuildRuns(maxMain, spacing);

        var naturalMain = 0;
        var naturalCross = 0;

        var minMain = 0;
        var minCross = 0;

        var maxCrossInfinite = false;

        for (var runIndex = 0; runIndex < _runs.Count; runIndex++)
        {
            var run = _runs[runIndex];
            naturalMain = Math.Max(naturalMain, run.MainNatural);
            naturalCross += run.CrossNatural;

            minMain = Math.Max(minMain, run.MainMin);
            minCross += run.CrossMin;

            if (run.MaxCrossInfinite)
            {
                maxCrossInfinite = true;
            }

            if (runIndex + 1 < _runs.Count)
            {
                naturalCross += runSpacing;
                minCross += runSpacing;
            }
        }

        if (maxMain != LayoutConstants.Infinite)
        {
            naturalMain = Math.Min(naturalMain, maxMain);
        }

        minMain = Math.Min(minMain, naturalMain);
        minCross = Math.Min(minCross, naturalCross);

        var min = IsHorizontal
            ? new Size(minMain, minCross)
            : new Size(minCross, minMain);

        var natural = IsHorizontal
            ? new Size(naturalMain, naturalCross)
            : new Size(naturalCross, naturalMain);

        var max = IsHorizontal
            ? new Size(LayoutConstants.Infinite, maxCrossInfinite ? LayoutConstants.Infinite : naturalCross)
            : new Size(maxCrossInfinite ? LayoutConstants.Infinite : naturalCross, LayoutConstants.Infinite);

        var growX = HorizontalAlignment == Align.Stretch ? 1 : 0;
        var growY = VerticalAlignment == Align.Stretch ? 1 : 0;
        var shrinkX = natural.Width > min.Width ? 1 : 0;
        var shrinkY = natural.Height > min.Height ? 1 : 0;

        return SizeHints.Flex(min, natural, max, growX, growY, shrinkX, shrinkY).Normalize();
    }

    /// <inheritdoc />
    protected override void ArrangeCore(in Rectangle finalRect)
    {
        var spacing = Math.Max(0, Spacing);
        var runSpacing = Math.Max(0, RunSpacing);

        var finalMain = IsHorizontal ? finalRect.Width : finalRect.Height;

        if (MeasureMode == WrapMeasureMode.ConstrainToRun && finalMain != _lastRunMain)
        {
            var childCount = Children.Count;
            var childMaxMain = finalMain;
            for (var i = 0; i < childCount; i++)
            {
                var child = Children[i];
                var childConstraints = IsHorizontal
                    ? new LayoutConstraints(0, childMaxMain, _lastMeasureConstraints.MinHeight, _lastMeasureConstraints.MaxHeight)
                    : new LayoutConstraints(_lastMeasureConstraints.MinWidth, _lastMeasureConstraints.MaxWidth, 0, childMaxMain);
                child.Measure(childConstraints);
            }
        }

        BuildRuns(finalMain, spacing);

        var maxItemsInRun = 0;
        for (var i = 0; i < _runs.Count; i++)
        {
            maxItemsInRun = Math.Max(maxItemsInRun, _runs[i].Count);
        }

        var minsArr = Array.Empty<int>();
        var natsArr = Array.Empty<int>();
        var maxsArr = Array.Empty<int>();
        var growsArr = Array.Empty<int>();
        var shrinksArr = Array.Empty<int>();
        var resultsArr = Array.Empty<int>();

        if (maxItemsInRun > 0)
        {
            minsArr = ArrayPool<int>.Shared.Rent(maxItemsInRun);
            natsArr = ArrayPool<int>.Shared.Rent(maxItemsInRun);
            maxsArr = ArrayPool<int>.Shared.Rent(maxItemsInRun);
            growsArr = ArrayPool<int>.Shared.Rent(maxItemsInRun);
            shrinksArr = ArrayPool<int>.Shared.Rent(maxItemsInRun);
            resultsArr = ArrayPool<int>.Shared.Rent(maxItemsInRun);
        }

        try
        {
            if (IsHorizontal)
            {
                ArrangeHorizontal(finalRect, spacing, runSpacing, minsArr, natsArr, maxsArr, growsArr, shrinksArr, resultsArr);
            }
            else
            {
                ArrangeVertical(finalRect, spacing, runSpacing, minsArr, natsArr, maxsArr, growsArr, shrinksArr, resultsArr);
            }
        }
        finally
        {
            if (maxItemsInRun > 0)
            {
                ArrayPool<int>.Shared.Return(minsArr);
                ArrayPool<int>.Shared.Return(natsArr);
                ArrayPool<int>.Shared.Return(maxsArr);
                ArrayPool<int>.Shared.Return(growsArr);
                ArrayPool<int>.Shared.Return(shrinksArr);
                ArrayPool<int>.Shared.Return(resultsArr);
            }
        }
    }

    private void ArrangeHorizontal(in Rectangle finalRect, int spacing, int runSpacing, int[] minsArr, int[] natsArr, int[] maxsArr, int[] growsArr, int[] shrinksArr, int[] resultsArr)
    {
        var x0 = finalRect.X;
        var y = finalRect.Y;

        for (var runIndex = 0; runIndex < _runs.Count; runIndex++)
        {
            var run = _runs[runIndex];
            var count = run.Count;
            if (count <= 0)
            {
                continue;
            }

            var totalSpacing = spacing * Math.Max(0, count - 1);
            var available = Math.Max(0, finalRect.Width - totalSpacing);

            var mins = minsArr.AsSpan(0, count);
            var nats = natsArr.AsSpan(0, count);
            var maxs = maxsArr.AsSpan(0, count);
            var grows = growsArr.AsSpan(0, count);
            var shrinks = shrinksArr.AsSpan(0, count);
            var widths = resultsArr.AsSpan(0, count);

            for (var i = 0; i < count; i++)
            {
                var hints = Children[run.Start + i].MeasureHints;
                mins[i] = hints.Min.Width;
                nats[i] = hints.Natural.Width;
                maxs[i] = hints.Max.Width;
                grows[i] = hints.FlexGrowX;
                shrinks[i] = hints.FlexShrinkX;
            }

            FlexAllocator.Allocate(available, mins, nats, maxs, grows, shrinks, widths);

            var used = 0;
            for (var i = 0; i < count; i++)
            {
                used += widths[i];
            }

            var runCross = run.CrossNatural;
            var leftover = Math.Max(0, finalRect.Width - (used + totalSpacing));

            GetJustifyOffsets(leftover, count, spacing, Justify, out var offset, out var gap, out var gapExtra, out var gapExtraCount);

            var x = x0 + offset;
            for (var i = 0; i < count; i++)
            {
                var w = widths[i];
                Children[run.Start + i].Arrange(new Rectangle(x, y, w, runCross));
                x += w;

                if (i + 1 < count)
                {
                    x += gap;
                    if (gapExtraCount > 0 && i < gapExtraCount)
                    {
                        x += gapExtra;
                    }
                }
            }

            y += runCross;
            if (runIndex + 1 < _runs.Count)
            {
                y += runSpacing;
            }
        }
    }

    private void ArrangeVertical(in Rectangle finalRect, int spacing, int runSpacing, int[] minsArr, int[] natsArr, int[] maxsArr, int[] growsArr, int[] shrinksArr, int[] resultsArr)
    {
        var y0 = finalRect.Y;
        var x = finalRect.X;

        for (var runIndex = 0; runIndex < _runs.Count; runIndex++)
        {
            var run = _runs[runIndex];
            var count = run.Count;
            if (count <= 0)
            {
                continue;
            }

            var totalSpacing = spacing * Math.Max(0, count - 1);
            var available = Math.Max(0, finalRect.Height - totalSpacing);

            var mins = minsArr.AsSpan(0, count);
            var nats = natsArr.AsSpan(0, count);
            var maxs = maxsArr.AsSpan(0, count);
            var grows = growsArr.AsSpan(0, count);
            var shrinks = shrinksArr.AsSpan(0, count);
            var heights = resultsArr.AsSpan(0, count);

            for (var i = 0; i < count; i++)
            {
                var hints = Children[run.Start + i].MeasureHints;
                mins[i] = hints.Min.Height;
                nats[i] = hints.Natural.Height;
                maxs[i] = hints.Max.Height;
                grows[i] = hints.FlexGrowY;
                shrinks[i] = hints.FlexShrinkY;
            }

            FlexAllocator.Allocate(available, mins, nats, maxs, grows, shrinks, heights);

            var used = 0;
            for (var i = 0; i < count; i++)
            {
                used += heights[i];
            }

            var runCross = run.CrossNatural;
            var leftover = Math.Max(0, finalRect.Height - (used + totalSpacing));

            GetJustifyOffsets(leftover, count, spacing, Justify, out var offset, out var gap, out var gapExtra, out var gapExtraCount);

            var y = y0 + offset;
            for (var i = 0; i < count; i++)
            {
                var h = heights[i];
                Children[run.Start + i].Arrange(new Rectangle(x, y, runCross, h));
                y += h;

                if (i + 1 < count)
                {
                    y += gap;
                    if (gapExtraCount > 0 && i < gapExtraCount)
                    {
                        y += gapExtra;
                    }
                }
            }

            x += runCross;
            if (runIndex + 1 < _runs.Count)
            {
                x += runSpacing;
            }
        }
    }

    private void BuildRuns(int main, int spacing)
    {
        var version = Children.Version;
        var justify = Justify;
        var measureMode = MeasureMode;
        var runSpacing = Math.Max(0, RunSpacing);

        if (main == _lastRunMain
            && version == _lastChildrenVersion
            && spacing == _lastSpacing
            && runSpacing == _lastRunSpacing
            && justify == _lastJustify
            && measureMode == _lastMeasureMode)
        {
            return;
        }

        _lastRunMain = main;
        _lastChildrenVersion = version;
        _lastSpacing = spacing;
        _lastRunSpacing = runSpacing;
        _lastJustify = justify;
        _lastMeasureMode = measureMode;

        _runs.Clear();

        var childCount = Children.Count;
        if (childCount == 0)
        {
            return;
        }

        if (main <= 0)
        {
            // Everything overflows; each child becomes its own run to keep deterministic ordering.
            for (var i = 0; i < childCount; i++)
            {
                AddRun(i, 1, spacing);
            }
            return;
        }

        if (main == LayoutConstants.Infinite)
        {
            AddRun(0, childCount, spacing);
            return;
        }

        var start = 0;
        var count = 0;
        var runMain = 0;

        for (var i = 0; i < childCount; i++)
        {
            var child = Children[i];
            var childMain = IsHorizontal ? child.MeasureHints.Natural.Width : child.MeasureHints.Natural.Height;

            var next = count == 0 ? childMain : runMain + spacing + childMain;
            if (count > 0 && next > main)
            {
                AddRun(start, count, spacing);
                start = i;
                count = 0;
                runMain = 0;
            }

            if (count > 0)
            {
                runMain += spacing;
            }

            runMain += childMain;
            count++;
        }

        if (count > 0)
        {
            AddRun(start, count, spacing);
        }
    }

    private void AddRun(int start, int count, int spacing)
    {
        var mainNatural = 0;
        var mainMin = 0;

        var crossNatural = 0;
        var crossMin = 0;
        var maxCrossInfinite = false;

        for (var i = 0; i < count; i++)
        {
            var hints = Children[start + i].MeasureHints;

            if (IsHorizontal)
            {
                mainNatural += hints.Natural.Width;
                mainMin += hints.Min.Width;

                crossNatural = Math.Max(crossNatural, hints.Natural.Height);
                crossMin = Math.Max(crossMin, hints.Min.Height);
                maxCrossInfinite |= LayoutConstants.IsInfinite(hints.Max.Height);
            }
            else
            {
                mainNatural += hints.Natural.Height;
                mainMin += hints.Min.Height;

                crossNatural = Math.Max(crossNatural, hints.Natural.Width);
                crossMin = Math.Max(crossMin, hints.Min.Width);
                maxCrossInfinite |= LayoutConstants.IsInfinite(hints.Max.Width);
            }
        }

        mainNatural += spacing * Math.Max(0, count - 1);
        mainMin += spacing * Math.Max(0, count - 1);

        _runs.Add(new Run(start, count, mainNatural, mainMin, crossNatural, crossMin, maxCrossInfinite));
    }

    private static void GetJustifyOffsets(int leftover, int itemCount, int spacing, WrapJustify justify, out int offset, out int gap, out int gapExtra, out int gapExtraCount)
    {
        offset = 0;
        gap = spacing;
        gapExtra = 0;
        gapExtraCount = 0;

        if (leftover <= 0 || itemCount <= 0)
        {
            return;
        }

        switch (justify)
        {
            case WrapJustify.Start:
                return;

            case WrapJustify.Center:
                offset = leftover / 2;
                return;

            case WrapJustify.End:
                offset = leftover;
                return;

            case WrapJustify.SpaceBetween:
                if (itemCount <= 1)
                {
                    return;
                }
                {
                    var gaps = itemCount - 1;
                    var extra = leftover / gaps;
                    var rem = leftover % gaps;
                    gap = spacing + extra;
                    gapExtra = 1;
                    gapExtraCount = rem;
                    return;
                }

            case WrapJustify.SpaceEvenly:
                {
                    var gaps = itemCount + 1;
                    var extra = leftover / gaps;
                    var rem = leftover % gaps;
                    gap = spacing + extra;
                    offset = gap;
                    if (rem > 0)
                    {
                        // Give the first gaps an extra cell; apply it to the leading offset first, then between items.
                        offset += 1;
                        rem--;
                        gapExtra = 1;
                        gapExtraCount = Math.Min(rem, Math.Max(0, itemCount - 1));
                    }
                    return;
                }

            case WrapJustify.SpaceAround:
                {
                    var extra = leftover / itemCount;
                    var rem = leftover % itemCount;
                    gap = spacing + extra;
                    offset = gap / 2;
                    // Distribute remainder to the first (between-item) gaps. This keeps a stable result without extra allocations.
                    gapExtra = 1;
                    gapExtraCount = Math.Min(rem, Math.Max(0, itemCount - 1));
                    return;
                }

            default:
                return;
        }
    }

    private readonly record struct Run(int Start, int Count, int MainNatural, int MainMin, int CrossNatural, int CrossMin, bool MaxCrossInfinite);
}
