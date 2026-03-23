// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Hosting;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class SelectTests
{
    [TestMethod]
    public void Select_Opens_And_Selects_Item_On_Click()
    {
        var select = new Select<string>()
            .Items(["First", "Second", "Third"]);

        var root = new VStack { select };

        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(40, 10));
        driver.Tick();

        // Click the select to open the popup.
        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Down, Button = TerminalMouseButton.Left, X = 1, Y = 0 });
        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Up, Button = TerminalMouseButton.Left, X = 1, Y = 0 });
        driver.Tick();

        // Click the second item in the popup list (roughly below the select).
        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Down, Button = TerminalMouseButton.Left, X = 2, Y = 3 });
        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Up, Button = TerminalMouseButton.Left, X = 2, Y = 3 });
        driver.TickUntil(() => select.SelectedIndex == 1);

        Assert.AreEqual(1, select.SelectedIndex);
    }

    [TestMethod]
    public void Select_IdleTick_DoesNotRebuild_SelectedContent()
    {
        var select = new Select<string>()
            .Items(["First", "Second", "Third"]);

        var root = new VStack { select };

        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(40, 10));
        driver.Tick();

        var initialContent = select.Content;
        Assert.IsNotNull(initialContent);

        driver.Tick();

        Assert.AreSame(initialContent, select.Content);
    }

    [TestMethod]
    public void Select_SelectedItemChange_Rebuilds_SelectedContent()
    {
        var select = new Select<string>()
            .Items(["First", "Second", "Third"]);

        var root = new VStack { select };

        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(40, 10));
        driver.Tick();

        var initialContent = select.Content;
        Assert.IsNotNull(initialContent);

        select.Items[0] = "Updated";
        driver.Tick();

        Assert.AreNotSame(initialContent, select.Content);
    }
}
