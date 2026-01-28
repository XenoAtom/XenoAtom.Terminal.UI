// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Layout;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class PadderMeasureArrangeTests
{
    [TestMethod]
    public void Padder_Measures_To_Content_Plus_Padding()
    {
        var content = new FixedSizeVisual(new Size(5, 2));
        var padder = new Padder(content).Padding(new Thickness(Left: 1, Top: 2, Right: 3, Bottom: 4));

        padder.Measure(LayoutConstraints.Unbounded);

        Assert.AreEqual(new Size(9, 8), padder.DesiredSize);
    }

    [TestMethod]
    public void Padder_Arranges_Content_Inside_Padding()
    {
        var content = new FixedSizeVisual(new Size(5, 2));
        var padding = new Thickness(Left: 1, Top: 2, Right: 3, Bottom: 4);
        var padder = new Padder(content).Padding(padding);

        padder.Measure(LayoutConstraints.Unbounded);
        padder.Arrange(new Rectangle(0, 0, padder.DesiredSize.Width, padder.DesiredSize.Height));

        Assert.AreEqual(new Rectangle(1, 2, 5, 2), content.Bounds);
    }

    [TestMethod]
    public void Border_Reserves_One_Cell_Inset_Around_Content()
    {
        var content = new FixedSizeVisual(new Size(5, 2));
        var border = new Border(content).Padding(1);

        border.Measure(LayoutConstraints.Unbounded);

        // 1 cell border on each side + padding(1) on each side
        // => horizontal = 2 + 2, vertical = 2 + 2
        Assert.AreEqual(new Size(9, 6), border.DesiredSize);
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
    }
}
