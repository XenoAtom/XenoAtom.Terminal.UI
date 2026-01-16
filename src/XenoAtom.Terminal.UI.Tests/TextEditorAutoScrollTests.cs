// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System;
using System.Linq;
using XenoAtom.Terminal;
using XenoAtom.Terminal.Backends;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Hosting;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class TextEditorAutoScrollTests
{
    private static Task WaitForTextAsync(object owner, Func<string?> getText, Func<string?, bool> predicate, TimeSpan timeout)
    {
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        void Handler(Binding binding)
        {
            if (ReferenceEquals(binding.Owner, owner) && binding.Accessor.Name == "Text" && predicate(getText()))
            {
                tcs.TrySetResult();
            }
        }

        BindingManager.Current.ValueChanged += Handler;
        return tcs.Task.WaitAsync(timeout).ContinueWith(task =>
        {
            BindingManager.Current.ValueChanged -= Handler;
            return task;
        }).Unwrap();
    }

    private static async Task WaitUntilAsync(Func<Task<bool>> predicate, TimeSpan timeout)
    {
        var start = Environment.TickCount64;
        while (Environment.TickCount64 - start < timeout.TotalMilliseconds)
        {
            if (await predicate())
            {
                return;
            }

            await Task.Delay(10);
        }

        Assert.Fail("Timed out waiting for condition.");
    }

    [TestMethod]
    public async Task TextArea_AutoScrolls_View_When_Typing_Past_Viewport()
    {
        var backend = new InMemoryTerminalBackend(new TerminalSize(30, 8));
        using var session = Terminal.Open(backend, new TerminalOptions { ImplicitStartInput = true }, force: true);

        var textArea = new TextArea();
        var root = new VStack { textArea };

        var app = new TerminalApp(root, session.Instance, new TerminalAppOptions { HostKind = TerminalHostKind.Fullscreen });
        var runTask = app.RunInBackgroundAsync();

        await Task.Delay(40);

        try
        {
            await app.Dispatcher.InvokeAsync(() => app.Focus(textArea));

            for (var i = 0; i < 24; i++)
            {
                backend.PushEvent(new TerminalTextEvent { Text = $"L{i:00}" });
                backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Enter });
            }

            backend.PushEvent(new TerminalTextEvent { Text = "LAST" });
            await WaitForTextAsync(textArea, () => textArea.Text, text => text?.EndsWith("LAST", StringComparison.Ordinal) == true, TimeSpan.FromSeconds(2));

            await WaitUntilAsync(async () =>
            {
                var offset = await app.Dispatcher.InvokeAsync(() => textArea.Scroll.OffsetY);
                return offset > 0;
            }, TimeSpan.FromSeconds(2));

            await Task.Delay(80);

            var screen = new AnsiTestScreen(30, 8);
            screen.Apply(backend.GetOutText());
            var rendered = screen.GetText();
            StringAssert.Contains(rendered, "LAST", "Expected the final line to be visible after auto-scroll.");

            var caretVisible = await app.Dispatcher.InvokeAsync(() => textArea.TryGetCursorCell(out _, out _));
            Assert.IsTrue(caretVisible, "Expected the caret to remain visible after auto-scrolling.");
        }
        finally
        {
            backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Escape });
            await runTask.WaitAsync(TimeSpan.FromSeconds(2));
        }
    }

    [TestMethod]
    public async Task TextBox_AutoScrolls_View_When_Typing_Past_Viewport()
    {
        var backend = new InMemoryTerminalBackend(new TerminalSize(30, 6));
        using var session = Terminal.Open(backend, new TerminalOptions { ImplicitStartInput = true }, force: true);

        var textBox = new TextBox();
        var root = new VStack { textBox };

        var app = new TerminalApp(root, session.Instance, new TerminalAppOptions { HostKind = TerminalHostKind.Fullscreen });
        var runTask = app.RunInBackgroundAsync();

        await Task.Delay(40);

        try
        {
            await app.Dispatcher.InvokeAsync(() => app.Focus(textBox));

            backend.PushEvent(new TerminalTextEvent { Text = new string('a', 64) + "TAIL" });
            await WaitForTextAsync(textBox, () => textBox.Text, text => text?.EndsWith("TAIL", StringComparison.Ordinal) == true, TimeSpan.FromSeconds(2));

            await WaitUntilAsync(
                async () => await app.Dispatcher.InvokeAsync(() => textBox.Scroll.OffsetX) > 0,
                TimeSpan.FromSeconds(2));

            await Task.Delay(80);

            var screen = new AnsiTestScreen(30, 6);
            screen.Apply(backend.GetOutText());
            var rendered = screen.GetText();
            StringAssert.Contains(rendered, "TAIL", "Expected the end of the text to be visible after horizontal auto-scroll.");

            var caretVisible = await app.Dispatcher.InvokeAsync(() => textBox.TryGetCursorCell(out _, out _));
            Assert.IsTrue(caretVisible, "Expected the caret to remain visible after horizontal auto-scroll.");
        }
        finally
        {
            backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Escape });
            await runTask.WaitAsync(TimeSpan.FromSeconds(2));
        }
    }
}
