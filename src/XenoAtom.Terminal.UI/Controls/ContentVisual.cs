// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Layout;

namespace XenoAtom.Terminal.UI.Controls;

/// <summary>
/// Base class for controls that expose a single visual content.
/// </summary>
public abstract partial class ContentVisual : Visual
{
    /// <summary>
    /// Gets or sets the single content visual.
    /// </summary>
    [Bindable]
    public partial Visual? Content { get; set; }

    /// <inheritdoc />
    protected override int ChildrenCount => _content is null ? 0 : 1;

    /// <inheritdoc />
    protected override Visual GetChild(int index)
        => index == 0 && _content is not null ? _content : throw new ArgumentOutOfRangeException(nameof(index));

    /// <inheritdoc />
    protected override SizeHints MeasureCore(in LayoutConstraints constraints)
    {
        var content = Content;
        return content is null ? SizeHints.Fixed(Size.Zero) : content.Measure(constraints);
    }

    /// <inheritdoc />
    protected override void ArrangeCore(in Rectangle finalRect)
    {
        Bounds = finalRect;
        Content?.Arrange(finalRect);
    }
}
