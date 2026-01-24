// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Hosting;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class TabControlInteractionTests
{
    [TestMethod]
    public void TabControl_Changes_SelectedIndex_On_ArrowKeys()
    {
        var tabs = new TabControl();
        tabs.AddTab("First", new TextBlock("First"));
        tabs.AddTab("Second", new TextBlock("Second"));

        var root = new VStack();
        root.Add(tabs);

        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(30, 10));
        driver.Tick();

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Right });
        driver.TickUntil(() => tabs.SelectedIndex == 1);

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Left });
        driver.TickUntil(() => tabs.SelectedIndex == 0);
    }

    [TestMethod]
    public void TabControl_Switches_Content_On_Mouse_Click()
    {
        var tabs = new TabControl(
            new TabPage("Tab1", new TextBlock("ContentTab1")),
            new TabPage("Tab2", new TextBlock("ContentTab2")));

        using var driver = new TerminalAppTestDriver(tabs, TerminalHostKind.Fullscreen, new TerminalSize(30, 10));
        driver.Tick();

        {
            var screen = new AnsiTestScreen(30, 10);
            screen.Apply(driver.Backend.GetOutText());
            var rendered = screen.GetText();
            StringAssert.Contains(rendered, "ContentTab1");
        }

        // Click on the second tab header (x is approximate; header is at y=0).
        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Down, Button = TerminalMouseButton.Left, X = 12, Y = 0 });
        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Up, Button = TerminalMouseButton.Left, X = 12, Y = 0 });
        driver.TickUntil(() => tabs.SelectedIndex == 1);

        var finalScreen = new AnsiTestScreen(30, 10);
        finalScreen.Apply(driver.Backend.GetOutText());
        var finalRendered = finalScreen.GetText();
        StringAssert.Contains(finalRendered, "ContentTab2");
    }

    [TestMethod]
    public void TabControl_Supports_Visual_Headers()
    {
        var tabs = new TabControl(
            new TabPage(new TextBlock("H1"), new TextBlock("C1")),
            new TabPage(new TextBlock("H2"), new TextBlock("C2")));

        tabs.Measure(new Size(20, 5));
        tabs.Arrange(new Rectangle(0, 0, 20, 5));

        Assert.AreEqual(0, tabs.SelectedIndex);
    }

    [TestMethod]
    public void TabControl_Sets_Bounds_On_Arrange()
    {
        var tabControl = new TabControl(
            new TabPage("One", new TextBlock("A")),
            new TabPage("Two", new TextBlock("B")))
            .HorizontalAlignment(Align.Stretch)
            .VerticalAlignment(Align.Stretch);

        tabControl.Measure(new Size(80, 25));
        tabControl.Arrange(new Rectangle(0, 0, 80, 25));

        Assert.AreEqual(new Rectangle(0, 0, 80, 25), tabControl.Bounds);
    }
}
