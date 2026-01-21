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
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ComputedVisual"/> class.
    /// </summary>
    /// <param name="state">The function that builds the child visual.</param>
    public ComputedVisual(State<Visual?> state)
    {
        this.Child(state);
    }

    /// <summary>
    /// Gets or sets the computed child visual.
    /// </summary>
    [Bindable]
    public partial Visual? Child { get; set; }

    partial void OnChildChanged(Visual? value)
    {
        if (value is null) return;

        // We make sure that all visual properties are bound to the child.
        this.BindHorizontalAlignment(value.Bind.HorizontalAlignment);
        this.BindVerticalAlignment(value.Bind.VerticalAlignment);
        this.BindMinWidth(value.Bind.MinWidth);
        this.BindMinHeight(value.Bind.MinHeight);
        this.BindMaxWidth(value.Bind.MaxWidth);
        this.BindMaxHeight(value.Bind.MaxHeight);
        this.BindMargin(value.Bind.Margin);
        this.BindIsVisible(value.Bind.IsVisible);
        this.BindIsEnabled(value.Bind.IsEnabled);
    }

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
