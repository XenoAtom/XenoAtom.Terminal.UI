// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Layout;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class WrapHStackLayoutTests
{
    [TestMethod]
    public void WrapHStack_Wraps_Items_Into_Rows()
    {
        var a = new FixedSizeVisual(3, 1);
        var b = new FixedSizeVisual(3, 1);
        var c = new FixedSizeVisual(3, 1);

        var stack = new WrapHStack(a, b, c) { Spacing = 1, RunSpacing = 1 };
        stack.Measure(new Size(7, 10));
        stack.Arrange(new Rectangle(0, 0, 7, 10));

        Assert.AreEqual(new Rectangle(0, 0, 3, 1), a.Bounds);
        Assert.AreEqual(new Rectangle(4, 0, 3, 1), b.Bounds);
        Assert.AreEqual(new Rectangle(0, 2, 3, 1), c.Bounds);
    }

    [TestMethod]
    public void WrapHStack_Justify_Center_Offsets_Items()
    {
        var a = new FixedSizeVisual(2, 1);
        var b = new FixedSizeVisual(2, 1);

        var stack = new WrapHStack(a, b) { Spacing = 1, Justify = WrapJustify.Center, HorizontalAlignment = Align.Stretch };
        stack.Measure(new Size(10, 1));
        stack.Arrange(new Rectangle(0, 0, 10, 1));

        Assert.AreEqual(2, a.Bounds.X);
        Assert.AreEqual(5, b.Bounds.X);
    }

    [TestMethod]
    public void WrapHStack_Unconstrained_MeasureMode_Allows_MainAxis_Overflow()
    {
        var constrained = new ClampToMaxMainVisual(10, 1);
        var unconstrained = new ClampToMaxMainVisual(10, 1);

        var stackConstrained = new WrapHStack(constrained) { MeasureMode = WrapMeasureMode.ConstrainToRun };
        var stackUnconstrained = new WrapHStack(unconstrained) { MeasureMode = WrapMeasureMode.Unconstrained };

        stackConstrained.Measure(new Size(6, 1));
        stackConstrained.Arrange(new Rectangle(0, 0, 6, 1));

        stackUnconstrained.Measure(new Size(6, 1));
        stackUnconstrained.Arrange(new Rectangle(0, 0, 6, 1));

        Assert.AreEqual(6, constrained.Bounds.Width);
        Assert.AreEqual(10, unconstrained.Bounds.Width);
    }

    private sealed class FixedSizeVisual : Visual
    {
        private readonly SizeHints _hints;

        public FixedSizeVisual(int width, int height)
        {
            HorizontalAlignment = Align.Start;
            VerticalAlignment = Align.Start;
            _hints = SizeHints.Fixed(new Size(width, height));
        }

        protected override SizeHints MeasureCore(in LayoutConstraints constraints) => _hints;
    }

    private sealed class ClampToMaxMainVisual : Visual
    {
        private readonly int _desiredWidth;
        private readonly int _desiredHeight;

        public ClampToMaxMainVisual(int desiredWidth, int desiredHeight)
        {
            HorizontalAlignment = Align.Start;
            VerticalAlignment = Align.Start;
            _desiredWidth = desiredWidth;
            _desiredHeight = desiredHeight;
        }

        protected override SizeHints MeasureCore(in LayoutConstraints constraints)
        {
            var maxWidth = constraints.IsWidthBounded ? constraints.MaxWidth : _desiredWidth;
            return SizeHints.Fixed(new Size(Math.Min(_desiredWidth, maxWidth), _desiredHeight));
        }
    }
}
