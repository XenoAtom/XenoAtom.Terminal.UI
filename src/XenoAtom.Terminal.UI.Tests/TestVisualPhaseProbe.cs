// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Layout;
using XenoAtom.Terminal.UI.Rendering;

namespace XenoAtom.Terminal.UI.Tests;

internal sealed class TestVisualPhaseProbe : Visual
{
    public int PrepareReadCount { get; private set; }

    public int MeasureReadCount { get; private set; }

    public int ArrangeReadCount { get; private set; }

    public int RenderReadCount { get; private set; }

    protected override void PrepareChildren()
    {
        _ = MinWidth;
        PrepareReadCount++;
    }

    protected override SizeHints MeasureCore(in LayoutConstraints constraints)
    {
        _ = constraints;
        _ = MinWidth;
        MeasureReadCount++;
        return SizeHints.Fixed(new Size(1, 1));
    }

    protected override void ArrangeCore(in Rectangle finalRect)
    {
        _ = finalRect;
        _ = MinWidth;
        ArrangeReadCount++;
    }

    protected override void RenderOverride(CellBuffer buffer)
    {
        _ = buffer;
        _ = MinWidth;
        RenderReadCount++;
    }
}
