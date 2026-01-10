// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Input;

namespace XenoAtom.Terminal.UI.Controls;

/// <summary>
/// A vertical stack of <see cref="Collapsible"/> controls with optional "single expanded item" behavior.
/// </summary>
public sealed partial class Accordion : Panel
{
    public Accordion()
    {
        this.SingleExpanded = true;
        AddHandler(Collapsible.ExpandedChangedEvent, OnChildExpandedChanged);
    }

    public Accordion(params Visual[] children) : this()
    {
        AddRange(children);
    }

    [Bindable]
    public partial int Spacing { get; set; }

    [Bindable]
    public partial bool SingleExpanded { get; set; }

    protected override Size MeasureOverride(Size availableSize)
    {
        var width = 0;
        var height = 0;
        var spacing = Math.Max(0, Spacing);

        for (var i = 0; i < Children.Count; i++)
        {
            var child = Children[i];
            child.Measure(availableSize);
            width = Math.Max(width, child.DesiredSize.Width);
            height += child.DesiredSize.Height;
            if (i + 1 < Children.Count)
            {
                height += spacing;
            }
        }

        return new Size(Math.Min(availableSize.Width, width), height);
    }

    protected override void ArrangeOverride(Rectangle finalRect)
    {
        Bounds = finalRect;

        var y = finalRect.Y;
        var spacing = Math.Max(0, Spacing);

        foreach (var child in Children)
        {
            var h = child.DesiredSize.Height;
            child.Arrange(new Rectangle(finalRect.X, y, finalRect.Width, h));
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
