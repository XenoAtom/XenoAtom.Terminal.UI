// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Reflection;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Hosting;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class TooltipTests
{
    [TestMethod]
    public void Tooltip_Preserves_Content_Alignment()
    {
        var textBox = new TextBox("Type here").HorizontalAlignment(Align.Stretch);
        var root = new VStack(textBox.Tooltip("Tooltip text"));

        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(30, 10));
        driver.Tick();

        Assert.AreEqual(30, textBox.Bounds.Width);
    }

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

    [TestMethod]
    public void Tooltip_Closes_When_Clicking_And_Does_Not_Stick_On_Leave()
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

        var screen = new AnsiTestScreen(30, 10);
        screen.Apply(driver.Backend.GetOutText());
        StringAssert.Contains(screen.GetText(), "Tooltip text");

        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Down, Button = TerminalMouseButton.Left, X = x, Y = y });
        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Up, Button = TerminalMouseButton.Left, X = x, Y = y });
        driver.TickUntil(() => clicks == 1);

        screen = new AnsiTestScreen(30, 10);
        screen.Apply(driver.Backend.GetOutText());
        Assert.DoesNotContain("Tooltip text", screen.GetText(), "Clicking a tooltip host should dismiss the tooltip before hover changes.");

        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Move, Button = TerminalMouseButton.None, X = 29, Y = 9 });
        driver.Tick(2);

        screen = new AnsiTestScreen(30, 10);
        screen.Apply(driver.Backend.GetOutText());
        Assert.DoesNotContain("Tooltip text", screen.GetText(), "Leaving after a click should not leave the tooltip stuck open.");
    }

    [TestMethod]
    public void Tooltip_Anchored_Inside_Dialog_Closes_With_Owner()
    {
        var button = new Button("OK");
        var dialog = new Dialog
        {
            Title = "Dialog",
            Width = 24,
            Height = 8,
            Left = 10,
            Top = 1,
            Content = new Padder(button).Padding(new Thickness(2, 1, 0, 0)),
        };

        using var driver = new TerminalAppTestDriver(new VStack(), TerminalHostKind.Fullscreen, new TerminalSize(50, 20));
        driver.Tick();

        dialog.Show();
        driver.Tick();

        var tooltipWindow = new TooltipWindow
        {
            Anchor = button,
            Content = new TextBlock("Tooltip text"),
            Placement = PopupPlacement.Below,
            OffsetY = 1,
        };

        driver.App.ShowTooltipWindow(tooltipWindow);
        driver.Tick();

        var screen = new AnsiTestScreen(50, 20);
        screen.Apply(driver.Backend.GetOutText());
        StringAssert.Contains(screen.GetText(), "Tooltip text");

        dialog.Close();
        driver.Tick();

        Assert.IsNull(tooltipWindow.Parent, "Closing the owner dialog should detach the tooltip window from the window layer.");
    }
}

