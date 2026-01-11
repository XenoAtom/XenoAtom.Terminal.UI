// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Text;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Layout;
using XenoAtom.Terminal.UI.Rendering;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.Controls;

public sealed partial class StatusBar : Visual
{
    public StatusBar()
    {
        HorizontalAlignment = HorizontalAlignment.Stretch;
    }

    [Bindable]
    public partial Visual? LeftText { get; set; }

    [Bindable]
    public partial Visual? RightText { get; set; }

    protected override int ChildrenCount
        => (_leftText is null ? 0 : 1) + (_rightText is null ? 0 : 1);

    protected override Visual GetChild(int index)
    {
        if (_leftText is not null)
        {
            if (index == 0)
            {
                return _leftText;
            }
            index--;
        }

        if (_rightText is not null)
        {
            if (index == 0)
            {
                return _rightText;
            }
        }

        throw new ArgumentOutOfRangeException(nameof(index));
    }

    protected override SizeHints MeasureCore(in LayoutConstraints constraints)
    {
        var labelConstraints = new LayoutConstraints(0, LayoutConstants.Infinite, 0, 1);
        _leftText?.Measure(labelConstraints);
        _rightText?.Measure(labelConstraints);

        var requiredWidth = (_leftText?.DesiredSize.Width ?? 0) + (_rightText?.DesiredSize.Width ?? 0);
        var natural = constraints.Clamp(new Size(requiredWidth, 1));
        return SizeHints.FlexX(min: new Size(0, 1), natural: natural, growX: 1, shrinkX: 1);
    }

    protected override void ArrangeCore(in Rectangle finalRect)
    {
        if (_leftText is not null)
        {
            var w = Math.Min(finalRect.Width, _leftText.DesiredSize.Width);
            _leftText.Arrange(new Rectangle(finalRect.X, finalRect.Y, w, 1));
        }

        if (_rightText is not null)
        {
            var w = Math.Min(finalRect.Width, _rightText.DesiredSize.Width);
            var x = finalRect.X + Math.Max(0, finalRect.Width - w);
            _rightText.Arrange(new Rectangle(x, finalRect.Y, w, 1));
        }
    }

    protected override void RenderOverride(CellBuffer buffer)
    {
        var rect = Bounds;
        if (rect.Width <= 0 || rect.Height <= 0)
        {
            return;
        }

        var theme = GetTheme();
        var statusBarStyle = Get<StatusBarStyle>();
        var style = statusBarStyle.Resolve(theme);

        for (var x = rect.X; x < rect.X + rect.Width; x++)
        {
            buffer.SetCell(x, rect.Y, new Rune(' '), style);
        }

        // Children render on top (inheriting the status bar style).
    }
}
