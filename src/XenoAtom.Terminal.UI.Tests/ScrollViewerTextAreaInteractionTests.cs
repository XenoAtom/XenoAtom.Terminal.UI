// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Linq;
using XenoAtom.Terminal;
using XenoAtom.Terminal.Backends;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Hosting;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class ScrollViewerTextAreaInteractionTests
{
    [TestMethod]
    public async Task ScrollViewerTextArea_MouseWheel_Scrolls_Without_Focus()
    {
        var backend = new InMemoryTerminalBackend(new TerminalSize(40, 12));
        using var session = Terminal.Open(backend, new TerminalOptions { ImplicitStartInput = true }, force: true);

        var button = new Button("Top");
        var textArea = new TextArea
        {
            Text = string.Join("\n", Enumerable.Range(0, 50).Select(i => $"Line {i:00}")),
        };

        var scrollViewer = new ScrollViewer
        {
            ContentMode = ScrollViewerContentMode.UseContentScrollModel,
            Content = textArea,
            MinHeight = 8,
            MaxHeight = 8,
        };

        var root = new VStack(button, scrollViewer).Spacing(0);

        var app = new TerminalApp(root, session.Instance, new TerminalAppOptions { HostKind = TerminalHostKind.Fullscreen });
        var runTask = app.RunInBackgroundAsync();

        await Task.Delay(60);

        try
        {
            await app.Dispatcher.InvokeAsync(() => app.Focus(button));
            var bounds = await app.Dispatcher.InvokeAsync(() => textArea.Bounds);
            var extent = await app.Dispatcher.InvokeAsync(() => textArea.Scroll.ExtentHeight);
            var viewport = await app.Dispatcher.InvokeAsync(() => textArea.Scroll.ViewportHeight);
            Assert.IsTrue(extent > viewport, $"Expected scrollable content. extent={extent} viewport={viewport}");
            var wheelX = bounds.X + 1;
            var wheelY = bounds.Y + 2;
            var hit = await app.Dispatcher.InvokeAsync(() => root.HitTest(wheelX, wheelY)?.GetType().Name ?? "<null>");
            Assert.AreEqual(nameof(TextArea), hit);

            backend.PushEvent(new TerminalMouseEvent
            {
                Kind = TerminalMouseKind.Wheel,
                Button = TerminalMouseButton.Wheel,
                X = wheelX,
                Y = wheelY,
                WheelDelta = -1,
            });

            for (var i = 0; i < 40; i++)
            {
                await Task.Delay(10);
                var offset = await app.Dispatcher.InvokeAsync(() => textArea.Scroll.OffsetY);
                if (offset > 0)
                {
                    return;
                }
            }

            var debug = await app.Dispatcher.InvokeAsync(() =>
                $"textArea.OffsetY={textArea.Scroll.OffsetY} extent={textArea.Scroll.ExtentHeight} viewport={textArea.Scroll.ViewportHeight} scrollViewer.VerticalOffset={scrollViewer.VerticalOffset}");
            Assert.Fail($"Expected wheel scrolling to update the TextArea scroll offset. {debug}");
        }
        finally
        {
            backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Escape });
            await runTask.WaitAsync(TimeSpan.FromSeconds(2));
        }
    }

    [TestMethod]
    public async Task ScrollViewerTextArea_MouseWheel_Scrolls_When_Focused()
    {
        var backend = new InMemoryTerminalBackend(new TerminalSize(40, 12));
        using var session = Terminal.Open(backend, new TerminalOptions { ImplicitStartInput = true }, force: true);

        var textArea = new TextArea
        {
            Text = string.Join("\n", Enumerable.Range(0, 50).Select(i => $"Line {i:00}")),
        };

        var scrollViewer = new ScrollViewer
        {
            ContentMode = ScrollViewerContentMode.UseContentScrollModel,
            Content = textArea,
            MinHeight = 8,
            MaxHeight = 8,
        };

        var root = new VStack(scrollViewer).Spacing(0);

        var app = new TerminalApp(root, session.Instance, new TerminalAppOptions { HostKind = TerminalHostKind.Fullscreen });
        var runTask = app.RunInBackgroundAsync();

        await Task.Delay(60);

        try
        {
            await app.Dispatcher.InvokeAsync(() => app.Focus(textArea));
            var bounds = await app.Dispatcher.InvokeAsync(() => textArea.Bounds);
            var extent = await app.Dispatcher.InvokeAsync(() => textArea.Scroll.ExtentHeight);
            var viewport = await app.Dispatcher.InvokeAsync(() => textArea.Scroll.ViewportHeight);
            Assert.IsTrue(extent > viewport, $"Expected scrollable content. extent={extent} viewport={viewport}");

            backend.PushEvent(new TerminalMouseEvent
            {
                Kind = TerminalMouseKind.Wheel,
                Button = TerminalMouseButton.Wheel,
                X = bounds.X + 1,
                Y = bounds.Y + 2,
                WheelDelta = -1,
            });

            for (var i = 0; i < 40; i++)
            {
                await Task.Delay(10);
                var offset = await app.Dispatcher.InvokeAsync(() => textArea.Scroll.OffsetY);
                if (offset > 0)
                {
                    return;
                }
            }

            var debug = await app.Dispatcher.InvokeAsync(() =>
                $"textArea.OffsetY={textArea.Scroll.OffsetY} extent={textArea.Scroll.ExtentHeight} viewport={textArea.Scroll.ViewportHeight} scrollViewer.VerticalOffset={scrollViewer.VerticalOffset}");
            Assert.Fail($"Expected wheel scrolling to update the TextArea scroll offset while focused. {debug}");
        }
        finally
        {
            backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Escape });
            await runTask.WaitAsync(TimeSpan.FromSeconds(2));
        }
    }

    [TestMethod]
    public async Task ScrollViewerTextArea_ScrollBar_Click_Scrolls_Without_Focus()
    {
        var backend = new InMemoryTerminalBackend(new TerminalSize(40, 12));
        using var session = Terminal.Open(backend, new TerminalOptions { ImplicitStartInput = true }, force: true);

        var button = new Button("Top");
        var textArea = new TextArea
        {
            Text = string.Join("\n", Enumerable.Range(0, 50).Select(i => $"Line {i:00}")),
        };

        var scrollViewer = new ScrollViewer
        {
            ContentMode = ScrollViewerContentMode.UseContentScrollModel,
            Content = textArea,
            MinHeight = 8,
            MaxHeight = 8,
        };

        var root = new VStack(button, scrollViewer).Spacing(0);

        var app = new TerminalApp(root, session.Instance, new TerminalAppOptions { HostKind = TerminalHostKind.Fullscreen });
        var runTask = app.RunInBackgroundAsync();

        await Task.Delay(60);

        try
        {
            await app.Dispatcher.InvokeAsync(() => app.Focus(button));
            var scrollBounds = await app.Dispatcher.InvokeAsync(() => scrollViewer.Bounds);
            var extent = await app.Dispatcher.InvokeAsync(() => textArea.Scroll.ExtentHeight);
            var viewport = await app.Dispatcher.InvokeAsync(() => textArea.Scroll.ViewportHeight);
            Assert.IsTrue(extent > viewport, $"Expected scrollable content. extent={extent} viewport={viewport}");

            var barX = scrollBounds.X + scrollBounds.Width - 1;
            var barY = scrollBounds.Y + 2;
            var hit = await app.Dispatcher.InvokeAsync(() => root.HitTest(barX, barY)?.GetType().Name ?? "<null>");
            Assert.AreEqual(nameof(ScrollBar), hit);

            backend.PushEvent(new TerminalMouseEvent
            {
                Kind = TerminalMouseKind.Down,
                Button = TerminalMouseButton.Left,
                X = barX,
                Y = barY,
            });
            backend.PushEvent(new TerminalMouseEvent
            {
                Kind = TerminalMouseKind.Up,
                Button = TerminalMouseButton.Left,
                X = barX,
                Y = barY,
            });

            for (var i = 0; i < 40; i++)
            {
                await Task.Delay(10);
                var offset = await app.Dispatcher.InvokeAsync(() => textArea.Scroll.OffsetY);
                if (offset > 0)
                {
                    return;
                }
            }

            var debug = await app.Dispatcher.InvokeAsync(() =>
                $"textArea.OffsetY={textArea.Scroll.OffsetY} extent={textArea.Scroll.ExtentHeight} viewport={textArea.Scroll.ViewportHeight} scrollViewer.VerticalOffset={scrollViewer.VerticalOffset}");
            Assert.Fail($"Expected clicking the scrollbar to scroll the content. {debug}");
        }
        finally
        {
            backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Escape });
            await runTask.WaitAsync(TimeSpan.FromSeconds(2));
        }
    }

    [TestMethod]
    public async Task TextArea_CtrlShiftHomeEnd_Selects_Entire_Document()
    {
        var backend = new InMemoryTerminalBackend(new TerminalSize(40, 12));
        using var session = Terminal.Open(backend, new TerminalOptions { ImplicitStartInput = true }, force: true);

        var textArea = new TextArea { Text = "Hello\nWorld\nAgain" };
        var root = new VStack(textArea).Spacing(0);

        var app = new TerminalApp(root, session.Instance, new TerminalAppOptions { HostKind = TerminalHostKind.Fullscreen });
        var runTask = app.RunInBackgroundAsync();

        await Task.Delay(60);

        try
        {
            await app.Dispatcher.InvokeAsync(() => app.Focus(textArea));

            backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.End, Modifiers = TerminalModifiers.Ctrl });
            backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Home, Modifiers = TerminalModifiers.Ctrl | TerminalModifiers.Shift });
            backend.PushEvent(new TerminalTextEvent { Text = "X" });

            await Task.Delay(80);

            var text = await app.Dispatcher.InvokeAsync(() => textArea.Text ?? string.Empty);
            Assert.AreEqual("X", text);

            backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Home, Modifiers = TerminalModifiers.Ctrl });
            backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.End, Modifiers = TerminalModifiers.Ctrl | TerminalModifiers.Shift });
            backend.PushEvent(new TerminalTextEvent { Text = "Y" });

            await Task.Delay(80);

            text = await app.Dispatcher.InvokeAsync(() => textArea.Text ?? string.Empty);
            Assert.AreEqual("Y", text);
        }
        finally
        {
            backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Escape });
            await runTask.WaitAsync(TimeSpan.FromSeconds(2));
        }
    }
}
