// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Layout;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class CenterTests
{
    [TestMethod]
    public void Center_Defaults_To_Stretch_And_Centers_Content()
    {
        var content = new FixedSizeVisual(new Size(2, 1));
        var center = new Center(content);

        center.Measure(new Size(100, 100));
        center.Arrange(new Rectangle(0, 0, 10, 5));

        Assert.AreEqual(new Rectangle(0, 0, 10, 5), center.Bounds, "Center should stretch to its slot by default.");
        Assert.AreEqual(new Rectangle(4, 2, 2, 1), content.Bounds, "Content should be centered within the slot.");
    }

    private sealed class FixedSizeVisual : Visual
    {
        private readonly Size _size;

        public FixedSizeVisual(Size size) => _size = size;

        protected override SizeHints MeasureCore(in LayoutConstraints constraints)
            => SizeHints.Fixed(constraints.Clamp(_size));

        protected override void ArrangeCore(in Rectangle finalRect) { }
    }
}

