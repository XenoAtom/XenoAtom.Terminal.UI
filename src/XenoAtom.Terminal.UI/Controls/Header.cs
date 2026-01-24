// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Text;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Layout;
using XenoAtom.Terminal.UI.Rendering;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.Controls;

/// <summary>
/// Represents a header bar with left, center, and right slots.
/// </summary>
public sealed partial class Header : Visual
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Header"/> class.
    /// </summary>
    public Header()
    {
        HorizontalAlignment = Align.Stretch;
    }

    /// <summary>
    /// Gets or sets the left-aligned content.
    /// </summary>
    [Bindable]
    public partial Visual? Left { get; set; }

    /// <summary>
    /// Gets or sets the centered content.
    /// </summary>
    [Bindable]
    public partial Visual? Center { get; set; }

    /// <summary>
    /// Gets or sets the right-aligned content.
    /// </summary>
    [Bindable]
    public partial Visual? Right { get; set; }

    /// <inheritdoc />
    protected override int ChildrenCount
        => (_left is null ? 0 : 1) + (_center is null ? 0 : 1) + (_right is null ? 0 : 1);

    /// <inheritdoc />
    protected override Visual GetChild(int index)
    {
        if (_left is not null)
        {
            if (index == 0) return _left;
            index--;
        }

        if (_center is not null)
        {
            if (index == 0) return _center;
            index--;
        }

        if (_right is not null)
        {
            if (index == 0) return _right;
        }

        throw new ArgumentOutOfRangeException(nameof(index));
    }

    /// <inheritdoc />
    protected override SizeHints MeasureCore(in LayoutConstraints constraints)
    {
        var labelConstraints = new LayoutConstraints(0, LayoutConstants.Infinite, 0, 1);
        _left?.Measure(labelConstraints);
        _center?.Measure(labelConstraints);
        _right?.Measure(labelConstraints);

        var leftW = _left?.DesiredSize.Width ?? 0;
        var centerW = _center?.DesiredSize.Width ?? 0;
        var rightW = _right?.DesiredSize.Width ?? 0;

        var requiredWidth = leftW + rightW;
        if (_center is not null)
        {
            requiredWidth = Math.Max(requiredWidth, leftW + centerW + rightW);
        }

        var natural = constraints.Clamp(new Size(requiredWidth, 1));
        return SizeHints.FlexX(min: new Size(0, 1), natural: natural, growX: 1, shrinkX: 1);
    }

    /// <inheritdoc />
    protected override void ArrangeCore(in Rectangle finalRect)
    {
        Bounds = finalRect;

        var left = _left;
        var center = _center;
        var right = _right;

        var leftW = left is null ? 0 : Math.Min(finalRect.Width, left.DesiredSize.Width);
        var rightW = right is null ? 0 : Math.Min(finalRect.Width, right.DesiredSize.Width);

        if (left is not null)
        {
            left.Arrange(new Rectangle(finalRect.X, finalRect.Y, leftW, 1));
        }

        if (right is not null)
        {
            var x = finalRect.X + Math.Max(0, finalRect.Width - rightW);
            right.Arrange(new Rectangle(x, finalRect.Y, rightW, 1));
        }

        if (center is not null)
        {
            var maxCenterWidth = Math.Max(0, finalRect.Width - leftW - rightW);
            var w = Math.Min(maxCenterWidth, center.DesiredSize.Width);
            var x = finalRect.X + leftW + Math.Max(0, (maxCenterWidth - w) / 2);
            center.Arrange(new Rectangle(x, finalRect.Y, w, 1));
        }
    }

    /// <inheritdoc />
    protected override void RenderOverride(CellBuffer buffer)
    {
        var rect = Bounds;
        if (rect.Width <= 0 || rect.Height <= 0)
        {
            return;
        }

        var theme = GetTheme();
        var style = GetStyle<HeaderStyle>().Resolve(theme);

        for (var x = rect.X; x < rect.X + rect.Width; x++)
        {
            buffer.SetCell(x, rect.Y, new Rune(' '), style);
        }
    }
}
