// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.Backends;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Hosting;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class ScrollViewerRenderingTests
{
    [TestMethod]
    public async Task ScrollViewer_Renders_Content_When_Inside_TabControl()
    {
        var backend = new InMemoryTerminalBackend(new TerminalSize(80, 20));
        using var session = Terminal.Open(backend, new TerminalOptions { ImplicitStartInput = true }, force: true);

        var demoTab = new ScrollViewer
        {
            Content = new VStack(new TextBlock("Hello from ScrollViewer")).Spacing(1).HorizontalAlignment(HorizontalAlignment.Stretch),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
        };

        var root = new TabControl(
            new TabPage("Demo", demoTab),
            new TabPage("Other", new TextBlock("Other")))
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
        };

        var app = new TerminalApp(root, session.Instance, new TerminalAppOptions { HostKind = TerminalHostKind.Fullscreen });
        var runTask = app.RunInBackgroundAsync();

        await Task.Delay(40);
        backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Escape });
        await runTask.WaitAsync(TimeSpan.FromSeconds(2));

        var outText = backend.GetOutText();
        StringAssert.Contains(outText, "Hello from ScrollViewer");
    }

    [TestMethod]
    public async Task ScrollViewer_Renders_Content()
    {
        var backend = new InMemoryTerminalBackend(new TerminalSize(60, 12));
        using var session = Terminal.Open(backend, new TerminalOptions { ImplicitStartInput = true }, force: true);

        var content = new VStack
        {
            "Log line 0",
            "Log line 1",
            "Log line 2",
            "Log line 3",
            "Log line 4",
        };

        var root = new ScrollViewer { Content = content };

        var app = new TerminalApp(root, session.Instance, new TerminalAppOptions { HostKind = TerminalHostKind.Fullscreen });
        var runTask = app.RunInBackgroundAsync();

        await Task.Delay(20);
        backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Escape });

        await runTask.WaitAsync(TimeSpan.FromSeconds(2));

        var outText = backend.GetOutText();
        StringAssert.Contains(outText, "Log line 0");
    }

    [TestMethod]
    public async Task ScrollViewer_Scroll_Updates_Rendered_Content()
    {
        var backend = new InMemoryTerminalBackend(new TerminalSize(20, 6));
        using var session = Terminal.Open(backend, new TerminalOptions { ImplicitStartInput = true }, force: true);

        var content = new VStack();
        for (var i = 0; i < 10; i++)
        {
            content.Add($"Item {i}");
        }

        var root = new ScrollViewer { Content = content, HorizontalAlignment = XenoAtom.Terminal.UI.HorizontalAlignment.Stretch };
        var app = new TerminalApp(root, session.Instance, new TerminalAppOptions { HostKind = TerminalHostKind.Fullscreen });
        var runTask = app.RunInBackgroundAsync();

        await Task.Delay(30);
        backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Wheel, Button = TerminalMouseButton.Wheel, WheelDelta = -1, X = 1, Y = 1 });
        await Task.Delay(50);

        backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Escape });
        await runTask.WaitAsync(TimeSpan.FromSeconds(2));

        var outText = backend.GetOutText();
        var screen = new AnsiTestScreen(20, 6);
        screen.Apply(outText);
        var rendered = screen.GetText();

        StringAssert.Contains(rendered, "Item 1");
        Assert.IsFalse(rendered.Contains("Item 0", StringComparison.Ordinal), "After scrolling down, Item 0 should no longer be visible in the viewport.");
    }

    [TestMethod]
    public async Task ScrollViewer_Renders_Content_When_Set_After_Initial_Render()
    {
        var backend = new InMemoryTerminalBackend(new TerminalSize(40, 10));
        using var session = Terminal.Open(backend, new TerminalOptions { ImplicitStartInput = true }, force: true);

        var root = new ScrollViewer { HorizontalAlignment = HorizontalAlignment.Stretch };
        var app = new TerminalApp(root, session.Instance, new TerminalAppOptions { HostKind = TerminalHostKind.Fullscreen });
        var runTask = app.RunInBackgroundAsync();

        await Task.Delay(40);

        app.Post(() =>
        {
            root.Content = new TextBlock("Late content");
        });

        await Task.Delay(80);

        backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Escape });
        await runTask.WaitAsync(TimeSpan.FromSeconds(2));

        var outText = backend.GetOutText();
        StringAssert.Contains(outText, "Late content");
    }
}
