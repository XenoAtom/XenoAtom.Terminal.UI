// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Text;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Rendering;

namespace XenoAtom.Terminal.UI.Controls;

public sealed partial class Backdrop : Visuals.Visual
{
    protected override CellSize MeasureOverride(CellSize availableSize) => availableSize;

    protected override void ArrangeOverride(CellRect finalRect) => Bounds = finalRect;

    protected override void RenderOverride(CellBuffer buffer)
    {
        var rect = Bounds;
        if (rect.Width <= 0 || rect.Height <= 0)
        {
            return;
        }

        var theme = GetTheme();
        var style = CellStyle.Dim;
        if (theme.Disabled is { } c)
        {
            style = style.WithBackground(c);
        }

        for (var y = rect.Y; y < rect.Y + rect.Height; y++)
        {
            for (var x = rect.X; x < rect.X + rect.Width; x++)
            {
                buffer.SetCell(x, y, new Rune(' '), style);
            }
        }
    }
}
