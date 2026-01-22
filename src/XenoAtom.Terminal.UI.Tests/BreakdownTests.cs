// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Hosting;
using XenoAtom.Terminal.UI.Styling;
using System.Text;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class BreakdownTests
{
    [TestMethod]
    public void Breakdown_Distributes_Segment_Widths_LeftToRight()
    {
        var breakdown = new Breakdown()
            .ShowValues(false)
            .ShowPercentages(false)
            .Style(new BreakdownStyle { FillRune = new Rune('#'), SegmentGap = 1 })
            .Segment(1, "A")
            .Segment(1, "B")
            .Segment(1, "C");

        using var driver = new TerminalAppTestDriver(breakdown, TerminalHostKind.Fullscreen, new TerminalSize(10, 4));
        driver.Tick();

        var screen = new AnsiTestScreen(10, 4);
        screen.Apply(driver.Backend.GetOutText());

        var rendered = screen.GetText();
        var line0 = rendered.Substring(0, 10);
        Assert.AreEqual("### ### ##", line0);
    }

    [TestMethod]
    public void Breakdown_Raises_SegmentClicked()
    {
        var clickedIndex = -1;

        var breakdown = new Breakdown()
            .ShowValues(false)
            .ShowPercentages(false)
            .Style(new BreakdownStyle { FillRune = new Rune('#'), SegmentGap = 1 })
            .Segment(1, "A")
            .Segment(1, "B");

        breakdown.SegmentClicked((_, e) => clickedIndex = e.Index);

        using var driver = new TerminalAppTestDriver(breakdown, TerminalHostKind.Fullscreen, new TerminalSize(20, 3));
        driver.Tick();

        var x = breakdown.Bounds.X + 15;
        var y = breakdown.Bounds.Y;

        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Down, Button = TerminalMouseButton.Left, X = x, Y = y });
        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Up, Button = TerminalMouseButton.Left, X = x, Y = y });
        driver.TickUntil(() => clickedIndex == 1);

        Assert.AreEqual(1, clickedIndex);
    }
}
