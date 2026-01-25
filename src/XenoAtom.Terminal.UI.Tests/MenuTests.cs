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
}
