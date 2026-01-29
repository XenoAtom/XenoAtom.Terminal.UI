// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.Backends;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Hosting;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class OptionListTests
{
    [TestMethod]
    public void OptionList_ArrowDown_Raises_SelectionChanged()
    {
        var list = new OptionList<OptionListItem> { MinHeight = 4, MaxHeight = 4 };
        list.Items.AddRange(
            new OptionListItem("First"),
            new OptionListItem("Second"),
            new OptionListItem("Third"));

        (int OldIndex, int NewIndex)? selectionChanged = null;
        list.SelectionChanged((_, e) => selectionChanged = (e.OldIndex, e.NewIndex));

        var root = new VStack { list };
        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(40, 10));
        driver.Tick();

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Down });
        driver.TickUntil(() => selectionChanged is not null);

        Assert.AreEqual(0, selectionChanged!.Value.OldIndex);
        Assert.AreEqual(1, selectionChanged!.Value.NewIndex);
    }

    [TestMethod]
    public void OptionList_Enter_Raises_ItemActivated()
    {
        var list = new OptionList<OptionListItem> { MinHeight = 4, MaxHeight = 4 };
        list.Items.AddRange(
            new OptionListItem("First"),
            new OptionListItem("Second"),
            new OptionListItem("Third"));

        int? activated = null;
        list.ItemActivated((_, e) => activated = e.Index);

        var root = new VStack { list };
        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(40, 10));
        driver.Tick();

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Down });
        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Enter });
        driver.TickUntil(() => activated is not null);

        Assert.AreEqual(1, activated);
    }

    [TestMethod]
    public void OptionList_Renders_Descriptions_On_Second_Line()
    {
        var list = new OptionList<OptionListItem> { MinHeight = 4, MaxHeight = 4 };
        list.Items.AddRange(
            new OptionListItem("Build", "Ctrl+B") { Description = "Build the project" },
            new OptionListItem("Run", "F5") { Description = "Run the app" });

        var root = new VStack { list };
        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(40, 10));
        driver.Tick();

        var outText = driver.Backend.GetOutText();
        var screen = new AnsiTestScreen(40, 10);
        screen.Apply(outText);
        var rendered = screen.GetText();

        StringAssert.Contains(rendered, "Build the project");
        StringAssert.Contains(rendered, "Run the app");
    }

    [TestMethod]
    public void OptionList_MouseWheel_Skips_Disabled_Items()
    {
        var list = new OptionList<OptionListItem> { MinHeight = 4, MaxHeight = 4 };
        list.Items.AddRange(
            new OptionListItem("Header") { IsEnabled = false },
            new OptionListItem("First"),
            new OptionListItem("Second"));

        list.SelectedIndex = 1;

        var root = new VStack { list };
        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(40, 10));
        driver.Tick();

        // Wheel up from "First": should skip the disabled header and remain on the first enabled item.
        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Wheel, Button = TerminalMouseButton.Wheel, WheelDelta = 1, X = 1, Y = 0 });
        driver.Tick();
        Assert.AreEqual(1, list.SelectedIndex);

        // Wheel down: should move to "Second".
        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Wheel, Button = TerminalMouseButton.Wheel, WheelDelta = -1, X = 1, Y = 0 });
        driver.Tick();
        Assert.AreEqual(2, list.SelectedIndex);

        // Wheel up from "Second": should go back to "First".
        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Wheel, Button = TerminalMouseButton.Wheel, WheelDelta = 1, X = 1, Y = 0 });
        driver.Tick();
        Assert.AreEqual(1, list.SelectedIndex);
    }

    [TestMethod]
    public void OptionList_Scrolls_Selected_Item_Into_View()
    {
        var list = new OptionList<OptionListItem> { MinHeight = 3, MaxHeight = 3 };
        for (var i = 0; i < 20; i++)
        {
            list.Items.Add(new OptionListItem($"Item {i:00}"));
        }

        var scrollViewer = new ScrollViewer(list) { MinHeight = 3, MaxHeight = 3 };
        var root = new VStack { scrollViewer };
        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(40, 10));
        driver.Tick();

        Assert.IsGreaterThan(0, scrollViewer.Bounds.Height);
        Assert.IsGreaterThan(0, list.Bounds.Height);
        Assert.IsGreaterThan(0, list.Scroll.ViewportHeight);
        Assert.IsGreaterThan(0, list.Scroll.ExtentHeight);

        list.SelectedIndex = 15;
        driver.Tick();

        Assert.IsGreaterThan(0, list.Scroll.OffsetY);
    }

    [TestMethod]
    public void OptionList_Scrolls_Rendered_Viewport_When_Selection_Moves()
    {
        var list = new OptionList<OptionListItem> { MinHeight = 3, MaxHeight = 3 };
        for (var i = 0; i < 7; i++)
        {
            list.Items.Add(new OptionListItem($"Item {i:00}"));
        }

        var scrollViewer = new ScrollViewer(list) { MinHeight = 3, MaxHeight = 3 };
        var root = new VStack(scrollViewer).Spacing(0);
        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(30, 8));
        driver.Tick();

        // Move selection from 0 -> 3. With a 3-row viewport, selecting index 3 must scroll so Item 03 is visible.
        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Down });
        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Down });
        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Down });
        driver.Tick();

        var outText = driver.Backend.GetOutText();
        var screen = new AnsiTestScreen(30, 8);
        screen.Apply(outText);
        var rendered = screen.GetText();

        Assert.IsFalse(rendered.Contains("Item 00", StringComparison.Ordinal), "Expected the viewport to scroll past the first item.");
        StringAssert.Contains(rendered, "Item 03", "Expected the selected item to be visible after scrolling.");
    }

    [TestMethod]
    public void OptionList_ScrollViewer_Offset_Updates_Rendered_Viewport()
    {
        var list = new OptionList<OptionListItem> { MinHeight = 3, MaxHeight = 3 };
        for (var i = 0; i < 20; i++)
        {
            list.Items.Add(new OptionListItem($"Item {i:00}"));
        }

        var scrollViewer = new ScrollViewer(list) { MinHeight = 3, MaxHeight = 3 };
        var root = new VStack(scrollViewer).Spacing(0);
        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(30, 8));
        driver.Tick();

        scrollViewer.VerticalOffset = 5;
        driver.Tick();

        var screen = new AnsiTestScreen(30, 8);
        screen.Apply(driver.Backend.GetOutText());
        var rendered = screen.GetText();

        Assert.IsFalse(rendered.Contains("Item 00", StringComparison.Ordinal), "Expected the viewport to scroll past the first item.");
        StringAssert.Contains(rendered, "Item 05", "Expected the scrolled item to be visible.");
    }

    [TestMethod]
    public void OptionList_Scrolls_Tall_Items_Without_Hiding_Last_Row()
    {
        var list = new OptionList<OptionListItem> { MinHeight = 6, MaxHeight = 6 };
        for (var i = 0; i < 7; i++)
        {
            list.Items.Add(new OptionListItem($"Item {i:00}")
            {
                Description = $"Description {i:00}",
            });
        }

        var scrollViewer = new ScrollViewer(list) { MinHeight = 6, MaxHeight = 6 };
        var root = new VStack { scrollViewer };
        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(40, 12));
        driver.Tick();

        // With 2-row items and a 6-row viewport, exactly 3 items fit.
        var itemHeight = list.Scroll.ExtentHeight / list.Items.Count;
        Assert.AreEqual(2, itemHeight, "Expected 2-row items.");
        Assert.AreEqual(6, list.Scroll.ViewportHeight);

        // Selecting the 4th item should scroll by exactly one item (2 rows).
        list.SelectedIndex = 3;
        driver.Tick();
        Assert.AreEqual(2, list.Scroll.OffsetY);
    }
}
