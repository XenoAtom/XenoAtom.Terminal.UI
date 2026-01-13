// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Layout;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class ScrollViewerLayoutTests
{
    [TestMethod]
    public void ScrollViewer_Stretches_Content_To_Viewport_When_NoHorizontalScrolling()
    {
        var content = new VStack("Hello");
        var scroll = new ScrollViewer { Content = content };

        var root = new DockLayout().Content(scroll);

        root.Measure(new Size(80, 20));
        root.Arrange(new Rectangle(0, 0, 80, 20));

        Assert.AreEqual(80, content.Bounds.Width);
        Assert.AreEqual(20, content.Bounds.Height);
    }

    [TestMethod]
    public void ScrollViewer_Updates_Viewport_Width_After_Resize()
    {
        var content = new VStack("Hello");
        var scroll = new ScrollViewer { Content = content };

        var root = new DockLayout().Content(scroll);

        root.Measure(new Size(80, 20));
        root.Arrange(new Rectangle(0, 0, 80, 20));
        Assert.AreEqual(80, content.Bounds.Width);

        root.Measure(new Size(81, 20));
        root.Arrange(new Rectangle(0, 0, 81, 20));
        Assert.AreEqual(81, content.Bounds.Width);
    }

    [TestMethod]
    public void ScrollViewer_DoesNot_Show_HorizontalScrollBar_When_Only_Width_Reduces_From_VerticalScrollBar()
    {
        var content = new WrapLikeVisual(totalCells: 61);

        var scroll = new ScrollViewer
        {
            Content = content,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
        };

        scroll.Measure(new Size(20, 3));
        scroll.Arrange(new Rectangle(0, 0, 20, 3));

        var bars = scroll.EnumerateVisualsDepthFirst().OfType<ScrollBar>().ToArray();
        Assert.AreEqual(2, bars.Length, "Expected ScrollViewer to have both internal scroll bars.");

        var v = bars.Single(b => b.Orientation == Orientation.Vertical);
        var h = bars.Single(b => b.Orientation == Orientation.Horizontal);

        Assert.IsTrue(v.IsVisible, "Expected content to overflow vertically (wrapping increases height).");
        Assert.IsFalse(h.IsVisible, "Horizontal bar should not appear due to the vertical bar reducing viewport width by 1.");
    }

    private sealed class WrapLikeVisual : Visual
    {
        private readonly int _totalCells;

        public WrapLikeVisual(int totalCells)
        {
            _totalCells = Math.Max(0, totalCells);
            HorizontalAlignment = HorizontalAlignment.Stretch;
            VerticalAlignment = VerticalAlignment.Stretch;
        }

        protected override SizeHints MeasureCore(in LayoutConstraints constraints)
        {
            var naturalWidth = Math.Max(0, _totalCells);
            var maxW = constraints.MaxWidth;

            if (maxW == int.MaxValue)
            {
                return SizeHints.Flex(
                    min: new Size(1, 1),
                    natural: new Size(Math.Clamp(naturalWidth, 0, int.MaxValue - 1), 1),
                    max: new Size(Math.Clamp(naturalWidth, 0, int.MaxValue - 1), int.MaxValue),
                    growX: 0,
                    growY: 0,
                    shrinkX: 1,
                    shrinkY: 0);
            }

            var width = Math.Max(1, Math.Min(int.MaxValue - 1, maxW));
            var lines = (naturalWidth + width - 1) / width;
            var height = Math.Max(1, lines);

            return SizeHints.Flex(
                min: new Size(1, 1),
                natural: new Size(width, Math.Clamp(height, 0, int.MaxValue - 1)),
                max: new Size(int.MaxValue, int.MaxValue),
                growX: 0,
                growY: 0,
                shrinkX: 1,
                shrinkY: 0);
        }

        protected override void ArrangeCore(in Rectangle finalRect) => Bounds = finalRect;
    }
}
