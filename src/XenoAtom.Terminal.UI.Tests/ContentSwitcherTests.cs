// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Layout;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class ContentSwitcherTests
{
    [TestMethod]
    public void Measures_And_Arranges_Only_Selected_Child()
    {
        var first = new CountingVisual(new Size(3, 1));
        var second = new CountingVisual(new Size(7, 2));

        var switcher = new ContentSwitcher();
        switcher.Children.Add(first);
        switcher.Children.Add(second);
        switcher.SelectedIndex = 1;

        switcher.Measure(new Size(100, 100));
        Assert.AreEqual(new Size(7, 2), switcher.DesiredSize);
        Assert.AreEqual(0, first.MeasureCount);
        Assert.AreEqual(1, second.MeasureCount);

        switcher.Arrange(new Rectangle(0, 0, 10, 10));
        Assert.AreEqual(0, first.ArrangeCount);
        Assert.AreEqual(1, second.ArrangeCount);
        Assert.AreEqual(new Rectangle(0, 0, 7, 2), second.Bounds);
    }

    private sealed class CountingVisual : Visual
    {
        private readonly Size _desired;

        public CountingVisual(Size desired)
        {
            _desired = desired;
        }

        public int MeasureCount { get; private set; }

        public int ArrangeCount { get; private set; }

        protected override SizeHints MeasureCore(in LayoutConstraints constraints)
        {
            MeasureCount++;
            return SizeHints.Fixed(constraints.Clamp(_desired));
        }

        protected override void ArrangeCore(in Rectangle finalRect)
        {
            ArrangeCount++;
        }
    }
}
