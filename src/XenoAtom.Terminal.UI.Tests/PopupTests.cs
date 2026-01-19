// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.Backends;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Hosting;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class PopupTests
{
    [TestMethod]
    public void Popup_Positions_Content_For_All_Placements()
    {
        var anchor = new Button("Anchor");
        var root = new Padder(anchor).Padding(new Thickness(10, 5, 0, 0));

        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(40, 16));
        driver.Tick();

        var anchorX = anchor.Bounds.X;
        var anchorY = anchor.Bounds.Y;
        var anchorRight = anchor.Bounds.Right;
        var anchorBottom = anchor.Bounds.Bottom;

        foreach (var placement in new[] { PopupPlacement.Below, PopupPlacement.Above, PopupPlacement.Right, PopupPlacement.Left })
        {
            var popup = new Popup
            {
                Anchor = anchor,
                Content = new TextBlock("P"),
                MatchAnchorWidth = false,
                Placement = placement,
            };

            driver.App.Post(popup.Show);
            driver.Tick();

            var outText = driver.Backend.GetOutText();
            var screen = new AnsiTestScreen(40, 16);
            screen.Apply(outText);
            var rendered = screen.GetText();

            var (expectedX, expectedY) = placement switch
            {
                PopupPlacement.Below => (anchorX, anchorBottom),
                PopupPlacement.Above => (anchorX, anchorY - 1),
                PopupPlacement.Right => (anchorRight, anchorY),
                PopupPlacement.Left => (anchorX - 1, anchorY),
                _ => throw new ArgumentOutOfRangeException(nameof(placement)),
            };

            Assert.AreEqual('P', GetChar(rendered, expectedX, expectedY), $"Placement: {placement}");

            driver.App.Post(popup.Close);
            driver.Tick();
        }
    }

    [TestMethod]
    public void Popup_Reused_Instance_Updates_Placement_On_Reshow()
    {
        var anchor = new Button("Anchor");
        var root = new VStack { anchor };

        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(40, 10));
        driver.Tick();

        var popup = new Popup
        {
            Anchor = anchor,
            Content = new TextBlock("PopupContent"),
            MatchAnchorWidth = false,
        };

        driver.App.Post(popup.Show);
        driver.Tick();
        driver.App.Post(popup.Close);
        driver.Tick();

        popup.Placement = PopupPlacement.Right;
        driver.App.Post(popup.Show);
        driver.Tick();

        Assert.AreEqual(anchor.Bounds.Right, popup.Content!.Bounds.X);
    }

    [TestMethod]
    public void Popup_RightPlacement_Shrinks_To_Available_Space()
    {
        var anchor = new Button("A");
        var root = new Padder(anchor).Padding(new Thickness(2, 2, 0, 0));

        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(40, 16));
        driver.Tick();

        var content = new TextBlock(new string('x', 200)).Wrap(true);

        var popup = new Popup
        {
            Anchor = anchor,
            Content = content,
            MatchAnchorWidth = false,
            Placement = PopupPlacement.Right,
        };

        driver.App.Post(popup.Show);
        driver.Tick();

        var expectedX = anchor.Bounds.Right;
        var expectedWidth = Math.Max(1, 40 - expectedX);

        Assert.AreEqual(expectedX, content.Bounds.X);
        Assert.AreEqual(expectedWidth, content.Bounds.Width);
    }

    [TestMethod]
    public void Popup_LeftPlacement_Shrinks_To_Available_Space()
    {
        var anchor = new Button("A");
        var root = new Padder(anchor).Padding(new Thickness(30, 2, 0, 0));

        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(40, 16));
        driver.Tick();

        var content = new TextBlock(new string('x', 200)).Wrap(true);

        var popup = new Popup
        {
            Anchor = anchor,
            Content = content,
            MatchAnchorWidth = false,
            Placement = PopupPlacement.Left,
        };

        driver.App.Post(popup.Show);
        driver.Tick();

        var expectedRight = anchor.Bounds.X;
        var expectedWidth = Math.Max(1, expectedRight);

        Assert.AreEqual(expectedRight, content.Bounds.Right);
        Assert.AreEqual(expectedWidth, content.Bounds.Width);
    }

    [TestMethod]
    public void Popup_Closes_On_Outside_Click()
    {
        var anchor = new Button("Anchor");
        var root = new VStack { anchor };

        var popup = new Popup
        {
            Anchor = anchor,
            Content = new TextBlock { Text = "PopupContent" },
            MatchAnchorWidth = true,
        };

        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(40, 10));
        driver.Tick();
        driver.App.Post(popup.Show);
        driver.Tick();

        // Click outside the popup (on the header row where the anchor is).
        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Down, Button = TerminalMouseButton.Left, X = 1, Y = 0 });
        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Up, Button = TerminalMouseButton.Left, X = 1, Y = 0 });
        driver.Tick();

        var outText = driver.Backend.GetOutText();
        var screen = new AnsiTestScreen(40, 10);
        screen.Apply(outText);
        var rendered = screen.GetText();

        Assert.IsFalse(rendered.Contains("PopupContent", StringComparison.Ordinal));
    }

    [TestMethod]
    public void Popup_Closes_On_Tab()
    {
        var anchor = new Button("Anchor");
        var root = new VStack { anchor, new TextBox("after") };

        var popup = new Popup
        {
            Anchor = anchor,
            Content = new TextBlock { Text = "PopupContent" },
            MatchAnchorWidth = true,
        };

        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(40, 10));
        driver.Tick();
        driver.App.Post(popup.Show);
        driver.Tick();

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Tab });
        driver.Tick();

        var outText = driver.Backend.GetOutText();
        var screen = new AnsiTestScreen(40, 10);
        screen.Apply(outText);
        var rendered = screen.GetText();

        Assert.IsFalse(rendered.Contains("PopupContent", StringComparison.Ordinal));
    }

    private static char GetChar(string rendered, int x, int y)
    {
        var lines = rendered.Split('\n');
        if ((uint)y >= (uint)lines.Length)
        {
            return '\0';
        }

        var line = lines[y];
        if (line.Length > 0 && line[^1] == '\r')
        {
            line = line[..^1];
        }

        if ((uint)x >= (uint)line.Length)
        {
            return '\0';
        }

        return line[x];
    }
}
