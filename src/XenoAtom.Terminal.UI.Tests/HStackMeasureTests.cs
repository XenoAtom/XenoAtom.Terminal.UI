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
        var a = new ProbeVisual { HorizontalAlignment = HorizontalAlignment.Stretch };
        var b = new ProbeVisual { HorizontalAlignment = HorizontalAlignment.Stretch };

        var stack = new HStack(a, b) { Spacing = 1 };
        stack.Measure(new Size(10, 1));
        stack.Arrange(new Rectangle(0, 0, 10, 1));

        Assert.IsTrue(a.Bounds.Width > 0);
        Assert.IsTrue(b.Bounds.Width > 0);
        Assert.AreEqual(10, a.Bounds.Width + b.Bounds.Width + 1);
    }

    private sealed class ProbeVisual : Visual
    {
        protected override SizeHints MeasureCore(in LayoutConstraints constraints)
            => SizeHints.Fixed(Size.Zero);
    }
}
