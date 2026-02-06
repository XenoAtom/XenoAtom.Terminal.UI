// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.Backends;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Commands;
using XenoAtom.Terminal.UI.Hosting;
using XenoAtom.Terminal.UI.Input;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class MenuTests
{
    [TestMethod]
    public void MenuBar_Enter_Invokes_Menu_Item_Action()
    {
        var invoked = false;

        var file = new MenuItem("File");
        file.Items.Add(new MenuItem("Open", () => invoked = true));

        var bar = new MenuBar();
        bar.Items.Add(file);

        var root = new VStack { bar };
        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(50, 12));
        driver.Tick();

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Enter }); // open
        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Enter }); // invoke first item
        driver.TickUntil(() => invoked);
    }

    [TestMethod]
    public void MenuBar_Right_Opens_Submenu_And_Invokes_Action()
    {
        var invoked = false;

        var file = new MenuItem("File");
        var recent = new MenuItem("Recent");
        recent.Items.Add(new MenuItem("Entry 1", () => invoked = true));
        file.Items.Add(recent);

        var bar = new MenuBar();
        bar.Items.Add(file);

        var root = new VStack { bar };
        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(60, 14));
        driver.Tick();

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Enter }); // open root menu
        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Right }); // open submenu
        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Enter }); // invoke submenu item
        driver.TickUntil(() => invoked);
    }

    [TestMethod]
    public void MenuBar_Left_Closes_Submenu()
    {
        var file = new MenuItem("File");
        var recent = new MenuItem("Recent");
        recent.Items.Add(new MenuItem("Entry 1"));
        file.Items.Add(recent);

        var bar = new MenuBar();
        bar.Items.Add(file);

        var root = new VStack { bar };
        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(60, 14));
        driver.Tick();

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Enter }); // open root menu
        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Right }); // open submenu
        driver.Tick();

        var withSubmenu = driver.Backend.GetOutText();
        var screen = new AnsiTestScreen(60, 14);
        screen.Apply(withSubmenu);
        var rendered = screen.GetText();
        StringAssert.Contains(rendered, "Entry 1");

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Left }); // close submenu
        driver.Tick();

        var withoutSubmenu = driver.Backend.GetOutText();
        var screen2 = new AnsiTestScreen(60, 14);
        screen2.Apply(withoutSubmenu);
        var rendered2 = screen2.GetText();
        Assert.IsFalse(rendered2.Contains("Entry 1", StringComparison.Ordinal), "Closing the submenu should remove its content from the screen.");
    }

    [TestMethod]
    public void MenuBar_Left_FromDeepSubmenu_ClosesOnlyOneLevel()
    {
        var file = new MenuItem("File");
        var recent = new MenuItem("Recent");
        var thisWeek = new MenuItem("This Week");
        thisWeek.Items.Add(new MenuItem("Entry 1"));
        recent.Items.Add(thisWeek);
        file.Items.Add(recent);

        var bar = new MenuBar();
        bar.Items.Add(file);

        var root = new VStack { bar };
        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(60, 14));
        driver.Tick();

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Enter }); // open root menu
        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Right }); // open submenu level 2
        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Right }); // open submenu level 3
        driver.Tick();

        var withDeepSubmenu = driver.Backend.GetOutText();
        var screen = new AnsiTestScreen(60, 14);
        screen.Apply(withDeepSubmenu);
        var rendered = screen.GetText();
        StringAssert.Contains(rendered, "Entry 1");

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Left }); // close only deepest submenu
        driver.Tick();

        var afterLeft = driver.Backend.GetOutText();
        var screenAfterLeft = new AnsiTestScreen(60, 14);
        screenAfterLeft.Apply(afterLeft);
        var renderedAfterLeft = screenAfterLeft.GetText();

        Assert.IsFalse(renderedAfterLeft.Contains("Entry 1", StringComparison.Ordinal), "Left should close only the deepest submenu.");
        StringAssert.Contains(renderedAfterLeft, "This Week");
        var remainingPopups = driver.App.Root.EnumerateVisualsDepthFirst().OfType<Popup>().Count();
        Assert.AreEqual(2, remainingPopups, "Root and parent submenu should remain open.");
    }

    [TestMethod]
    public void MenuBar_WhenOpen_GlobalCommandDoesNotExecuteBehindPopup()
    {
        var globalInvoked = false;

        var file = new MenuItem("File");
        file.Items.Add(new MenuItem("Open"));

        var bar = new MenuBar();
        bar.Items.Add(file);

        var root = new VStack { bar };
        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(60, 14));
        driver.App.AddGlobalCommand(new Command
        {
            Id = "Test.Global",
            LabelMarkup = "Global",
            Gesture = new KeyGesture(TerminalChar.CtrlG, TerminalModifiers.Ctrl),
            Execute = _ => globalInvoked = true,
        });

        driver.Tick();

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Enter }); // open root menu
        driver.Tick();

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Unknown, Char = TerminalChar.CtrlG, Modifiers = TerminalModifiers.Ctrl });
        driver.Tick(2);

        Assert.IsFalse(globalInvoked);
    }

    [TestMethod]
    public void MenuBar_OutsideClick_Closes_All_Open_Submenus()
    {
        var file = new MenuItem("File");
        var recent = new MenuItem("Recent");
        recent.Items.Add(new MenuItem("Entry 1"));
        file.Items.Add(recent);

        var bar = new MenuBar();
        bar.Items.Add(file);

        var root = new VStack { bar, new TextBlock("Outside Area") };
        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(60, 14));
        driver.Tick();

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Enter }); // open root menu
        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Right }); // open submenu
        driver.Tick();

        var withSubmenu = driver.Backend.GetOutText();
        var screen = new AnsiTestScreen(60, 14);
        screen.Apply(withSubmenu);
        var rendered = screen.GetText();
        StringAssert.Contains(rendered, "Entry 1");

        driver.Backend.PushEvent(new TerminalMouseEvent
        {
            Kind = TerminalMouseKind.Down,
            Button = TerminalMouseButton.Left,
            X = 0,
            Y = 13,
        });
        driver.Backend.PushEvent(new TerminalMouseEvent
        {
            Kind = TerminalMouseKind.Up,
            Button = TerminalMouseButton.Left,
            X = 0,
            Y = 13,
        });
        driver.Tick();

        var withoutMenus = driver.Backend.GetOutText();
        var screen2 = new AnsiTestScreen(60, 14);
        screen2.Apply(withoutMenus);
        var rendered2 = screen2.GetText();
        Assert.IsFalse(rendered2.Contains("Entry 1", StringComparison.Ordinal), "Outside click should close the entire submenu chain.");
        var remainingPopups = driver.App.Root.EnumerateVisualsDepthFirst().OfType<Popup>().Count();
        Assert.AreEqual(0, remainingPopups, "Outside click should close the top-level menu popup as well.");
    }

    [TestMethod]
    public void MenuBar_ClickOnParentMenu_ClosesOnlyChildSubmenu()
    {
        var file = new MenuItem("File");
        var recent = new MenuItem("Recent");
        recent.Items.Add(new MenuItem("Entry 1"));
        file.Items.Add(recent);

        var bar = new MenuBar();
        bar.Items.Add(file);

        var root = new VStack { bar };
        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(60, 14));
        driver.Tick();

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Enter }); // open root menu
        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Right }); // open submenu
        driver.Tick();

        var withSubmenu = driver.Backend.GetOutText();
        var screen = new AnsiTestScreen(60, 14);
        screen.Apply(withSubmenu);
        var rendered = screen.GetText();
        StringAssert.Contains(rendered, "Entry 1");

        var clickPoint = FindFirstTextPosition(screen, "Recent");
        Assert.IsNotNull(clickPoint, "Expected to find parent menu row text in the rendered output.");

        driver.Backend.PushEvent(new TerminalMouseEvent
        {
            Kind = TerminalMouseKind.Down,
            Button = TerminalMouseButton.Left,
            X = clickPoint.Value.X,
            Y = clickPoint.Value.Y,
        });
        driver.Backend.PushEvent(new TerminalMouseEvent
        {
            Kind = TerminalMouseKind.Up,
            Button = TerminalMouseButton.Left,
            X = clickPoint.Value.X,
            Y = clickPoint.Value.Y,
        });
        driver.Tick();

        var afterClick = driver.Backend.GetOutText();
        var screenAfterClick = new AnsiTestScreen(60, 14);
        screenAfterClick.Apply(afterClick);
        var renderedAfterClick = screenAfterClick.GetText();

        Assert.IsFalse(renderedAfterClick.Contains("Entry 1", StringComparison.Ordinal), "Clicking parent menu should close child submenu.");
        Assert.IsNotNull(FindFirstTextPosition(screenAfterClick, "Recent"), "Parent menu should remain open after closing child submenu.");
        var remainingPopups = driver.App.Root.EnumerateVisualsDepthFirst().OfType<Popup>().Count();
        Assert.AreEqual(1, remainingPopups, "Only the parent popup should remain open.");
    }

    private static (int X, int Y)? FindFirstTextPosition(AnsiTestScreen screen, string text)
    {
        var lines = screen.GetText().Split('\n');
        for (var y = 0; y < lines.Length; y++)
        {
            var x = lines[y].IndexOf(text, StringComparison.Ordinal);
            if (x >= 0)
            {
                return (x, y);
            }
        }

        return null;
    }
}
