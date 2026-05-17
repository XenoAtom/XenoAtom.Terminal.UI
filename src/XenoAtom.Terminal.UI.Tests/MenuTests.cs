// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Reflection;
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
    public void MenuBar_OpenMenu_Can_Be_Called_From_App_Defined_Shortcut()
    {
        var file = new MenuItem("File");
        file.Items.Add(new MenuItem("Open"));

        var bar = new MenuBar();
        bar.Items.Add(file);

        var button = new Button("Body");
        var root = new VStack { bar, button };
        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(50, 12));
        driver.App.Focus(button);
        driver.App.AddGlobalCommand(new Command
        {
            Id = "Test.OpenMenu",
            LabelMarkup = "Open menu",
            Gesture = new KeyGesture(TerminalKey.F9),
            Execute = _ => bar.OpenMenu(),
        });
        driver.Tick();

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.F9 });
        driver.TickUntil(() => driver.App.Root.EnumerateVisualsDepthFirst().OfType<Popup>().Any());

        var screen = new AnsiTestScreen(50, 12);
        screen.Apply(driver.Backend.GetOutText());
        StringAssert.Contains(screen.GetText(), "Open");
    }

    [TestMethod]
    public void MenuBar_Closing_Menu_Restores_Previous_Focus()
    {
        var file = new MenuItem("File");
        file.Items.Add(new MenuItem("Open"));

        var bar = new MenuBar();
        bar.Items.Add(file);

        var textBox = new TextBox("Body");
        var root = new VStack { bar, textBox };
        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(50, 12));
        driver.App.Focus(textBox);
        driver.App.AddGlobalCommand(new Command
        {
            Id = "Test.OpenMenu",
            LabelMarkup = "Open menu",
            Gesture = new KeyGesture(TerminalKey.F9),
            Execute = _ => bar.OpenMenu(),
        });
        driver.Tick();

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.F9 });
        driver.TickUntil(() => driver.App.Root.EnumerateVisualsDepthFirst().OfType<Popup>().Any());

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Escape });
        driver.TickUntil(() => !driver.App.Root.EnumerateVisualsDepthFirst().OfType<Popup>().Any());

        Assert.AreSame(textBox, driver.App.FocusedElement);
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
    public void MenuBar_Escape_FromSubmenu_Closes_Only_That_Submenu()
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

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Enter });
        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Right });
        driver.Tick();
        Assert.AreEqual(2, driver.App.Root.EnumerateVisualsDepthFirst().OfType<Popup>().Count());

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Escape });
        driver.Tick();

        Assert.AreEqual(1, driver.App.Root.EnumerateVisualsDepthFirst().OfType<Popup>().Count(), "Escape from a submenu should close one menu level.");

        var screen = new AnsiTestScreen(60, 14);
        screen.Apply(driver.Backend.GetOutText());
        var rendered = screen.GetText();
        StringAssert.Contains(rendered, "Recent");
        Assert.IsFalse(rendered.Contains("Entry 1", StringComparison.Ordinal));
    }

    [TestMethod]
    public void MenuBar_Hovering_TopLevel_Item_Updates_Keyboard_Selected_Item()
    {
        var bar = new MenuBar();
        bar.Items.Add(new MenuItem("File"));
        bar.Items.Add(new MenuItem("Edit"));

        var root = new VStack { bar };
        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(40, 6));
        driver.Tick();

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Right });
        driver.Tick();
        Assert.AreEqual(1, GetSelectedIndex(bar));

        var screen = new AnsiTestScreen(40, 6);
        screen.Apply(driver.Backend.GetOutText());
        var filePoint = FindFirstTextPosition(screen, "File");
        Assert.IsNotNull(filePoint);

        driver.Backend.PushEvent(new TerminalMouseEvent
        {
            Kind = TerminalMouseKind.Move,
            X = filePoint.Value.X,
            Y = filePoint.Value.Y,
        });
        driver.Tick();

        Assert.AreEqual(0, GetSelectedIndex(bar), "Hovering another top-level item should move the keyboard selection off the previously focused item.");
    }

    [TestMethod]
    public void MenuBar_Left_Right_Navigation_Rerenders_Selected_TopLevel_Item()
    {
        var bar = new MenuBar();
        bar.Items.Add(new MenuItem("File"));
        bar.Items.Add(new MenuItem("Help"));

        var root = new VStack { bar };
        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(40, 6));
        driver.Tick();

        var initialOutputLength = driver.Backend.GetOutText().Length;

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Right });
        driver.TickUntil(() => GetSelectedIndex(bar) == 1);

        var afterRightOutputLength = driver.Backend.GetOutText().Length;
        Assert.IsGreaterThan(initialOutputLength, afterRightOutputLength, "Right-arrow navigation should repaint the selected top-level menu item.");

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Left });
        driver.TickUntil(() => GetSelectedIndex(bar) == 0);

        Assert.IsGreaterThan(afterRightOutputLength, driver.Backend.GetOutText().Length, "Left-arrow navigation should repaint the selected top-level menu item.");
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
    public void MenuBar_Hovering_Another_TopLevel_Item_Switches_Open_Menu()
    {
        var file = new MenuItem("File");
        var recent = new MenuItem("Recent");
        recent.Items.Add(new MenuItem("File Only"));
        file.Items.Add(recent);

        var edit = new MenuItem("Edit");
        edit.Items.Add(new MenuItem("Edit Only"));

        var bar = new MenuBar();
        bar.Items.Add(file);
        bar.Items.Add(edit);

        var root = new VStack { bar };
        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(60, 14));
        driver.Tick();

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Enter }); // open File
        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Right }); // open Recent submenu
        driver.Tick();

        var withFileOpen = driver.Backend.GetOutText();
        var fileScreen = new AnsiTestScreen(60, 14);
        fileScreen.Apply(withFileOpen);
        StringAssert.Contains(fileScreen.GetText(), "File Only");

        var editPoint = FindFirstTextPosition(fileScreen, "Edit");
        Assert.IsNotNull(editPoint, "Expected to find the Edit menu header.");

        driver.Backend.PushEvent(new TerminalMouseEvent
        {
            Kind = TerminalMouseKind.Move,
            X = editPoint.Value.X,
            Y = editPoint.Value.Y,
        });
        driver.Tick();

        var afterHover = driver.Backend.GetOutText();
        var afterHoverScreen = new AnsiTestScreen(60, 14);
        afterHoverScreen.Apply(afterHover);
        var renderedAfterHover = afterHoverScreen.GetText();

        Assert.IsFalse(renderedAfterHover.Contains("File Only", StringComparison.Ordinal), "Hovering another top-level item should close the previous menu.");
        StringAssert.Contains(renderedAfterHover, "Edit Only");
        var remainingPopups = driver.App.Root.EnumerateVisualsDepthFirst().OfType<Popup>().Count();
        Assert.AreEqual(1, remainingPopups, "Switching top-level menus should not leave nested submenus open.");
    }

    [TestMethod]
    public void MenuBar_Clicking_Another_TopLevel_Item_Opens_It_With_A_Single_Click()
    {
        var file = new MenuItem("File");
        var recent = new MenuItem("Recent");
        recent.Items.Add(new MenuItem("File Only"));
        file.Items.Add(recent);

        var edit = new MenuItem("Edit");
        edit.Items.Add(new MenuItem("Edit Only"));

        var bar = new MenuBar();
        bar.Items.Add(file);
        bar.Items.Add(edit);

        var root = new VStack { bar };
        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(60, 14));
        driver.Tick();

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Enter }); // open File
        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Right }); // open Recent submenu
        driver.Tick();

        var withFileOpen = driver.Backend.GetOutText();
        var fileScreen = new AnsiTestScreen(60, 14);
        fileScreen.Apply(withFileOpen);
        StringAssert.Contains(fileScreen.GetText(), "File Only");

        var editPoint = FindFirstTextPosition(fileScreen, "Edit");
        Assert.IsNotNull(editPoint, "Expected to find the Edit menu header.");

        driver.Backend.PushEvent(new TerminalMouseEvent
        {
            Kind = TerminalMouseKind.Down,
            Button = TerminalMouseButton.Left,
            X = editPoint.Value.X,
            Y = editPoint.Value.Y,
        });
        driver.Backend.PushEvent(new TerminalMouseEvent
        {
            Kind = TerminalMouseKind.Up,
            Button = TerminalMouseButton.Left,
            X = editPoint.Value.X,
            Y = editPoint.Value.Y,
        });
        driver.Tick();

        var afterClick = driver.Backend.GetOutText();
        var afterClickScreen = new AnsiTestScreen(60, 14);
        afterClickScreen.Apply(afterClick);
        var renderedAfterClick = afterClickScreen.GetText();

        Assert.IsFalse(renderedAfterClick.Contains("File Only", StringComparison.Ordinal), "Clicking another top-level item should replace the currently open menu.");
        StringAssert.Contains(renderedAfterClick, "Edit Only");
        var remainingPopups = driver.App.Root.EnumerateVisualsDepthFirst().OfType<Popup>().Count();
        Assert.AreEqual(1, remainingPopups, "Switching top-level menus should close any deeper submenu chain.");
    }

    [TestMethod]
    public void MenuBar_Clicking_Active_TopLevel_Item_With_Submenu_Open_Keeps_Root_Menu_Open()
    {
        var file = new MenuItem("File");
        var recent = new MenuItem("Recent");
        recent.Items.Add(new MenuItem("Entry 1"));
        file.Items.Add(recent);

        var edit = new MenuItem("Edit");
        edit.Items.Add(new MenuItem("Edit Only"));

        var bar = new MenuBar();
        bar.Items.Add(file);
        bar.Items.Add(edit);

        var root = new VStack { bar };
        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(60, 14));
        driver.Tick();

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Enter }); // open File
        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Right }); // open Recent submenu
        driver.Tick();

        var withSubmenu = driver.Backend.GetOutText();
        var screen = new AnsiTestScreen(60, 14);
        screen.Apply(withSubmenu);
        StringAssert.Contains(screen.GetText(), "Entry 1");

        var filePoint = FindFirstTextPosition(screen, "File");
        Assert.IsNotNull(filePoint, "Expected to find the File menu header.");

        driver.Backend.PushEvent(new TerminalMouseEvent
        {
            Kind = TerminalMouseKind.Down,
            Button = TerminalMouseButton.Left,
            X = filePoint.Value.X,
            Y = filePoint.Value.Y,
        });
        driver.Backend.PushEvent(new TerminalMouseEvent
        {
            Kind = TerminalMouseKind.Up,
            Button = TerminalMouseButton.Left,
            X = filePoint.Value.X,
            Y = filePoint.Value.Y,
        });
        driver.Tick();

        var afterClick = driver.Backend.GetOutText();
        var afterClickScreen = new AnsiTestScreen(60, 14);
        afterClickScreen.Apply(afterClick);
        var renderedAfterClick = afterClickScreen.GetText();

        Assert.IsFalse(renderedAfterClick.Contains("Entry 1", StringComparison.Ordinal), "Clicking the active top-level item should close the child submenu.");
        StringAssert.Contains(renderedAfterClick, "Recent");
        var remainingPopups = driver.App.Root.EnumerateVisualsDepthFirst().OfType<Popup>().Count();
        Assert.AreEqual(1, remainingPopups, "The root menu should remain open.");
    }

    [TestMethod]
    public void MenuBar_Clicking_Submenu_Background_Closes_Only_Child_Submenu()
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

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Enter }); // open File
        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Right }); // open Recent submenu
        driver.Tick();

        var withSubmenu = driver.Backend.GetOutText();
        var screen = new AnsiTestScreen(60, 14);
        screen.Apply(withSubmenu);
        StringAssert.Contains(screen.GetText(), "Entry 1");

        var recentPoint = FindFirstTextPosition(screen, "Recent");
        Assert.IsNotNull(recentPoint, "Expected to find the Recent row.");

        driver.Backend.PushEvent(new TerminalMouseEvent
        {
            Kind = TerminalMouseKind.Down,
            Button = TerminalMouseButton.Left,
            X = Math.Max(0, recentPoint.Value.X - 1),
            Y = Math.Max(0, recentPoint.Value.Y - 1),
        });
        driver.Backend.PushEvent(new TerminalMouseEvent
        {
            Kind = TerminalMouseKind.Up,
            Button = TerminalMouseButton.Left,
            X = Math.Max(0, recentPoint.Value.X - 1),
            Y = Math.Max(0, recentPoint.Value.Y - 1),
        });
        driver.Tick();

        var afterClick = driver.Backend.GetOutText();
        var afterClickScreen = new AnsiTestScreen(60, 14);
        afterClickScreen.Apply(afterClick);
        var renderedAfterClick = afterClickScreen.GetText();

        Assert.IsFalse(renderedAfterClick.Contains("Entry 1", StringComparison.Ordinal), "Clicking submenu background should close the child submenu.");
        StringAssert.Contains(renderedAfterClick, "Recent");
        var remainingPopups = driver.App.Root.EnumerateVisualsDepthFirst().OfType<Popup>().Count();
        Assert.AreEqual(1, remainingPopups, "The clicked submenu should remain open.");
    }

    [TestMethod]
    public void MenuBar_Clicking_Parent_Popup_Chrome_Keeps_Parent_Open()
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

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Enter }); // open File
        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Right }); // open Recent submenu
        driver.Tick();

        var withSubmenu = driver.Backend.GetOutText();
        var screen = new AnsiTestScreen(60, 14);
        screen.Apply(withSubmenu);
        StringAssert.Contains(screen.GetText(), "Entry 1");

        var recentPoint = FindFirstTextPosition(screen, "Recent");
        Assert.IsNotNull(recentPoint, "Expected to find the Recent row.");

        driver.Backend.PushEvent(new TerminalMouseEvent
        {
            Kind = TerminalMouseKind.Down,
            Button = TerminalMouseButton.Left,
            X = Math.Max(0, recentPoint.Value.X - 1),
            Y = recentPoint.Value.Y,
        });
        driver.Backend.PushEvent(new TerminalMouseEvent
        {
            Kind = TerminalMouseKind.Up,
            Button = TerminalMouseButton.Left,
            X = Math.Max(0, recentPoint.Value.X - 1),
            Y = recentPoint.Value.Y,
        });
        driver.Tick();

        var afterClick = driver.Backend.GetOutText();
        var afterClickScreen = new AnsiTestScreen(60, 14);
        afterClickScreen.Apply(afterClick);
        var renderedAfterClick = afterClickScreen.GetText();

        Assert.IsFalse(renderedAfterClick.Contains("Entry 1", StringComparison.Ordinal), "Clicking inside parent popup chrome should close only the child submenu.");
        StringAssert.Contains(renderedAfterClick, "Recent");
        var remainingPopups = driver.App.Root.EnumerateVisualsDepthFirst().OfType<Popup>().Count();
        Assert.AreEqual(1, remainingPopups, "The parent submenu should remain open.");
    }

    [TestMethod]
    public void MenuBar_Clicking_Parent_MenuItem_Closes_Only_Child_Submenu()
    {
        var file = new MenuItem("File");
        var recent = new MenuItem("Recent");
        recent.Items.Add(new MenuItem("Entry 1"));
        file.Items.Add(recent);
        file.Items.Add(new MenuItem("Open"));

        var bar = new MenuBar();
        bar.Items.Add(file);

        var root = new VStack { bar };
        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(60, 14));
        driver.Tick();

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Enter }); // open File
        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Right }); // open Recent submenu
        driver.Tick();

        var withSubmenu = driver.Backend.GetOutText();
        var screen = new AnsiTestScreen(60, 14);
        screen.Apply(withSubmenu);
        StringAssert.Contains(screen.GetText(), "Entry 1");

        var openPoint = FindFirstTextPosition(screen, "Open");
        Assert.IsNotNull(openPoint, "Expected to find the Open row.");

        driver.Backend.PushEvent(new TerminalMouseEvent
        {
            Kind = TerminalMouseKind.Down,
            Button = TerminalMouseButton.Left,
            X = openPoint.Value.X,
            Y = openPoint.Value.Y,
        });
        driver.Backend.PushEvent(new TerminalMouseEvent
        {
            Kind = TerminalMouseKind.Up,
            Button = TerminalMouseButton.Left,
            X = openPoint.Value.X,
            Y = openPoint.Value.Y,
        });
        driver.Tick();

        var afterClick = driver.Backend.GetOutText();
        var afterClickScreen = new AnsiTestScreen(60, 14);
        afterClickScreen.Apply(afterClick);
        var renderedAfterClick = afterClickScreen.GetText();

        Assert.IsFalse(renderedAfterClick.Contains("Entry 1", StringComparison.Ordinal), "Clicking a parent menu item should first close the child submenu.");
        StringAssert.Contains(renderedAfterClick, "Open");
        var remainingPopups = driver.App.Root.EnumerateVisualsDepthFirst().OfType<Popup>().Count();
        Assert.AreEqual(1, remainingPopups, "The parent menu should remain open.");
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

    [TestMethod]
    public void MenuBar_Submenu_Opens_To_The_Right_Of_Parent_Menu_Surface()
    {
        var file = new MenuItem("File");
        var recent = new MenuItem("Recent");
        recent.Items.Add(new MenuItem("Entry 1"));
        file.Items.Add(recent);

        var bar = new MenuBar();
        bar.Items.Add(file);

        var root = new VStack { bar };
        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(80, 20));
        driver.Tick();

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Enter }); // open root menu
        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Right }); // open submenu
        driver.Tick();

        var popups = GetOpenMenuPopups(driver);
        Assert.AreEqual(2, popups.Length, "Expected the root menu and one submenu popup to be open.");
        Assert.AreEqual(
            popups[0].PopupRect.Right - 2,
            popups[1].PopupRect.X,
            "Submenus should align with the parent popup border connector area instead of shifting relative to the menu item text.");
    }

    [TestMethod]
    public void MenuBar_Deep_Submenus_Open_From_Each_Parent_Right_Border_Minus_Two()
    {
        var file = new MenuItem("File");
        var recent = new MenuItem("Recent");
        var workspaces = new MenuItem("Workspaces");
        workspaces.Items.Add(new MenuItem("Entry 1"));
        recent.Items.Add(workspaces);
        file.Items.Add(recent);

        var bar = new MenuBar();
        bar.Items.Add(file);

        var root = new VStack { bar };
        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(80, 20));
        driver.Tick();

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Enter }); // open root menu
        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Right }); // open first submenu
        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Right }); // open second submenu
        driver.Tick();

        var popups = GetOpenMenuPopups(driver);
        Assert.AreEqual(3, popups.Length, "Expected the root menu and two submenu popups to be open.");

        Assert.AreEqual(
            popups[0].PopupRect.Right - 2,
            popups[1].PopupRect.X,
            "The first submenu should open from the parent menu border connector area.");
        Assert.AreEqual(
            popups[1].PopupRect.Right - 2,
            popups[2].PopupRect.X,
            "Nested submenus should continue to open from the previous submenu border connector area.");
    }

    [TestMethod]
    public void MenuBar_Moving_Over_Parent_Submenu_Surface_Keeps_Deep_Chain_Open()
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
        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(80, 20));
        driver.Tick();

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Enter }); // open root menu
        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Right }); // open first submenu
        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Right }); // open second submenu
        driver.Tick();

        var popups = GetOpenMenuPopups(driver);
        Assert.AreEqual(3, popups.Length, "Expected the root menu plus two submenu levels to be open.");

        var parentSubmenuPopup = popups[1];
        driver.Backend.PushEvent(new TerminalMouseEvent
        {
            Kind = TerminalMouseKind.Move,
            X = parentSubmenuPopup.PopupRect.X,
            Y = Math.Min(parentSubmenuPopup.PopupRect.Bottom - 1, parentSubmenuPopup.PopupRect.Y + 1),
        });
        driver.Tick();

        Assert.AreEqual(3, GetOpenMenuPopups(driver).Length, "Hovering the parent submenu surface should not close that submenu or its descendants.");
    }

    [TestMethod]
    public void MenuBar_Clicking_Parent_Submenu_Surface_Closes_Only_Descendants_In_Deep_Chain()
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
        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(80, 20));
        driver.Tick();

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Enter }); // open root menu
        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Right }); // open first submenu
        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Right }); // open second submenu
        driver.Tick();

        var popups = GetOpenMenuPopups(driver);
        Assert.AreEqual(3, popups.Length, "Expected the root menu plus two submenu levels to be open.");

        var parentSubmenuPopup = popups[1];
        var clickX = parentSubmenuPopup.PopupRect.X;
        var clickY = Math.Min(parentSubmenuPopup.PopupRect.Bottom - 1, parentSubmenuPopup.PopupRect.Y + 1);

        driver.Backend.PushEvent(new TerminalMouseEvent
        {
            Kind = TerminalMouseKind.Down,
            Button = TerminalMouseButton.Left,
            X = clickX,
            Y = clickY,
        });
        driver.Backend.PushEvent(new TerminalMouseEvent
        {
            Kind = TerminalMouseKind.Up,
            Button = TerminalMouseButton.Left,
            X = clickX,
            Y = clickY,
        });
        driver.Tick();

        var remainingPopups = GetOpenMenuPopups(driver);
        Assert.AreEqual(2, remainingPopups.Length, "Clicking a parent submenu surface should close only descendant submenus.");

        var screen = new AnsiTestScreen(80, 20);
        screen.Apply(driver.Backend.GetOutText());
        var rendered = screen.GetText();
        StringAssert.Contains(rendered, "This Week");
        Assert.IsFalse(rendered.Contains("Entry 1", StringComparison.Ordinal), "The deepest submenu should be closed.");
    }

    [TestMethod]
    public void MenuBar_Moving_Over_Ancestor_Menu_Item_Closes_Only_Deeper_Submenus()
    {
        var file = new MenuItem("File");
        var recent = new MenuItem("Recent");
        var thisWeek = new MenuItem("This Week");
        thisWeek.Items.Add(new MenuItem("Entry 1"));
        recent.Items.Add(thisWeek);
        file.Items.Add(recent);
        file.Items.Add(new MenuItem("Open"));

        var bar = new MenuBar();
        bar.Items.Add(file);

        var root = new VStack { bar };
        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(80, 20));
        driver.Tick();

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Enter }); // open root menu
        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Right }); // open first submenu
        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Right }); // open second submenu
        driver.Tick();

        var screen = new AnsiTestScreen(80, 20);
        screen.Apply(driver.Backend.GetOutText());
        var openPoint = FindFirstTextPosition(screen, "Open");
        Assert.IsNotNull(openPoint, "Expected to find the ancestor menu item.");

        driver.Backend.PushEvent(new TerminalMouseEvent
        {
            Kind = TerminalMouseKind.Move,
            X = openPoint.Value.X,
            Y = openPoint.Value.Y,
        });
        driver.Tick();

        var remainingPopups = GetOpenMenuPopups(driver);
        Assert.AreEqual(1, remainingPopups.Length, "Moving over an ancestor item without submenu should only close deeper submenus and keep the ancestor menu open.");

        var afterMove = new AnsiTestScreen(80, 20);
        afterMove.Apply(driver.Backend.GetOutText());
        var rendered = afterMove.GetText();
        StringAssert.Contains(rendered, "Open");
        Assert.IsFalse(rendered.Contains("Entry 1", StringComparison.Ordinal), "The deepest submenu should be closed.");
    }

    [TestMethod]
    public void MenuBar_Moving_Over_Ancestor_Submenu_Item_Does_Not_Close_Its_Menu_Level()
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
        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(80, 20));
        driver.Tick();

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Enter }); // open root menu
        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Right }); // open first submenu
        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Right }); // open second submenu
        driver.Tick();

        var screen = new AnsiTestScreen(80, 20);
        screen.Apply(driver.Backend.GetOutText());
        var recentPoint = FindFirstTextPosition(screen, "Recent");
        Assert.IsNotNull(recentPoint, "Expected to find the ancestor submenu item.");

        driver.Backend.PushEvent(new TerminalMouseEvent
        {
            Kind = TerminalMouseKind.Move,
            X = recentPoint.Value.X,
            Y = recentPoint.Value.Y,
        });
        driver.Tick();

        Assert.IsTrue(
            GetOpenMenuPopups(driver).Length >= 2,
            "Moving over the ancestor submenu item should not close that submenu level.");
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

    private static Popup[] GetOpenMenuPopups(TerminalAppTestDriver driver)
        => driver.App.Root.EnumerateVisualsDepthFirst()
            .OfType<Popup>()
            .OrderBy(popup => popup.PopupRect.X)
            .ThenBy(popup => popup.PopupRect.Y)
            .ToArray();

    private static int GetSelectedIndex(MenuBar bar)
        => (int)typeof(MenuBar)
            .GetProperty("SelectedIndex", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(bar)!;
}
