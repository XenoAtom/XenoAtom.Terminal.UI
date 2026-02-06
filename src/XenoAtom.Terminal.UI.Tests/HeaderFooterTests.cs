// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Geometry;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class HeaderFooterTests
{
    [TestMethod]
    public void Header_Arranges_Left_Center_Right_Slots()
    {
        var left = new TextBlock("L");
        var center = new TextBlock("CENTER");
        var right = new TextBlock("R");

        var header = new Header { Left = left, Center = center, Right = right };
        header.Measure(new Size(20, 1));
        header.Arrange(new Rectangle(0, 0, 20, 1));

        Assert.AreEqual(0, left.Bounds.X);
        Assert.AreEqual(19, right.Bounds.X);
        Assert.IsTrue(center.Bounds.X > left.Bounds.Right);
        Assert.IsTrue(center.Bounds.Right < right.Bounds.X + 1);
    }

    [TestMethod]
    public void Footer_Arranges_Left_Center_Right_Slots()
    {
        var left = new TextBlock("L");
        var center = new TextBlock("CENTER");
        var right = new TextBlock("R");

        var footer = new Footer { Left = left, Center = center, Right = right };
        footer.Measure(new Size(20, 1));
        footer.Arrange(new Rectangle(0, 0, 20, 1));

        Assert.AreEqual(0, left.Bounds.X);
        Assert.AreEqual(19, right.Bounds.X);
        Assert.IsTrue(center.Bounds.X > left.Bounds.Right);
        Assert.IsTrue(center.Bounds.Right < right.Bounds.X + 1);
    }
}
