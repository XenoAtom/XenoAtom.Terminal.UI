// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Layout;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class VisualLayoutTests
{
    [TestMethod]
    public void Default_Alignment_Is_Left_Top()
    {
        var v = new FixedSizeVisual(new Size(4, 2));
        v.Measure(new Size(100, 100));
        v.Arrange(new Rectangle(0, 0, 10, 10));

        Assert.AreEqual(new Rectangle(0, 0, 4, 2), v.Bounds);
    }

    [TestMethod]
    public void Center_Alignment_Centers_In_Slot()
    {
        var v = new FixedSizeVisual(new Size(4, 2))
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };

        v.Measure(new Size(100, 100));
        v.Arrange(new Rectangle(0, 0, 10, 10));

        Assert.AreEqual(new Rectangle(3, 4, 4, 2), v.Bounds);
    }

    [TestMethod]
    public void Stretch_Alignment_Fills_Slot()
    {
        var v = new FixedSizeVisual(new Size(4, 2))
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
        };

        v.Measure(new Size(100, 100));
        v.Arrange(new Rectangle(0, 0, 10, 10));

        Assert.AreEqual(new Rectangle(0, 0, 10, 10), v.Bounds);
    }

    [TestMethod]
    public void Min_Max_Constraints_Apply_During_Measure_And_Arrange()
    {
        var v = new FixedSizeVisual(new Size(4, 2))
        {
            MinWidth = 6,
            MinHeight = 3,
            MaxWidth = 7,
            MaxHeight = 4,
        };

        v.Measure(new Size(100, 100));
        Assert.AreEqual(new Size(6, 3), v.DesiredSize);

        v.Arrange(new Rectangle(0, 0, 10, 10));
        Assert.AreEqual(new Rectangle(0, 0, 6, 3), v.Bounds);
    }

    [TestMethod]
    public void Margin_Is_Excluded_From_Bounds_And_Included_In_DesiredSize()
    {
        var v = new FixedSizeVisual(new Size(4, 2))
        {
            Margin = new Thickness(1, 2, 3, 4),
        };

        v.Measure(new Size(100, 100));
        Assert.AreEqual(new Size(4 + 4, 2 + 6), v.DesiredSize);

        v.Arrange(new Rectangle(0, 0, 20, 20));
        Assert.AreEqual(new Rectangle(1, 2, 4, 2), v.Bounds);
    }

    private sealed class FixedSizeVisual : Visual
    {
        private readonly Size _size;

        public FixedSizeVisual(Size size)
        {
            _size = size;
        }

        protected override SizeHints MeasureCore(in LayoutConstraints constraints)
            => SizeHints.Fixed(constraints.Clamp(_size));

        protected override void ArrangeCore(in Rectangle finalRect) { }
    }
}
