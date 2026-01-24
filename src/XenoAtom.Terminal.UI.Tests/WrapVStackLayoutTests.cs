// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Layout;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class WrapVStackLayoutTests
{
    [TestMethod]
    public void WrapVStack_Wraps_Items_Into_Columns()
    {
        var a = new FixedSizeVisual(1, 2);
        var b = new FixedSizeVisual(1, 2);
        var c = new FixedSizeVisual(1, 2);

        var stack = new WrapVStack(a, b, c) { Spacing = 1, RunSpacing = 1 };
        stack.Measure(new Size(10, 5));
        stack.Arrange(new Rectangle(0, 0, 10, 5));

        Assert.AreEqual(new Rectangle(0, 0, 1, 2), a.Bounds);
        Assert.AreEqual(new Rectangle(0, 3, 1, 2), b.Bounds);
        Assert.AreEqual(new Rectangle(2, 0, 1, 2), c.Bounds);
    }

    [TestMethod]
    public void WrapVStack_Justify_End_Offsets_Items()
    {
        var a = new FixedSizeVisual(1, 1);
        var b = new FixedSizeVisual(1, 1);

        var stack = new WrapVStack(a, b) { Spacing = 1, Justify = WrapJustify.End, VerticalAlignment = Align.Stretch };
        stack.Measure(new Size(1, 6));
        stack.Arrange(new Rectangle(0, 0, 1, 6));

        Assert.AreEqual(3, a.Bounds.Y);
        Assert.AreEqual(5, b.Bounds.Y);
    }

    [TestMethod]
    public void WrapVStack_Unconstrained_MeasureMode_Allows_MainAxis_Overflow()
    {
        var constrained = new ClampToMaxMainVisual(1, 10);
        var unconstrained = new ClampToMaxMainVisual(1, 10);

        var stackConstrained = new WrapVStack(constrained) { MeasureMode = WrapMeasureMode.ConstrainToRun };
        var stackUnconstrained = new WrapVStack(unconstrained) { MeasureMode = WrapMeasureMode.Unconstrained };

        stackConstrained.Measure(new Size(1, 6));
        stackConstrained.Arrange(new Rectangle(0, 0, 1, 6));

        stackUnconstrained.Measure(new Size(1, 6));
        stackUnconstrained.Arrange(new Rectangle(0, 0, 1, 6));

        Assert.AreEqual(6, constrained.Bounds.Height);
        Assert.AreEqual(10, unconstrained.Bounds.Height);
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
            var maxHeight = constraints.IsHeightBounded ? constraints.MaxHeight : _desiredHeight;
            return SizeHints.Fixed(new Size(_desiredWidth, Math.Min(_desiredHeight, maxHeight)));
        }
    }
}
