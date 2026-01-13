// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Geometry;

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
}

