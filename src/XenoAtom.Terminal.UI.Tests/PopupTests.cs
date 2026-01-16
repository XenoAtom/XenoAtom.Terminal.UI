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
}
