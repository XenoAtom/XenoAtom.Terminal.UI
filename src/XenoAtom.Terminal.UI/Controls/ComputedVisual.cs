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
    private Visual? _currentChild;

    /// <summary>
    /// Initializes a new instance of the <see cref="ComputedVisual"/> class.
    /// </summary>
    /// <param name="build">The function that builds the child visual.</param>
    public ComputedVisual(Func<Visual?> build)
    {
        DynamicVisual = build;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ComputedVisual"/> class.
    /// </summary>
    /// <param name="state">The state that provides the child visual.</param>
    public ComputedVisual(State<Visual?> state)
    {
        ArgumentNullException.ThrowIfNull(state);
        DynamicVisual = (Func<Visual?>)(() => state.Value);
    }

    /// <summary>
    /// Gets or sets the dynamic builder used to produce the child visual.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This property is evaluated during <see cref="Visual.PrepareChildren"/>. Property reads performed while building
    /// the visual are tracked, so changes automatically rebuild the computed content.
    /// </para>
    /// <para>
    /// Use <see cref="Child"/> to provide a static fallback when no dynamic builder is configured.
    /// </para>
    /// </remarks>
    [Bindable]
    public partial Delegator<Func<Visual?>> DynamicVisual { get; set; }

    /// <summary>
    /// Gets or sets the (optional) static child visual.
    /// </summary>
    /// <remarks>
    /// When <see cref="DynamicVisual"/> is set, it takes precedence over this value.
    /// </remarks>
    [Bindable(NoVisualAttach = true)]
    public partial Visual? Child { get; set; }

    /// <inheritdoc />
    protected override void PrepareChildren()
    {
        var builder = DynamicVisual.Invoke;
        var desired = builder is not null ? builder() : Child;

        if (ReferenceEquals(_currentChild, desired))
        {
            return;
        }

        if (_currentChild is not null)
        {
            DetachChild(_currentChild);
            _currentChild = null;
        }

        if (desired is null)
        {
            // When no visual is produced, this control should behave like it doesn't exist.
            // In particular, it must not block hit-testing of visuals behind it (common for overlay scenarios).
            IsHitTestVisible = false;

            // Bind wrapper layout/visibility properties to the computed child so the computed visual behaves like the child
            // in its container (alignment, min/max, margin, etc.).
            this.BindHorizontalAlignment(default);
            this.BindVerticalAlignment(default);
            this.BindMinWidth(default);
            this.BindMinHeight(default);
            this.BindMaxWidth(default);
            this.BindMaxHeight(default);
            this.BindMargin(default);
            this.BindIsVisible(default);
            this.BindIsEnabled(default);
            return;
        }

        IsHitTestVisible = true;
        _currentChild = desired;
        AttachChild(desired);

        // Bind wrapper layout/visibility properties to the computed child so the computed visual behaves like the child
        // in its container (alignment, min/max, margin, etc.).
        this.BindHorizontalAlignment(desired.Bind.HorizontalAlignment);
        this.BindVerticalAlignment(desired.Bind.VerticalAlignment);
        this.BindMinWidth(desired.Bind.MinWidth);
        this.BindMinHeight(desired.Bind.MinHeight);
        this.BindMaxWidth(desired.Bind.MaxWidth);
        this.BindMaxHeight(desired.Bind.MaxHeight);
        this.BindMargin(desired.Bind.Margin);
        this.BindIsVisible(desired.Bind.IsVisible);
        this.BindIsEnabled(desired.Bind.IsEnabled);
    }

    /// <inheritdoc />
    protected override SizeHints MeasureCore(in LayoutConstraints constraints)
    {
        var child = _currentChild;
        if (child is null)
        {
            return SizeHints.Fixed(default);
        }

        return child.Measure(constraints);
    }

    /// <inheritdoc />
    protected override void ArrangeCore(in Rectangle finalRect)
    {
        _currentChild?.Arrange(finalRect);
    }

    /// <inheritdoc />
    protected override int ChildrenCount => _currentChild is null ? 0 : 1;

    /// <inheritdoc />
    protected override Visual GetChild(int index)
    {
        if (index == 0 && _currentChild is not null)
        {
            return _currentChild;
        }

        throw new ArgumentOutOfRangeException(nameof(index));
    }
}
