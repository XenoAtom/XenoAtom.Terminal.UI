// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Layout;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class HStackMeasureTests
{
    [TestMethod]
    public void HStack_Distributes_Width_To_Stretch_Children_During_Arrange()
    {
        var a = new ProbeVisual { HorizontalAlignment = Align.Stretch };
        var b = new ProbeVisual { HorizontalAlignment = Align.Stretch };

        var stack = new HStack(a, b) { Spacing = 1, HorizontalAlignment = Align.Stretch };
        stack.Measure(new Size(10, 1));

        Assert.AreEqual(Align.Stretch, a.HorizontalAlignment);
        Assert.AreEqual(int.MaxValue, a.MaxWidth);
        Assert.AreEqual(int.MaxValue, a.MeasureHints.Max.Width);
        Assert.IsGreaterThan(0, a.MeasureHints.FlexGrowX);

        stack.Arrange(new Rectangle(0, 0, 10, 1));

        Assert.IsGreaterThan(0, a.Bounds.Width);
        Assert.IsGreaterThan(0, b.Bounds.Width);
        Assert.AreEqual(10, a.Bounds.Width + b.Bounds.Width + 1);
    }

    private sealed class ProbeVisual : Visual
    {
        protected override SizeHints MeasureCore(in LayoutConstraints constraints)
            => SizeHints.Fixed(Size.Zero);
    }
}
