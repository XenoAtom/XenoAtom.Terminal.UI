// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

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
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="HStack"/> class with children.
    /// </summary>
    /// <param name="children">The child visuals.</param>
    public HStack(params Visual[] children)
    {
        HorizontalAlignment = Align.Start;
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

        var totalSpacing = spacing * Math.Max(0, childCount - 1);
        var childConstraints = new LayoutConstraints(0, constraints.MaxWidth, constraints.MinHeight, constraints.MaxHeight);

        var minW = totalSpacing;
        var natW = totalSpacing;
        var maxW = totalSpacing;

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

        var totalSpacing = spacing * Math.Max(0, childCount - 1);
        var available = Math.Max(0, finalRect.Width - totalSpacing);

        var mins = new int[childCount];
        var nats = new int[childCount];
        var maxs = new int[childCount];
        var grows = new int[childCount];
        var shrinks = new int[childCount];
        var widths = new int[childCount];

        for (var i = 0; i < childCount; i++)
        {
            var hints = Children[i].MeasureHints;
            mins[i] = hints.Min.Width;
            nats[i] = hints.Natural.Width;
            maxs[i] = hints.Max.Width;
            grows[i] = hints.FlexGrowX;
            shrinks[i] = hints.FlexShrinkX;
        }

        FlexAllocator.Allocate(available, mins, nats, maxs, grows, shrinks, widths);

        var x = finalRect.X;
        for (var i = 0; i < childCount; i++)
        {
            var w = widths[i];
            Children[i].Arrange(new Rectangle(x, finalRect.Y, w, finalRect.Height));
            x += w + spacing;
        }
    }
}
