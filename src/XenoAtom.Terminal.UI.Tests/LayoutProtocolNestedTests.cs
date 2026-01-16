// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Layout;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class LayoutProtocolNestedTests
{
    [TestMethod]
    public void ScrollViewer_Uses_Extent_When_Unbounded_And_Viewport_When_Bounded()
    {
        var content = new FixedSizeVisual(new Size(100, 50));
        var viewer = new ScrollViewer { Content = content };

        viewer.Measure(LayoutConstraints.Unbounded);
        Assert.AreEqual(new Size(100, 50), viewer.DesiredSize);

        viewer.Measure(new LayoutConstraints(0, 40, 0, 10));
        Assert.AreEqual(new Size(40, 10), viewer.DesiredSize);
    }

    [TestMethod]
    public void Nested_Containers_Measure_And_Arrange_Without_Unbounded_Natural_Sizes()
    {
        var viewer = new ScrollViewer
        {
            Content = new VStack(
                    new Markup("Line 1\nLine 2\nLine 3").Wrap(true),
                    new Rule(),
                    new TextBlock("Side text"))
                .Spacing(1)
                .HorizontalAlignment(HorizontalAlignment.Stretch),
        };

        var root = new HStack(
            new Group { Content = viewer },
            new Border("Right"))
        {
            Spacing = 1,
        };

        root.Measure(new LayoutConstraints(0, 60, 0, 12));
        root.Arrange(new Rectangle(0, 0, 60, 12));

        Assert.IsTrue(root.Bounds.Width > 0);
        Assert.IsTrue(root.Bounds.Height > 0);
        Assert.IsTrue(viewer.Bounds.Width > 0);
        Assert.IsTrue(viewer.Bounds.Height > 0);
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
