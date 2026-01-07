// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Geometry;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class HStackMeasureTests
{
    [TestMethod]
    public void HStack_Does_Not_Starve_Stretch_Children_During_Measure()
    {
        var a = new ProbeVisual { HorizontalAlignment = HorizontalAlignment.Stretch };
        var b = new ProbeVisual { HorizontalAlignment = HorizontalAlignment.Stretch };

        var stack = new HStack(a, b) { Spacing = 1 };
        stack.Measure(new Size(10, 5));

        Assert.IsTrue(a.LastAvailableSize.Width > 0);
        Assert.IsTrue(b.LastAvailableSize.Width > 0);
        Assert.IsTrue(a.LastAvailableSize.Width + b.LastAvailableSize.Width <= 9);
    }

    private sealed class ProbeVisual : Visual
    {
        public Size LastAvailableSize { get; private set; }

        protected override Size MeasureOverride(Size availableSize)
        {
            LastAvailableSize = availableSize;
            return Size.Zero;
        }
    }
}
