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
    public async Task Popup_Closes_On_Outside_Click()
    {
        var backend = new InMemoryTerminalBackend(new TerminalSize(40, 10));
        using var session = Terminal.Open(backend, new TerminalOptions { ImplicitStartInput = true }, force: true);

        var anchor = new Button("Anchor");
        var root = new VStack { anchor };

        var popup = new Popup
        {
            Anchor = anchor,
            Content = new TextBlock { Text = "PopupContent" },
            MatchAnchorWidth = true,
        };

        var app = new TerminalApp(root, session.Instance, new TerminalAppOptions { HostKind = TerminalHostKind.Fullscreen });
        var runTask = app.RunInBackgroundAsync();

        await Task.Delay(30);
        app.Post(popup.Show);
        await Task.Delay(60);

        // Click outside the popup (on the header row where the anchor is).
        backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Down, Button = TerminalMouseButton.Left, X = 1, Y = 0 });
        backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Up, Button = TerminalMouseButton.Left, X = 1, Y = 0 });

        await Task.Delay(60);

        backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Escape });
        await runTask.WaitAsync(TimeSpan.FromSeconds(2));

        var outText = backend.GetOutText();
        var screen = new AnsiTestScreen(40, 10);
        screen.Apply(outText);
        var rendered = screen.GetText();

        Assert.IsFalse(rendered.Contains("PopupContent", StringComparison.Ordinal));
    }

    [TestMethod]
    public async Task Popup_Closes_On_Tab()
    {
        var backend = new InMemoryTerminalBackend(new TerminalSize(40, 10));
        using var session = Terminal.Open(backend, new TerminalOptions { ImplicitStartInput = true }, force: true);

        var anchor = new Button("Anchor");
        var root = new VStack { anchor, new TextBox { Text = "after" } };

        var popup = new Popup
        {
            Anchor = anchor,
            Content = new TextBlock { Text = "PopupContent" },
            MatchAnchorWidth = true,
        };

        var app = new TerminalApp(root, session.Instance, new TerminalAppOptions { HostKind = TerminalHostKind.Fullscreen });
        var runTask = app.RunInBackgroundAsync();

        await Task.Delay(30);
        app.Post(popup.Show);
        await Task.Delay(60);

        backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Tab });
        await Task.Delay(60);

        backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Escape });
        await runTask.WaitAsync(TimeSpan.FromSeconds(2));

        var outText = backend.GetOutText();
        var screen = new AnsiTestScreen(40, 10);
        screen.Apply(outText);
        var rendered = screen.GetText();

        Assert.IsFalse(rendered.Contains("PopupContent", StringComparison.Ordinal));
    }
}

