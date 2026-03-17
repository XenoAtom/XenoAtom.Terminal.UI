// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Hosting;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class TabControlFeatureTests
{
    [TestMethod]
    public void TabControl_CloseButton_Removes_Tab_And_Raises_Events()
    {
        var closable = new TabPage("One", new TextBlock("A"))
        {
            ShowCloseButton = true,
        };

        var requestClosingCount = 0;
        var closedCount = 0;
        closable.RequestClosing += (_, e) =>
        {
            requestClosingCount++;
            Assert.AreEqual(TabCloseReason.CloseButton, e.Reason);
            Assert.AreEqual(0, e.Index);
        };
        closable.Closed += (_, e) =>
        {
            closedCount++;
            Assert.AreEqual(TabCloseReason.CloseButton, e.Reason);
            Assert.AreEqual(0, e.Index);
        };

        var tabs = new TabControl(
            closable,
            new TabPage("Two", new TextBlock("B")));

        using var driver = new TerminalAppTestDriver(tabs, TerminalHostKind.Fullscreen, new TerminalSize(30, 8));
        driver.Tick();

        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Down, Button = TerminalMouseButton.Left, X = 8, Y = 0 });
        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Up, Button = TerminalMouseButton.Left, X = 8, Y = 0 });
        driver.TickUntil(() => tabs.Tabs.Count == 1);

        Assert.AreEqual(1, requestClosingCount);
        Assert.AreEqual(1, closedCount);

        var screen = new AnsiTestScreen(30, 8);
        screen.Apply(driver.Backend.GetOutText());
        StringAssert.Contains(screen.GetText(), "Two");
    }

    [TestMethod]
    public void TabControl_Cancelled_Close_Request_Leaves_Tab_Open()
    {
        var closable = new TabPage("One", new TextBlock("A"))
        {
            ShowCloseButton = true,
        };

        closable.RequestClosing += (_, e) => e.Cancel = true;

        var tabs = new TabControl(
            closable,
            new TabPage("Two", new TextBlock("B")));

        using var driver = new TerminalAppTestDriver(tabs, TerminalHostKind.Fullscreen, new TerminalSize(30, 8));
        driver.Tick();

        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Down, Button = TerminalMouseButton.Left, X = 8, Y = 0 });
        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Up, Button = TerminalMouseButton.Left, X = 8, Y = 0 });
        driver.Tick();

        Assert.AreEqual(2, tabs.Tabs.Count);
    }

    [TestMethod]
    public void TabControl_Updates_When_TabPage_Header_And_Content_Change()
    {
        var page = new TabPage("Old", new TextBlock("OldContent"));
        var tabs = new TabControl(page);

        using var driver = new TerminalAppTestDriver(tabs, TerminalHostKind.Fullscreen, new TerminalSize(30, 8));
        driver.Tick();

        page.Header = new TextBlock("New");
        page.Content = new TextBlock("NewContent");
        page.ShowCloseButton = true;
        driver.Tick();

        var screen = new AnsiTestScreen(30, 8);
        screen.Apply(driver.Backend.GetOutText());
        var rendered = screen.GetText();

        StringAssert.Contains(rendered, "New");
        StringAssert.Contains(rendered, "NewContent");
    }

    [TestMethod]
    public void TabControl_Skips_Disabled_Tab_During_Interaction()
    {
        var tabs = new TabControl(
            new TabPage("One", new TextBlock("A")),
            new TabPage("Two", new TextBlock("B")) { IsEnabled = false },
            new TabPage("Three", new TextBlock("C")));

        using var driver = new TerminalAppTestDriver(tabs, TerminalHostKind.Fullscreen, new TerminalSize(40, 8));
        driver.Tick();

        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Down, Button = TerminalMouseButton.Left, X = 10, Y = 0 });
        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Up, Button = TerminalMouseButton.Left, X = 10, Y = 0 });
        driver.Tick();
        Assert.AreEqual(0, tabs.SelectedIndex, "Clicking a disabled tab should not select it.");

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Right });
        driver.TickUntil(() => tabs.SelectedIndex == 2);
    }

    [TestMethod]
    public void TabControl_Overflow_Buttons_Scroll_Visible_Window()
    {
        var tabs = new TabControl(
            new TabPage("Tab0", new TextBlock("A")),
            new TabPage("Tab1", new TextBlock("B")),
            new TabPage("Tab2", new TextBlock("C")),
            new TabPage("Tab3", new TextBlock("D")),
            new TabPage("Tab4", new TextBlock("E")));

        using var driver = new TerminalAppTestDriver(tabs, TerminalHostKind.Fullscreen, new TerminalSize(20, 8));
        driver.Tick();

        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Down, Button = TerminalMouseButton.Left, X = 19, Y = 0 });
        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Up, Button = TerminalMouseButton.Left, X = 19, Y = 0 });
        driver.TickUntil(() => tabs.FirstVisibleIndex == 1);

        var screen = new AnsiTestScreen(20, 8);
        screen.Apply(driver.Backend.GetOutText());
        var rendered = screen.GetText();
        StringAssert.Contains(rendered, "Tab1");
        Assert.IsFalse(rendered.Contains("Tab0", StringComparison.Ordinal));
    }
}
