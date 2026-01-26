// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Hosting;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class SelectionListTests
{
    [TestMethod]
    public void SelectionList_Space_Toggles_Checked_Item()
    {
        var list = new SelectionList<string> { MinHeight = 4, MaxHeight = 4 };
        for (var i = 0; i < 6; i++)
        {
            list.AddItem($"Item {i}");
        }

        var root = new VStack { list };
        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(40, 10));
        driver.Tick();

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Down });
        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Space });
        driver.TickUntil(() => list.Checked[1]);

        Assert.IsTrue(list.Checked[1]);
    }

    [TestMethod]
    public void SelectionList_Scrolling_Keeps_Selected_Row_Visible()
    {
        var list = new SelectionList<string> { MinHeight = 4, MaxHeight = 4 };
        for (var i = 0; i < 10; i++)
        {
            list.AddItem($"Item {i}");
        }

        var root = new VStack { list };
        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(20, 6));
        driver.Tick();

        for (var i = 0; i < 6; i++)
        {
            driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Down });
        }
        driver.TickUntil(() => list.SelectedIndex == 6);

        var outText = driver.Backend.GetOutText();
        var screen = new AnsiTestScreen(20, 6);
        screen.Apply(outText);
        var rendered = screen.GetText();

        StringAssert.Contains(rendered, "Item 6");
        Assert.IsFalse(rendered.Contains("Item 0", StringComparison.Ordinal), "After scrolling down, Item 0 should no longer be visible in the viewport.");
    }

    [TestMethod]
    public void SelectionList_Scrolls_Rendered_Viewport_When_Selection_Moves_Inside_ScrollViewer()
    {
        var list = new SelectionList<string> { MinHeight = 3, MaxHeight = 3 };
        for (var i = 0; i < 7; i++)
        {
            list.AddItem($"Item {i:00}");
        }

        var scrollViewer = new ScrollViewer(list) { MinHeight = 3, MaxHeight = 3 };
        var root = new VStack(scrollViewer).Spacing(0);
        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(30, 8));
        driver.Tick();

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Down });
        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Down });
        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Down });
        driver.Tick();

        var screen = new AnsiTestScreen(30, 8);
        screen.Apply(driver.Backend.GetOutText());
        var rendered = screen.GetText();

        Assert.IsFalse(rendered.Contains("Item 00", StringComparison.Ordinal), "Expected the viewport to scroll past the first item.");
        StringAssert.Contains(rendered, "Item 03", "Expected the selected item to be visible after scrolling.");
    }
}
