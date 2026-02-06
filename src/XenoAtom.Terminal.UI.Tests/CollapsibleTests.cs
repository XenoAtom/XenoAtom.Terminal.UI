// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Hosting;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class CollapsibleTests
{
    [TestMethod]
    public void Collapsible_Toggles_On_Enter_Key()
    {
        var collapsible = new Collapsible("Header", "Body") { IsExpanded = false };

        using var driver = new TerminalAppTestDriver(collapsible, TerminalHostKind.Fullscreen, new TerminalSize(30, 8));
        driver.Tick();

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Enter });
        driver.Tick();

        Assert.IsTrue(collapsible.IsExpanded);
    }

    [TestMethod]
    public void Collapsible_Toggles_On_Header_Click()
    {
        var collapsible = new Collapsible("Header", "Body") { IsExpanded = false };

        using var driver = new TerminalAppTestDriver(collapsible, TerminalHostKind.Fullscreen, new TerminalSize(30, 8));
        driver.Tick();

        var x = collapsible.Bounds.X + 1;
        var y = collapsible.Bounds.Y;

        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Down, Button = TerminalMouseButton.Left, X = x, Y = y });
        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Up, Button = TerminalMouseButton.Left, X = x, Y = y });
        driver.Tick();

        Assert.IsTrue(collapsible.IsExpanded);
    }
}
