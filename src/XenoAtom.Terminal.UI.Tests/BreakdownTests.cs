// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Hosting;
using XenoAtom.Terminal.UI.Layout;
using XenoAtom.Terminal.UI.Styling;
using System.Text;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class BreakdownTests
{
    [TestMethod]
    public void Breakdown_Legend_Reflow_DoesNot_Reparent_Segment_Label()
    {
        // This is a regression test for a crash where the legend was rebuilt by creating new visuals
        // while reusing the same segment label instances, causing a "visual already has a parent" exception.
        var breakdown = new BreakdownChart()
            .ShowValues(true)
            .ShowPercentages(true)
            .Style(new BreakdownStyle { LegendLayout = BreakdownLegendLayout.Compact })
            .Segment(42, "🗃️  Data")
            .Segment(18, "📦  Packages")
            .Segment(9, "🧹  Temp")
            .Segment(3, "🧯  Other");

        // Measure once with an unbounded width (common when hosted inside a ScrollViewer)...
        breakdown.Measure(new LayoutConstraints(0, LayoutConstants.Infinite, 0, LayoutConstants.Infinite));
        // ...then with a finite width (during arrange/layout pass).
        breakdown.Measure(new LayoutConstraints(0, 40, 0, LayoutConstants.Infinite));
    }

    [TestMethod]
    public void Breakdown_Default_Tooltip_DoesNot_Reparent_Segment_Label()
    {
        // Regression test for a crash where the default tooltip attempted to reuse the segment label visual (already
        // attached to the legend), causing a "visual already has a parent" exception when the tooltip window opened.
        var breakdown = new BreakdownChart()
            .ShowValues(false)
            .ShowPercentages(false)
            .Style(new BreakdownStyle { FillRune = new Rune('#') })
            .Segment(1, new TextBlock("A"));

        var root = new VStack
        {
            " ",
            " ",
            breakdown,
        };

        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(30, 10));
        driver.Tick();

        var x = breakdown.Bounds.X + 1;
        var y = breakdown.Bounds.Y;

        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Move, Button = TerminalMouseButton.None, X = x, Y = y });
        driver.Tick(2);

        var screen = new AnsiTestScreen(30, 10);
        screen.Apply(driver.Backend.GetOutText());
        StringAssert.Contains(screen.GetText(), "(100%)");
    }

    [TestMethod]
    public void Breakdown_Distributes_Segment_Widths_LeftToRight()
    {
        var breakdown = new BreakdownChart()
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

        var breakdown = new BreakdownChart()
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
