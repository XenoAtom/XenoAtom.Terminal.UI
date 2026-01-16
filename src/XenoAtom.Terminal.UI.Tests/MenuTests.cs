// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.Backends;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Hosting;

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
}
