// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Input;
using XenoAtom.Terminal.UI.Layout;

namespace XenoAtom.Terminal.UI.Controls;

/// <summary>
/// A vertical stack of <see cref="Collapsible"/> controls with optional "single expanded item" behavior.
/// </summary>
public sealed partial class Accordion : Panel
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Accordion"/> class.
    /// </summary>
    public Accordion()
    {
        this.SingleExpanded = true;
        AddHandler(Collapsible.ExpandedChangedEvent, OnChildExpandedChanged);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Accordion"/> class with children.
    /// </summary>
    /// <param name="children">The collapsible children.</param>
    public Accordion(params Visual[] children) : this()
    {
        AddRange(children);
    }

    /// <summary>
    /// Gets or sets the spacing (in rows) between items.
    /// </summary>
    [Bindable]
    public partial int Spacing { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether only one item can be expanded at a time.
    /// </summary>
    [Bindable]
    public partial bool SingleExpanded { get; set; }

    /// <inheritdoc/>
    protected override SizeHints MeasureCore(in LayoutConstraints constraints)
    {
        var spacing = Math.Max(0, Spacing);
        var childCount = Children.Count;
        if (childCount == 0)
        {
            return SizeHints.Fixed(Size.Zero);
        }

        int totalSpacing = spacing * Math.Max(0, childCount - 1);
        var childConstraints = new LayoutConstraints(constraints.MinWidth, constraints.MaxWidth, 0, constraints.MaxHeight);

        var minW = 0;
        var natW = 0;
        var maxW = 0;
        var maxWInf = false;

        var minH = totalSpacing;
        var natH = totalSpacing;
        var maxH = totalSpacing;

        var growY = 0;
        var shrinkY = 0;

        for (var i = 0; i < childCount; i++)
        {
            var child = Children[i];
            var hints = child.Measure(childConstraints);

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
                maxH = maxH + hints.Max.Height;
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

    /// <inheritdoc/>
    protected override void ArrangeCore(in Rectangle finalRect)
    {
        Bounds = finalRect;

        var y = finalRect.Y;
        var spacing = Math.Max(0, Spacing);
        var childCount = Children.Count;
        if (childCount == 0)
        {
            return;
        }

        int totalSpacing = spacing * Math.Max(0, childCount - 1);
        var available = Math.Max(0, finalRect.Height - totalSpacing);

        var mins = new int[childCount];
        var nats = new int[childCount];
        var maxs = new int[childCount];
        var grows = new int[childCount];
        var shrinks = new int[childCount];
        var heights = new int[childCount];

        for (var i = 0; i < childCount; i++)
        {
            var hints = Children[i].MeasureHints;
            mins[i] = hints.Min.Height;
            nats[i] = hints.Natural.Height;
            maxs[i] = hints.Max.Height;
            grows[i] = hints.FlexGrowY;
            shrinks[i] = hints.FlexShrinkY;
        }

        FlexAllocator.Allocate(available, mins, nats, maxs, grows, shrinks, heights);

        for (var i = 0; i < childCount; i++)
        {
            var h = heights[i];
            Children[i].Arrange(new Rectangle(finalRect.X, y, finalRect.Width, h));
            y += h + spacing;
        }
    }

    private void OnChildExpandedChanged(object? sender, ExpandedChangedEventArgs e)
    {
        _ = sender;
        if (!SingleExpanded || !e.NewValue)
        {
            return;
        }

        if (e.OriginalSource is not Collapsible expanded)
        {
            return;
        }

        for (var i = 0; i < Children.Count; i++)
        {
            if (Children[i] is Collapsible other && !ReferenceEquals(other, expanded))
            {
                other.IsExpanded = false;
            }
        }
    }
}
