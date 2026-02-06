// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Geometry;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class ZStackTests
{
    [TestMethod]
    public void ZStack_Arranges_All_Children_To_Same_Bounds()
    {
        var first = new TextBlock("First").Stretch();
        var second = new TextBlock("Second").Stretch();

        var stack = new ZStack(first, second);
        stack.Measure(new Size(20, 5));
        stack.Arrange(new Rectangle(0, 0, 20, 5));

        Assert.AreEqual(new Rectangle(0, 0, 20, 5), first.Bounds);
        Assert.AreEqual(new Rectangle(0, 0, 20, 5), second.Bounds);
    }
}
