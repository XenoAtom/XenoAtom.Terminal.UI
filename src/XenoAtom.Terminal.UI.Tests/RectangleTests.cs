// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Geometry;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class RectangleTests
{
    [TestMethod]
    public void Rectangle_Intersects_Matches_Expected_Semantics()
    {
        var a = new Rectangle(0, 0, 3, 3);
        var b = new Rectangle(2, 2, 3, 3);
        var c = new Rectangle(3, 0, 1, 1); // touches a's right edge, no overlap

        Assert.IsTrue(a.Intersects(b));
        Assert.IsTrue(Rectangle.Intersects(a, b));
        Assert.IsFalse(a.Intersects(c));
        Assert.IsFalse(Rectangle.Intersects(a, c));
    }

    [TestMethod]
    public void Rectangle_Union_Contains_Both_Rectangles()
    {
        var a = new Rectangle(1, 2, 3, 4);
        var b = new Rectangle(0, 0, 2, 2);

        var u = Rectangle.Union(a, b);

        Assert.AreEqual(0, u.X);
        Assert.AreEqual(0, u.Y);
        Assert.AreEqual(4, u.Right);
        Assert.AreEqual(6, u.Bottom);
    }
}

