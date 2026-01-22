// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Hosting;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class TooltipTests
{
    [TestMethod]
    public void Tooltip_Shows_After_Delay_And_Hides_On_Leave()
    {
        var button = new Button("OK");
        var root = new VStack
        {
            button.Tooltip("Tooltip text").ShowDelayMilliseconds(20)
        };

        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(30, 10));
        driver.Tick();

        var insideX = button.Bounds.X + 1;
        var insideY = button.Bounds.Y;
        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Move, Button = TerminalMouseButton.None, X = insideX, Y = insideY });
        driver.Tick(5); // 50ms

        var screen = new AnsiTestScreen(30, 10);
        screen.Apply(driver.Backend.GetOutText());
        StringAssert.Contains(screen.GetText(), "Tooltip text");

        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Move, Button = TerminalMouseButton.None, X = 29, Y = 9 });
        driver.Tick(2);

        screen = new AnsiTestScreen(30, 10);
        screen.Apply(driver.Backend.GetOutText());
        Assert.DoesNotContain("Tooltip text", screen.GetText());
    }

    [TestMethod]
    public void Tooltip_Does_Not_Intercept_Clicks()
    {
        var clicks = 0;
        var button = new Button("OK").Click(() => clicks++);
        var root = new VStack
        {
            button.Tooltip("Tooltip text").ShowDelayMilliseconds(0)
        };

        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(30, 10));
        driver.Tick();

        var x = button.Bounds.X + 1;
        var y = button.Bounds.Y;

        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Move, Button = TerminalMouseButton.None, X = x, Y = y });
        driver.Tick(2);

        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Down, Button = TerminalMouseButton.Left, X = x, Y = y });
        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Up, Button = TerminalMouseButton.Left, X = x, Y = y });
        driver.TickUntil(() => clicks == 1);
    }
}

