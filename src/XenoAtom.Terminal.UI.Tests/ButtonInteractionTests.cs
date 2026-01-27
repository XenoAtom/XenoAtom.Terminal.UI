// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Hosting;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class ButtonInteractionTests
{
    [TestMethod]
    public void Button_Raises_Click_On_Enter()
    {
        var button = new Button("OK");
        var clicked = false;
        button.Click((_, _) => clicked = true);

        var root = new VStack { button };
        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(20, 6));
        driver.Tick();

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Enter });
        driver.TickUntil(() => clicked);
    }

    [TestMethod]
    public void Button_Raises_Click_On_Mouse()
    {
        var button = new Button("OK");
        var clicked = false;
        button.Click((_, _) => clicked = true);

        var root = new VStack { button };
        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(20, 6));
        driver.Tick();

        var x = button.Bounds.X + 1;
        var y = button.Bounds.Y;
        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Down, Button = TerminalMouseButton.Left, X = x, Y = y });
        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Up, Button = TerminalMouseButton.Left, X = x, Y = y });

        driver.TickUntil(() => clicked);
    }

    [TestMethod]
    public void Button_Does_Not_Click_When_Released_Outside()
    {
        var button = new Button("OK");
        var clicked = false;
        button.Click((_, _) => clicked = true);

        var root = new VStack { button };
        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(20, 6));
        driver.Tick();

        var insideX = button.Bounds.X + 1;
        var insideY = button.Bounds.Y;
        var outsideX = button.Bounds.Right + 1;
        var outsideY = button.Bounds.Bottom + 1;

        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Down, Button = TerminalMouseButton.Left, X = insideX, Y = insideY });
        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Drag, Button = TerminalMouseButton.Left, X = outsideX, Y = outsideY });
        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Up, Button = TerminalMouseButton.Left, X = outsideX, Y = outsideY });
        driver.Tick();

        Assert.IsFalse(clicked);
    }

    [TestMethod]
    public void Button_Does_Not_Click_When_Disabled()
    {
        var button = new Button("OK") { IsEnabled = false };
        var clicked = false;
        button.Click((_, _) => clicked = true);

        var root = new VStack { button };
        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(20, 6));
        driver.Tick();

        var x = button.Bounds.X + 1;
        var y = button.Bounds.Y;
        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Down, Button = TerminalMouseButton.Left, X = x, Y = y });
        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Up, Button = TerminalMouseButton.Left, X = x, Y = y });
        driver.Tick();

        Assert.IsFalse(clicked);
    }
}
