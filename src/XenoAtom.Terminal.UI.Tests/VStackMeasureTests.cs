// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Geometry;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class VStackMeasureTests
{
    [TestMethod]
    public void VStack_Defaults_To_Horizontal_Stretch()
    {
        var stack = new VStack();
        Assert.AreEqual(Align.Stretch, stack.HorizontalAlignment);
        Assert.AreEqual(Align.Start, stack.VerticalAlignment);

        var root = new DockLayout().Content(stack);
        root.Measure(new Size(30, 6));
        root.Arrange(new Rectangle(0, 0, 30, 6));

        Assert.AreEqual(30, stack.Bounds.Width);
    }
}
