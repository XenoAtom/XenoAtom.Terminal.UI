// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.


// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Layout;

namespace XenoAtom.Terminal.UI.Controls;

/// <summary>
/// Represents a visual whose content is produced by a dynamic builder function.
/// </summary>
public sealed partial class ComputedVisual : Visual
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ComputedVisual"/> class.
    /// </summary>
    /// <param name="build">The function that builds the child visual.</param>
    public ComputedVisual(Func<Visual?> build)
    {
        this.Child(build);
        // We should not use Child property directly here to avoid capturing the Func in the lambda
        // If the child is changed, the initializer will still be called
        this.HorizontalAlignment(() => _child?.HorizontalAlignment ?? HorizontalAlignment.Stretch);
        this.VerticalAlignment(() => _child?.VerticalAlignment ?? VerticalAlignment.Stretch);
    }

    /// <summary>
    /// Gets or sets the computed child visual.
    /// </summary>
    [Bindable]
    public partial Visual? Child { get; set; }

    /// <inheritdoc />
    protected override SizeHints MeasureCore(in LayoutConstraints constraints)
    {
        var child = Child;
        if (child is null)
        {
            return SizeHints.Fixed(default);
        }

        return child.Measure(constraints);
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
    protected override int ChildrenCount => _child is null ? 0 : 1;

    /// <inheritdoc />
    protected override Visual GetChild(int index)
    {
        if (index == 0 && _child is not null)
        {
            return _child;
        }

        throw new ArgumentOutOfRangeException(nameof(index));
    }
}
