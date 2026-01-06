// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Text;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Rendering;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.Controls;

public sealed partial class StatusBar : Visuals.Visual
{
    [Bindable]
    public partial string? LeftText { get; set; }

    [Bindable]
    public partial string? RightText { get; set; }

    protected override CellSize MeasureOverride(CellSize availableSize)
    {
        return new CellSize(availableSize.Width, 1);
    }

    protected override void ArrangeOverride(CellRect finalRect)
    {
        Bounds = finalRect;
    }

    protected override void RenderOverride(CellBuffer buffer)
    {
        var rect = Bounds;
        if (rect.Width <= 0 || rect.Height <= 0)
        {
            return;
        }

        var theme = GetTheme();
        var statusBarStyle = GetEnvironmentValue(StatusBarStyle.Key);
        var style = statusBarStyle.Resolve(theme);

        for (var x = rect.X; x < rect.X + rect.Width; x++)
        {
            buffer.SetCell(x, rect.Y, new Rune(' '), style);
        }

        var left = LeftText ?? string.Empty;
        var right = RightText ?? string.Empty;

        buffer.WriteText(rect.X, rect.Y, left.AsSpan(), style);

        var rightWidth = TerminalTextUtility.GetWidth(right.AsSpan());
        var rightX = rect.X + Math.Max(0, rect.Width - rightWidth);
        buffer.WriteText(rightX, rect.Y, right.AsSpan(), style);
    }
}
