// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Reflection;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Hosting;
using XenoAtom.Terminal.UI.Styling;

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

        var closePoint = GetHeaderPoint(tabs, 0, closeButton: true);
        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Down, Button = TerminalMouseButton.Left, X = closePoint.X, Y = closePoint.Y });
        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Up, Button = TerminalMouseButton.Left, X = closePoint.X, Y = closePoint.Y });
        driver.TickUntil(() => tabs.Tabs.Count == 1);

        Assert.AreEqual(1, requestClosingCount);
        Assert.AreEqual(1, closedCount);

        var screen = new AnsiTestScreen(30, 8);
        screen.Apply(driver.Backend.GetOutText());
        StringAssert.Contains(screen.GetText(), "Two");
    }

    [TestMethod]
    public void TabControl_MiddleClick_Removes_Tab()
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
            Assert.AreEqual(TabCloseReason.MiddleClick, e.Reason);
            Assert.AreEqual(0, e.Index);
        };
        closable.Closed += (_, e) =>
        {
            closedCount++;
            Assert.AreEqual(TabCloseReason.MiddleClick, e.Reason);
            Assert.AreEqual(0, e.Index);
        };

        var tabs = new TabControl(
            closable,
            new TabPage("Two", new TextBlock("B")));

        using var driver = new TerminalAppTestDriver(tabs, TerminalHostKind.Fullscreen, new TerminalSize(30, 8));
        driver.Tick();

        var tabPoint = GetHeaderPoint(tabs, 0, closeButton: false);
        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Down, Button = TerminalMouseButton.Middle, X = tabPoint.X, Y = tabPoint.Y });
        driver.TickUntil(() => tabs.Tabs.Count == 1);

        Assert.AreEqual(1, requestClosingCount);
        Assert.AreEqual(1, closedCount);
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

        var closePoint = GetHeaderPoint(tabs, 0, closeButton: true);
        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Down, Button = TerminalMouseButton.Left, X = closePoint.X, Y = closePoint.Y });
        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Up, Button = TerminalMouseButton.Left, X = closePoint.X, Y = closePoint.Y });
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
    public void TabControl_SelectedIndex_Change_Raises_SelectionChanged()
    {
        var first = new TabPage("One", new TextBlock("A"));
        var second = new TabPage("Two", new TextBlock("B"));
        var tabs = new TabControl(first, second);

        TabSelectionChangedEventArgs? selectionChanged = null;
        tabs.SelectionChanged((_, e) => selectionChanged = e);

        using var driver = new TerminalAppTestDriver(tabs, TerminalHostKind.Fullscreen, new TerminalSize(30, 8));
        driver.Tick();

        tabs.SelectedIndex = 1;

        Assert.IsNotNull(selectionChanged, "Expected changing SelectedIndex to raise SelectionChanged.");
        Assert.AreEqual(0, selectionChanged.OldIndex);
        Assert.AreEqual(1, selectionChanged.NewIndex);
        Assert.AreSame(first, selectionChanged.OldPage);
        Assert.AreSame(second, selectionChanged.NewPage);
    }

    [TestMethod]
    public void TabControl_Closing_Selected_Tab_Raises_SelectionChanged_When_Page_Changes_At_Same_Index()
    {
        var first = new TabPage("One", new TextBlock("A"))
        {
            ShowCloseButton = true,
        };
        var second = new TabPage("Two", new TextBlock("B"));
        var tabs = new TabControl(first, second);

        TabSelectionChangedEventArgs? selectionChanged = null;
        tabs.SelectionChanged((_, e) => selectionChanged = e);

        using var driver = new TerminalAppTestDriver(tabs, TerminalHostKind.Fullscreen, new TerminalSize(30, 8));
        driver.Tick();

        Assert.IsTrue(tabs.TryCloseTab(0));

        Assert.IsNotNull(selectionChanged, "Expected closing the selected tab to raise SelectionChanged.");
        Assert.AreEqual(0, selectionChanged.OldIndex);
        Assert.AreEqual(0, selectionChanged.NewIndex);
        Assert.AreSame(first, selectionChanged.OldPage);
        Assert.AreSame(second, selectionChanged.NewPage);
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

    [TestMethod]
    public void TabControl_Selecting_Visible_Overflow_Tab_Preserves_Current_Window()
    {
        var tabs = new TabControl();
        for (var i = 0; i < 20; i++)
        {
            tabs.AddTab(new TabPage($"Tab-{i:00}", new TextBlock($"Content {i}")));
        }

        using var driver = new TerminalAppTestDriver(tabs, TerminalHostKind.Fullscreen, new TerminalSize(32, 8));
        driver.Tick();

        tabs.FirstVisibleIndex = 10;
        driver.Tick();

        var headerPoint = GetHeaderPoint(tabs, 11, closeButton: false);
        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Down, Button = TerminalMouseButton.Left, X = headerPoint.X, Y = headerPoint.Y });
        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Up, Button = TerminalMouseButton.Left, X = headerPoint.X, Y = headerPoint.Y });
        driver.Tick();

        Assert.AreEqual(11, tabs.SelectedIndex);
        Assert.AreEqual(10, tabs.FirstVisibleIndex, "Selecting an already visible overflow tab should not scroll the strip back to the left.");
    }

    [TestMethod]
    public void TabControl_Selecting_Partially_Visible_Tab_Scrolls_Tab_Fully_Into_View()
    {
        var tabs = new TabControl();
        for (var i = 0; i < 20; i++)
        {
            tabs.AddTab(new TabPage($"Tab-{i:00}", new TextBlock($"Content {i}")));
        }

        using var driver = new TerminalAppTestDriver(tabs, TerminalHostKind.Fullscreen, new TerminalSize(32, 8));
        driver.Tick();

        tabs.FirstVisibleIndex = 10;
        driver.Tick();

        var headerPoint = GetHeaderPoint(tabs, 12, closeButton: false);
        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Down, Button = TerminalMouseButton.Left, X = headerPoint.X, Y = headerPoint.Y });
        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Up, Button = TerminalMouseButton.Left, X = headerPoint.X, Y = headerPoint.Y });
        driver.TickUntil(() => tabs.SelectedIndex == 12 && tabs.FirstVisibleIndex == 11);

        Assert.AreEqual(12, tabs.SelectedIndex);
        Assert.AreEqual(11, tabs.FirstVisibleIndex, "Selecting a clipped overflow tab should scroll enough to show the full tab header.");
    }

    [TestMethod]
    public void TabControl_Keyboard_Selecting_Partially_Visible_Tab_Scrolls_Tab_Fully_Into_View()
    {
        var tabs = new TabControl();
        for (var i = 0; i < 20; i++)
        {
            tabs.AddTab(new TabPage($"Tab-{i:00}", new TextBlock($"Content {i}")));
        }

        using var driver = new TerminalAppTestDriver(tabs, TerminalHostKind.Fullscreen, new TerminalSize(32, 8));
        driver.Tick();

        tabs.SelectedIndex = 11;
        tabs.FirstVisibleIndex = 10;
        driver.Tick();

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Right });
        driver.TickUntil(() => tabs.SelectedIndex == 12 && tabs.FirstVisibleIndex == 11);

        Assert.AreEqual(12, tabs.SelectedIndex);
        Assert.AreEqual(11, tabs.FirstVisibleIndex, "Keyboard navigation should scroll enough to show the full selected tab header.");
    }

    [TestMethod]
    public void TabControl_MoveTab_Reorders_Pages_And_Preserves_Selected_Page()
    {
        var first = new TabPage("One", new TextBlock("A"));
        var second = new TabPage("Two", new TextBlock("B"));
        var third = new TabPage("Three", new TextBlock("C"));
        var tabs = new TabControl(first, second, third)
        {
            SelectedIndex = 1,
            FirstVisibleIndex = 1,
        };

        TabSelectionChangedEventArgs? selectionChanged = null;
        tabs.SelectionChanged((_, e) => selectionChanged = e);

        tabs.MoveTab(1, 2);

        Assert.AreSame(first, tabs.Tabs[0]);
        Assert.AreSame(third, tabs.Tabs[1]);
        Assert.AreSame(second, tabs.Tabs[2]);
        Assert.AreEqual(2, tabs.SelectedIndex);
        Assert.IsNotNull(selectionChanged);
        Assert.AreEqual(1, selectionChanged.OldIndex);
        Assert.AreEqual(2, selectionChanged.NewIndex);
        Assert.AreSame(second, selectionChanged.OldPage);
        Assert.AreSame(second, selectionChanged.NewPage);
    }

    [TestMethod]
    public void TabControl_Dragging_Header_Reorders_Pages_And_Preserves_Selected_Page()
    {
        var first = new TabPage("One", new TextBlock("A"));
        var second = new TabPage("Two", new TextBlock("B"));
        var third = new TabPage("Three", new TextBlock("C"));
        var tabs = new TabControl(first, second, third)
        {
            SelectedIndex = 2,
        };

        using var driver = new TerminalAppTestDriver(tabs, TerminalHostKind.Fullscreen, new TerminalSize(40, 8));
        driver.Tick();

        var dragStart = GetHeaderPoint(tabs, 0, closeButton: false);
        var dragEnd = GetHeaderPoint(tabs, 2, closeButton: false);

        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Down, Button = TerminalMouseButton.Left, X = dragStart.X, Y = dragStart.Y });
        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Drag, Button = TerminalMouseButton.Left, X = dragEnd.X, Y = dragEnd.Y });
        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Up, Button = TerminalMouseButton.Left, X = dragEnd.X, Y = dragEnd.Y });
        driver.TickUntil(() => ReferenceEquals(tabs.Tabs[2], first));

        Assert.AreSame(second, tabs.Tabs[0]);
        Assert.AreSame(third, tabs.Tabs[1]);
        Assert.AreSame(first, tabs.Tabs[2]);
        Assert.AreEqual(1, tabs.SelectedIndex);
        Assert.AreSame(third, tabs.Tabs[tabs.SelectedIndex]);
    }

    [TestMethod]
    public void TabControl_Dragging_Header_Does_Not_Reorder_When_Disabled()
    {
        var first = new TabPage("One", new TextBlock("A"));
        var second = new TabPage("Two", new TextBlock("B"));
        var third = new TabPage("Three", new TextBlock("C"));
        var tabs = new TabControl(first, second, third)
        {
            AllowTabDragReorder = false,
        };

        using var driver = new TerminalAppTestDriver(tabs, TerminalHostKind.Fullscreen, new TerminalSize(40, 8));
        driver.Tick();

        var dragStart = GetHeaderPoint(tabs, 0, closeButton: false);
        var dragEnd = GetHeaderPoint(tabs, 2, closeButton: false);

        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Down, Button = TerminalMouseButton.Left, X = dragStart.X, Y = dragStart.Y });
        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Drag, Button = TerminalMouseButton.Left, X = dragEnd.X, Y = dragEnd.Y });
        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Up, Button = TerminalMouseButton.Left, X = dragEnd.X, Y = dragEnd.Y });
        driver.Tick();

        Assert.AreSame(first, tabs.Tabs[0]);
        Assert.AreSame(second, tabs.Tabs[1]);
        Assert.AreSame(third, tabs.Tabs[2]);
    }

    [TestMethod]
    public void TabControl_Style_Switch_Rebuilds_Chrome_Without_Reparenting_Content()
    {
        var style = new State<TabControlStyle>(TabControlStyle.Default);
        var tabs = new TabControl(
                new TabPage("One", new TextBlock("A")),
                new TabPage("Two", new TextBlock("B")))
            .Style(style);

        using var driver = new TerminalAppTestDriver(tabs, TerminalHostKind.Fullscreen, new TerminalSize(30, 8));
        driver.Tick();

        style.Value = TabControlStyle.Rounded;
        driver.Tick();

        var screen = new AnsiTestScreen(30, 8);
        screen.Apply(driver.Backend.GetOutText());
        Assert.IsTrue(screen.GetText().Contains("A", StringComparison.Ordinal));

        style.Value = TabControlStyle.Compact;
        driver.Tick();

        screen = new AnsiTestScreen(30, 8);
        screen.Apply(driver.Backend.GetOutText());
        Assert.IsTrue(screen.GetText().Contains("A", StringComparison.Ordinal));
    }

    private static (int X, int Y) GetHeaderPoint(TabControl tabs, int index, bool closeButton)
    {
        var layouts = (System.Collections.IEnumerable)typeof(TabControl)
            .GetField("_headerLayouts", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(tabs)!;

        foreach (var layout in layouts)
        {
            var layoutType = layout.GetType();
            var candidateIndex = (int)layoutType.GetProperty("Index")!.GetValue(layout)!;
            if (candidateIndex != index)
            {
                continue;
            }

            if (closeButton)
            {
                var closeStart = (int)layoutType.GetProperty("CloseStart")!.GetValue(layout)!;
                var closeEnd = (int)layoutType.GetProperty("CloseEnd")!.GetValue(layout)!;
                return ((closeStart + closeEnd - 1) / 2, 1);
            }

            var start = (int)layoutType.GetProperty("Start")!.GetValue(layout)!;
            var end = (int)layoutType.GetProperty("End")!.GetValue(layout)!;
            return ((start + end - 1) / 2, 1);
        }

        throw new AssertFailedException($"Unable to resolve header layout for tab index {index}.");
    }
}
