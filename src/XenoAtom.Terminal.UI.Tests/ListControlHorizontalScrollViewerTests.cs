// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Collections;
using XenoAtom.Terminal.UI.Hosting;
using XenoAtom.Terminal.UI.Scrolling;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class ListControlHorizontalScrollViewerTests
{
    [TestMethod]
    public void ListBox_Shows_HorizontalScrollBar_When_VerticalScrollBar_Reduces_Viewport()
    {
        var list = new ListBox<string>();
        Populate(list.Items);
        AssertShowsHorizontalScrollbarAndScrolls(list);
    }

    [TestMethod]
    public void ListBox_DoesNotShow_HorizontalScrollBar_When_Width_Can_Grow_For_VerticalScrollBar()
    {
        var list = new ListBox<string>();
        PopulateShort(list.Items);
        AssertDoesNotShowHorizontalScrollbarWhenWidthCanGrow(list, expectedVisibleItemText: "Item 00");
    }

    [TestMethod]
    public void OptionList_Shows_HorizontalScrollBar_When_VerticalScrollBar_Reduces_Viewport()
    {
        var list = new OptionList<string>();
        Populate(list.Items);
        AssertShowsHorizontalScrollbarAndScrolls(list);
    }

    [TestMethod]
    public void OptionList_DoesNotShow_HorizontalScrollBar_When_Width_Can_Grow_For_VerticalScrollBar()
    {
        var list = new OptionList<string>();
        PopulateShort(list.Items);
        AssertDoesNotShowHorizontalScrollbarWhenWidthCanGrow(list, expectedVisibleItemText: "Item 00");
    }

    [TestMethod]
    public void RadioButtonList_Shows_HorizontalScrollBar_When_VerticalScrollBar_Reduces_Viewport()
    {
        var list = new RadioButtonList<string>();
        Populate(list.Items);
        AssertShowsHorizontalScrollbarAndScrolls(list);
    }

    [TestMethod]
    public void RadioButtonList_DoesNotShow_HorizontalScrollBar_When_Width_Can_Grow_For_VerticalScrollBar()
    {
        var list = new RadioButtonList<string>();
        PopulateShort(list.Items);
        AssertDoesNotShowHorizontalScrollbarWhenWidthCanGrow(list, expectedVisibleItemText: "Item 00");
    }

    [TestMethod]
    public void SelectionList_Shows_HorizontalScrollBar_When_VerticalScrollBar_Reduces_Viewport()
    {
        var list = new SelectionList<string>();
        for (var i = 0; i < 30; i++)
        {
            list.AddItem($"Item {i:00} - 0123456789");
        }

        AssertShowsHorizontalScrollbarAndScrolls(list);
    }

    [TestMethod]
    public void SelectionList_DoesNotShow_HorizontalScrollBar_When_Width_Can_Grow_For_VerticalScrollBar()
    {
        var list = new SelectionList<string>();
        for (var i = 0; i < 30; i++)
        {
            list.AddItem($"Item {i:00}");
        }

        AssertDoesNotShowHorizontalScrollbarWhenWidthCanGrow(list, expectedVisibleItemText: "Item 00");
    }

    private static void Populate(BindableList<string> items)
    {
        for (var i = 0; i < 30; i++)
        {
            items.Add($"Item {i:00} - 0123456789");
        }
    }

    private static void PopulateShort(BindableList<string> items)
    {
        for (var i = 0; i < 30; i++)
        {
            items.Add($"Item {i:00}");
        }
    }

    private static void AssertShowsHorizontalScrollbarAndScrolls(IScrollable control)
    {
        var visual = (Visual)control;
        visual.HorizontalAlignment = Align.Stretch;
        visual.VerticalAlignment = Align.Stretch;

        var scrollViewer = new ScrollViewer(visual)
        {
            MinWidth = 10,
            MaxWidth = 10,
            MinHeight = 5,
            MaxHeight = 5,
            HorizontalAlignment = Align.Start,
            VerticalAlignment = Align.Start,
        };

        var bordered = new Border(scrollViewer)
        {
            HorizontalAlignment = Align.Start,
            VerticalAlignment = Align.Start,
        };

        var root = new VStack(bordered).Spacing(0);
        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(20, 10));
        driver.Tick();

        var bars = scrollViewer.EnumerateVisualsDepthFirst().OfType<ScrollBar>().ToArray();
        Assert.HasCount(2, bars, "Expected ScrollViewer to have both internal scroll bars.");

        var v = bars.Single(b => b.Orientation == Orientation.Vertical);
        var h = bars.Single(b => b.Orientation == Orientation.Horizontal);

        Assert.IsTrue(v.IsVisible, "Expected content to overflow vertically.");
        Assert.IsTrue(h.IsVisible, "Expected horizontal overflow due to the vertical bar reducing viewport width.");
        Assert.IsGreaterThan(control.Scroll.ViewportWidth, control.Scroll.ExtentWidth, "Expected a horizontal extent larger than the viewport.");

        var firstItem = visual.EnumerateVisualsDepthFirst().First(vv => vv.Parent == visual);
        var x0 = firstItem.Bounds.X;

        scrollViewer.HorizontalOffset = 1;
        driver.TickUntil(() => control.Scroll.OffsetX == 1);

        Assert.AreEqual(x0 - 1, firstItem.Bounds.X, "Expected item visuals to shift when horizontally scrolled.");
    }

    private static void AssertDoesNotShowHorizontalScrollbarWhenWidthCanGrow(IScrollable control, string expectedVisibleItemText)
    {
        var visual = (Visual)control;
        visual.HorizontalAlignment = Align.Stretch;
        visual.VerticalAlignment = Align.Stretch;

        var scrollViewer = new ScrollViewer(visual)
        {
            MinHeight = 5,
            MaxHeight = 5,
            HorizontalAlignment = Align.Start,
            VerticalAlignment = Align.Start,
        };

        var bordered = new Border(scrollViewer)
        {
            HorizontalAlignment = Align.Start,
            VerticalAlignment = Align.Start,
        };

        var root = new HStack(bordered).Spacing(0);
        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(40, 10));
        driver.Tick();

        var bars = scrollViewer.EnumerateVisualsDepthFirst().OfType<ScrollBar>().ToArray();
        Assert.HasCount(2, bars, "Expected ScrollViewer to have both internal scroll bars.");

        var v = bars.Single(b => b.Orientation == Orientation.Vertical);
        var h = bars.Single(b => b.Orientation == Orientation.Horizontal);

        Assert.IsTrue(v.IsVisible, "Expected content to overflow vertically.");
        Assert.IsFalse(h.IsVisible, "Expected ScrollViewer to reserve width for the vertical bar instead of introducing horizontal overflow.");
        Assert.AreEqual(control.Scroll.ViewportWidth, control.Scroll.ExtentWidth, "Expected no horizontal overflow when width can grow.");

        var screen = new AnsiTestScreen(40, 10);
        screen.Apply(driver.Backend.GetOutText());
        var rendered = screen.GetText();
        StringAssert.Contains(rendered, expectedVisibleItemText, "Expected the first item text to be visible without horizontal scrolling.");
    }
}
