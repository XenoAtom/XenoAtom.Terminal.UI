// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Scrolling;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class ScrollModelTests
{
    [TestMethod]
    public void ScrollModel_DoesNotThrow_WhenVersionIsReadThenScrollChangesDuringPrepareChildren()
    {
        var visual = new ReadThenChangeScrollVisual();

        using var driver = new TerminalAppTestDriver(visual, TerminalHostKind.Fullscreen, new TerminalSize(10, 5));

        driver.Tick();

        Assert.IsTrue(visual.Prepared, "Expected the visual to run PrepareChildren during layout.");
        Assert.AreEqual(1, visual.Scroll.OffsetY, "Expected the scroll change made during PrepareChildren to apply.");
        Assert.AreEqual(1, visual.ChangedCount, "Expected tracking-time scroll changes to raise one deferred change notification.");
    }

    private sealed class ReadThenChangeScrollVisual : Visual, IScrollable
    {
        public ReadThenChangeScrollVisual()
        {
            Scroll = new ScrollModel(this);
            Scroll.Changed += () => ChangedCount++;
        }

        public ScrollModel Scroll { get; }

        public bool Prepared { get; private set; }

        public int ChangedCount { get; private set; }

        protected override void PrepareChildren()
        {
            if (Prepared)
            {
                return;
            }

            Prepared = true;
            _ = Scroll.Version;
            Scroll.SetViewport(1, 1);
            Scroll.SetExtent(1, 3);
            Scroll.SetOffset(0, 1);
        }

        protected override SizeHints MeasureCore(in LayoutConstraints constraints)
            => SizeHints.Fixed(constraints.Clamp(new Size(1, 1)));
    }
}
