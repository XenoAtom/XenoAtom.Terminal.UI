// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Buffers;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Layout;

namespace XenoAtom.Terminal.UI.Controls;

/// <summary>
/// Arranges children vertically in a stack.
/// </summary>
public sealed partial class VStack : Panel
{
    /// <summary>
    /// Initializes a new instance of the <see cref="VStack"/> class.
    /// </summary>
    public VStack()
    {
        HorizontalAlignment = Align.Stretch;
        VerticalAlignment = Align.Start;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="VStack"/> class with children.
    /// </summary>
    /// <param name="children">The child visuals.</param>
    public VStack(params Visual[] children)
    {
        HorizontalAlignment = Align.Stretch;
        VerticalAlignment = Align.Start;
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

        var childConstraints = new LayoutConstraints(constraints.MinWidth, constraints.MaxWidth, 0, constraints.MaxHeight);
        var visibleCount = 0;

        var minW = 0;
        var natW = 0;
        var maxW = 0;
        var maxWInf = false;

        var minH = 0;
        var natH = 0;
        var maxH = 0;

        var growY = 0;
        var shrinkY = 0;

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
                minH += spacing;
                natH += spacing;
                if (maxH != LayoutConstants.Infinite)
                {
                    maxH += spacing;
                }
            }

            visibleCount++;

            minW = Math.Max(minW, hints.Min.Width);
            natW = Math.Max(natW, hints.Natural.Width);
            if (LayoutConstants.IsInfinite(hints.Max.Width))
            {
                maxWInf = true;
            }
            else if (!maxWInf)
            {
                maxW = Math.Max(maxW, hints.Max.Width);
            }

            minH += hints.Min.Height;
            natH += hints.Natural.Height;

            if (LayoutConstants.IsInfinite(hints.Max.Height))
            {
                maxH = LayoutConstants.Infinite;
            }
            else if (maxH != LayoutConstants.Infinite)
            {
                maxH += hints.Max.Height;
            }

            growY += hints.FlexGrowY;
            shrinkY += hints.FlexShrinkY;
        }

        var maxSize = new Size(
            maxWInf ? LayoutConstants.Infinite : LayoutConstants.ClampFinite(maxW),
            maxH == LayoutConstants.Infinite ? LayoutConstants.Infinite : LayoutConstants.ClampFinite(maxH));

        return SizeHints.Flex(
            new Size(LayoutConstants.ClampFinite(minW), LayoutConstants.ClampFinite(minH)),
            new Size(LayoutConstants.ClampFinite(natW), LayoutConstants.ClampFinite(natH)),
            maxSize,
            0,
            growY,
            0,
            shrinkY).Normalize();
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
        var available = Math.Max(0, finalRect.Height - totalSpacing);

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
        var heights = scratch.Slice(5 * childCount, childCount);

        try
        {
            for (var i = 0; i < childCount; i++)
            {
                var hints = Children[i].MeasureHints;
                if (Children[i].IsVisible)
                {
                    mins[i] = hints.Min.Height;
                    nats[i] = hints.Natural.Height;
                    maxs[i] = hints.Max.Height;
                    grows[i] = hints.FlexGrowY;
                    shrinks[i] = hints.FlexShrinkY;
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

            FlexAllocator.Allocate(available, mins, nats, maxs, grows, shrinks, heights);

            var y = finalRect.Y;
            for (var i = 0; i < childCount; i++)
            {
                var h = heights[i];
                Children[i].Arrange(new Rectangle(finalRect.X, y, finalRect.Width, h));
                if (Children[i].IsVisible)
                {
                    y += h + spacing;
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
