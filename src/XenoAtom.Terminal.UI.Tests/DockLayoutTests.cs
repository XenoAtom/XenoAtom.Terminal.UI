// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Geometry;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class DockLayoutTests
{
    [TestMethod]
    public void DockLayout_Arranges_Top_Bottom_And_Content_Regions()
    {
        var top = new TextBlock("Top");
        var bottom = new TextBlock("Bottom");
        var content = new VStack("Body").Stretch();

        var dock = new DockLayout
        {
            Top = top,
            Bottom = bottom,
            Content = content,
        };

        dock.Measure(new Size(40, 6));
        dock.Arrange(new Rectangle(0, 0, 40, 6));

        Assert.AreEqual(0, top.Bounds.Y);
        Assert.AreEqual(1, top.Bounds.Height);

        Assert.AreEqual(5, bottom.Bounds.Y);
        Assert.AreEqual(1, bottom.Bounds.Height);

        Assert.AreEqual(1, content.Bounds.Y);
        Assert.AreEqual(4, content.Bounds.Height);
    }

    [TestMethod]
    public void DockLayout_Measure_Uses_Widest_Child()
    {
        var top = new TextBlock("T");
        var content = new VStack("LongContentWidth").Stretch();
        var bottom = new TextBlock("B");

        var dock = new DockLayout
        {
            Top = top,
            Content = content,
            Bottom = bottom,
        };

        dock.Measure(new Size(80, 20));

        Assert.AreEqual(content.DesiredSize.Width, dock.DesiredSize.Width);
    }

    [TestMethod]
    public void DockLayout_With_Null_Top_Does_Not_Reserve_Space()
    {
        var bottom = new TextBlock("Bottom");
        var content = new VStack("Body").Stretch();

        var dock = new DockLayout
        {
            Top = null,
            Bottom = bottom,
            Content = content,
        };

        dock.Measure(new Size(40, 6));
        dock.Arrange(new Rectangle(0, 0, 40, 6));

        Assert.AreEqual(5, bottom.Bounds.Y);
        Assert.AreEqual(1, bottom.Bounds.Height);

        Assert.AreEqual(0, content.Bounds.Y);
        Assert.AreEqual(5, content.Bounds.Height);
    }

    [TestMethod]
    public void DockLayout_With_Null_Bottom_Does_Not_Reserve_Space()
    {
        var top = new TextBlock("Top");
        var content = new VStack("Body").Stretch();

        var dock = new DockLayout
        {
            Top = top,
            Bottom = null,
            Content = content,
        };

        dock.Measure(new Size(40, 6));
        dock.Arrange(new Rectangle(0, 0, 40, 6));

        Assert.AreEqual(0, top.Bounds.Y);
        Assert.AreEqual(1, top.Bounds.Height);

        Assert.AreEqual(1, content.Bounds.Y);
        Assert.AreEqual(5, content.Bounds.Height);
    }

    [TestMethod]
    public void DockLayout_With_Null_Top_And_Bottom_Uses_Full_Content_Height()
    {
        var content = new VStack("Body").Stretch();

        var dock = new DockLayout
        {
            Top = null,
            Bottom = null,
            Content = content,
        };

        dock.Measure(new Size(40, 6));
        dock.Arrange(new Rectangle(0, 0, 40, 6));

        Assert.AreEqual(0, content.Bounds.Y);
        Assert.AreEqual(6, content.Bounds.Height);
    }
}
