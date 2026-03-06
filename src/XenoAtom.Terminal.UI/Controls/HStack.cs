// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Buffers;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Layout;

namespace XenoAtom.Terminal.UI.Controls;

/// <summary>
/// Arranges children horizontally in a stack.
/// </summary>
public sealed partial class HStack : Panel
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HStack"/> class.
    /// </summary>
    public HStack()
    {
        HorizontalAlignment = Align.Start;
        VerticalAlignment = Align.Stretch;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="HStack"/> class with children.
    /// </summary>
    /// <param name="children">The child visuals.</param>
    public HStack(params Visual[] children)
    {
        HorizontalAlignment = Align.Start;
        VerticalAlignment = Align.Stretch;
        AddRange(children);
    }

    /// <summary>
    /// Gets or sets the spacing between children.
    /// </summary>
    [Bindable]
    public partial int Spacing { get; set; }

    /// <inheritdoc />
    protected override SizeHints MeasureCore(in LayoutConstraints constraints)
    {
        var spacing = Math.Max(0, Spacing);
        var childCount = Children.Count;
        if (childCount == 0)
        {
            return SizeHints.Fixed(Size.Zero);
        }

        var childConstraints = new LayoutConstraints(0, constraints.MaxWidth, constraints.MinHeight, constraints.MaxHeight);
        var visibleCount = 0;

        var minW = 0;
        var natW = 0;
        var maxW = 0;

        var minH = 0;
        var natH = 0;
        var maxH = 0;
        var maxHInf = false;

        var growX = 0;
        var shrinkX = 0;

        for (var i = 0; i < childCount; i++)
        {
            var child = Children[i];
            var hints = child.Measure(childConstraints);
            if (!child.IsVisible)
            {
                continue;
            }

            if (visibleCount > 0)
            {
                minW += spacing;
                natW += spacing;
                if (maxW != LayoutConstants.Infinite)
                {
                    maxW += spacing;
                }
            }

            visibleCount++;

            minW += hints.Min.Width;
            natW += hints.Natural.Width;

            if (LayoutConstants.IsInfinite(hints.Max.Width))
            {
                maxW = LayoutConstants.Infinite;
            }
            else if (maxW != LayoutConstants.Infinite)
            {
                maxW += hints.Max.Width;
            }

            minH = Math.Max(minH, hints.Min.Height);
            natH = Math.Max(natH, hints.Natural.Height);
            if (LayoutConstants.IsInfinite(hints.Max.Height))
            {
                maxHInf = true;
            }
            else if (!maxHInf)
            {
                maxH = Math.Max(maxH, hints.Max.Height);
            }

            growX += hints.FlexGrowX;
            shrinkX += hints.FlexShrinkX;
        }

        var maxSize = new Size(
            maxW == LayoutConstants.Infinite ? LayoutConstants.Infinite : LayoutConstants.ClampFinite(maxW),
            maxHInf ? LayoutConstants.Infinite : LayoutConstants.ClampFinite(maxH));

        return SizeHints.Flex(
            new Size(LayoutConstants.ClampFinite(minW), LayoutConstants.ClampFinite(minH)),
            new Size(LayoutConstants.ClampFinite(natW), LayoutConstants.ClampFinite(natH)),
            maxSize,
            growX,
            0,
            shrinkX,
            0).Normalize();
    }

    /// <inheritdoc />
    protected override void ArrangeCore(in Rectangle finalRect)
    {
        var spacing = Math.Max(0, Spacing);
        var childCount = Children.Count;
        if (childCount == 0)
        {
            return;
        }

        var visibleCount = 0;
        for (var i = 0; i < childCount; i++)
        {
            if (Children[i].IsVisible)
            {
                visibleCount++;
            }
        }

        var totalSpacing = spacing * Math.Max(0, visibleCount - 1);
        var available = Math.Max(0, finalRect.Width - totalSpacing);

        var scratchLength = childCount * 6;
        int[]? rentedScratch = null;
        var scratch = scratchLength <= 6 * 128
            ? stackalloc int[scratchLength]
            : (rentedScratch = ArrayPool<int>.Shared.Rent(scratchLength));

        var mins = scratch[..childCount];
        var nats = scratch.Slice(childCount, childCount);
        var maxs = scratch.Slice(2 * childCount, childCount);
        var grows = scratch.Slice(3 * childCount, childCount);
        var shrinks = scratch.Slice(4 * childCount, childCount);
        var widths = scratch.Slice(5 * childCount, childCount);

        try
        {
            for (var i = 0; i < childCount; i++)
            {
                var hints = Children[i].MeasureHints;
                if (Children[i].IsVisible)
                {
                    mins[i] = hints.Min.Width;
                    nats[i] = hints.Natural.Width;
                    maxs[i] = hints.Max.Width;
                    grows[i] = hints.FlexGrowX;
                    shrinks[i] = hints.FlexShrinkX;
                }
                else
                {
                    mins[i] = 0;
                    nats[i] = 0;
                    maxs[i] = 0;
                    grows[i] = 0;
                    shrinks[i] = 0;
                }
            }

            FlexAllocator.Allocate(available, mins, nats, maxs, grows, shrinks, widths);

            var x = finalRect.X;
            for (var i = 0; i < childCount; i++)
            {
                var w = widths[i];
                Children[i].Arrange(new Rectangle(x, finalRect.Y, w, finalRect.Height));
                if (Children[i].IsVisible)
                {
                    x += w + spacing;
                }
            }
        }
        finally
        {
            if (rentedScratch is not null)
            {
                ArrayPool<int>.Shared.Return(rentedScratch);
            }
        }
    }
}
