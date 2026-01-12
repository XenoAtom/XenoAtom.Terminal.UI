// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.


// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Layout;

namespace XenoAtom.Terminal.UI.Controls;

public sealed partial class ComputedVisual : Visual
{
    public ComputedVisual(Func<Visual?> build)
    {
        this.Child(build);
        // We should not use Child property directly here to avoid capturing the Func in the lambda
        // If the child is changed, the initializer will still be called
        this.HorizontalAlignment(() => _child?.HorizontalAlignment ?? HorizontalAlignment.Stretch);
        this.VerticalAlignment(() => _child?.VerticalAlignment ?? VerticalAlignment.Stretch);
    }

    [Bindable]
    public partial Visual? Child { get; set; }

    protected override SizeHints MeasureCore(in LayoutConstraints constraints)
    {
        var child = Child;
        if (child is null)
        {
            return SizeHints.Fixed(default);
        }

        return child.Measure(constraints);
    }

    protected override void ArrangeCore(in Rectangle finalRect)
    {
        var child = Child;
        if (child is null)
        {
            // When there is no child, don't participate in hit-testing or rendering.
            Bounds = default;
            return;
        }

        child.Arrange(finalRect);
    }

    protected override int ChildrenCount => _child is null ? 0 : 1;

    protected override Visual GetChild(int index)
    {
        if (index == 0 && _child is not null)
        {
            return _child;
        }

        throw new ArgumentOutOfRangeException(nameof(index));
    }
}
