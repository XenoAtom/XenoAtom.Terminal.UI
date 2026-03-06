// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Layout;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class VisibilityLayoutTests
{
    [TestMethod]
    public void Invisible_Visual_Collapses_During_Measure_And_Arrange()
    {
        var visual = new ProbeVisual(6, 2)
        {
            IsVisible = false,
        };

        var hints = visual.Measure(new LayoutConstraints(0, 20, 0, 10));
        visual.Arrange(new Rectangle(3, 4, 10, 5));

        Assert.AreEqual(Size.Zero, hints.Natural);
        Assert.AreEqual(Size.Zero, visual.DesiredSize);
        Assert.AreEqual(new Rectangle(3, 4, 0, 0), visual.Bounds);
    }

    [TestMethod]
    public void VStack_Ignores_Invisible_Children_For_Size_And_Spacing()
    {
        var first = new ProbeVisual(3, 1);
        var hidden = new ProbeVisual(8, 2)
        {
            IsVisible = false,
        };
        var third = new ProbeVisual(4, 1);

        var stack = new VStack(first, hidden, third)
        {
            Spacing = 1,
        };

        stack.Measure(new LayoutConstraints(0, 20, 0, 20));
        stack.Arrange(new Rectangle(0, 0, 20, 20));

        Assert.AreEqual(new Size(4, 3), stack.DesiredSize);
        Assert.AreEqual(new Rectangle(0, 0, 3, 1), first.Bounds);
        Assert.AreEqual(0, hidden.Bounds.Width);
        Assert.AreEqual(0, hidden.Bounds.Height);
        Assert.AreEqual(new Rectangle(0, 2, 4, 1), third.Bounds);
    }

    [TestMethod]
    public void WrapHStack_Ignores_Invisible_Children_For_Runs_And_Spacing()
    {
        var first = new ProbeVisual(3, 1);
        var hidden = new ProbeVisual(3, 1)
        {
            IsVisible = false,
        };
        var third = new ProbeVisual(3, 1);

        var stack = new WrapHStack(first, hidden, third)
        {
            Spacing = 1,
            RunSpacing = 1,
        };

        stack.Measure(new LayoutConstraints(0, 20, 0, 20));
        stack.Arrange(new Rectangle(0, 0, 20, 20));

        Assert.AreEqual(new Size(7, 1), stack.DesiredSize);
        Assert.AreEqual(new Rectangle(0, 0, 3, 1), first.Bounds);
        Assert.AreEqual(new Rectangle(0, 0, 0, 0), hidden.Bounds);
        Assert.AreEqual(new Rectangle(4, 0, 3, 1), third.Bounds);
    }

    private sealed class ProbeVisual : Visual
    {
        private readonly Size _size;

        public ProbeVisual(int width, int height)
        {
            _size = new Size(width, height);
        }

        protected override SizeHints MeasureCore(in LayoutConstraints constraints)
            => SizeHints.Fixed(constraints.Clamp(_size));
    }
}
