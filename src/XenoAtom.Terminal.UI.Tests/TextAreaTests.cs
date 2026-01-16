// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Linq;
using XenoAtom.Terminal;
using XenoAtom.Terminal.Backends;
using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Hosting;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class TextAreaTests
{
    private static Task WaitForTextAsync(TextArea textArea, Func<string?, bool> predicate, TimeSpan timeout)
    {
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        void Handler(Binding binding)
        {
            if (ReferenceEquals(binding.Owner, textArea) && binding.Accessor.Name == "Text" && predicate(textArea.Text))
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

    [TestMethod]
    public async Task TextArea_Edits_Multiple_Lines()
    {
        var backend = new InMemoryTerminalBackend(new TerminalSize(40, 10));
        using var session = Terminal.Open(backend, new TerminalOptions { ImplicitStartInput = true }, force: true);

        var textArea = new TextArea();
        var root = new VStack { textArea };

        var app = new TerminalApp(root, session.Instance, new TerminalAppOptions { HostKind = TerminalHostKind.Fullscreen });
        var runTask = app.RunInBackgroundAsync();

        await Task.Delay(20);

        var reached = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        void Handler(Binding binding)
        {
            if (ReferenceEquals(binding.Owner, textArea) && binding.Accessor.Name == "Text" && textArea.Text == "Hello\nWorld")
            {
                reached.TrySetResult();
            }
        }

        BindingManager.Current.ValueChanged += Handler;
        try
        {
            backend.PushEvent(new TerminalTextEvent { Text = "Hello" });
            backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Enter });
            backend.PushEvent(new TerminalTextEvent { Text = "World" });

            await reached.Task.WaitAsync(TimeSpan.FromSeconds(2));
        }
        finally
        {
            BindingManager.Current.ValueChanged -= Handler;
            backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Escape });
            await runTask.WaitAsync(TimeSpan.FromSeconds(2));
        }

        var outText = backend.GetOutText();
        var screen = new AnsiTestScreen(40, 10);
        screen.Apply(outText);
        var rendered = screen.GetText();

        StringAssert.Contains(rendered, "Hello");
        StringAssert.Contains(rendered, "World");
    }

    [TestMethod]
    public async Task TextArea_Wraps_Text_By_Default()
    {
        var backend = new InMemoryTerminalBackend(new TerminalSize(10, 6));
        using var session = Terminal.Open(backend, new TerminalOptions { ImplicitStartInput = true }, force: true);

        var textArea = new TextArea { Text = "0123456789" };
        var root = new VStack { textArea };

        var app = new TerminalApp(root, session.Instance, new TerminalAppOptions { HostKind = TerminalHostKind.Fullscreen });
        var runTask = app.RunInBackgroundAsync();

        await Task.Delay(40);
        backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Escape });
        await runTask.WaitAsync(TimeSpan.FromSeconds(2));

        var screen = new AnsiTestScreen(10, 6);
        screen.Apply(backend.GetOutText());
        var rendered = screen.GetText().Split('\n');

        Assert.IsTrue(rendered.Length >= 3, "Expected multiple lines of output.");
        Assert.IsTrue(rendered[1].Contains("012345", StringComparison.Ordinal), "Expected first wrapped line.");
        Assert.IsTrue(rendered[2].Contains("6789", StringComparison.Ordinal), "Expected second wrapped line.");
    }

    [TestMethod]
    public async Task TextArea_CtrlHomeEnd_Moves_Caret_To_Document_Edges()
    {
        var backend = new InMemoryTerminalBackend(new TerminalSize(40, 10));
        using var session = Terminal.Open(backend, new TerminalOptions { ImplicitStartInput = true }, force: true);

        var textArea = new TextArea { Text = "Line 1\nLine 2\nLine 3" };
        var root = new VStack { textArea };

        var app = new TerminalApp(root, session.Instance, new TerminalAppOptions { HostKind = TerminalHostKind.Fullscreen });
        var runTask = app.RunInBackgroundAsync();

        await Task.Delay(40);

        try
        {
            backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.End, Modifiers = TerminalModifiers.Ctrl });
            backend.PushEvent(new TerminalTextEvent { Text = "Z" });
            await WaitForTextAsync(textArea, text => text?.EndsWith("Z", StringComparison.Ordinal) == true, TimeSpan.FromSeconds(2));

            backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Home, Modifiers = TerminalModifiers.Ctrl });
            backend.PushEvent(new TerminalTextEvent { Text = "A" });
            await WaitForTextAsync(textArea, text => text?.StartsWith("A", StringComparison.Ordinal) == true, TimeSpan.FromSeconds(2));
        }
        finally
        {
            backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Escape });
            await runTask.WaitAsync(TimeSpan.FromSeconds(2));
        }
    }

    [TestMethod]
    public async Task TextArea_ScrollOffset_Does_Not_Reset_During_Layout()
    {
        var backend = new InMemoryTerminalBackend(new TerminalSize(40, 8));
        using var session = Terminal.Open(backend, new TerminalOptions { ImplicitStartInput = true }, force: true);

        var text = string.Join("\n", Enumerable.Range(0, 40).Select(i => $"Line {i:00}"));
        var textArea = new TextArea { Text = text };
        var root = new VStack { textArea };

        var app = new TerminalApp(root, session.Instance, new TerminalAppOptions { HostKind = TerminalHostKind.Fullscreen });
        var runTask = app.RunInBackgroundAsync();

        await Task.Delay(40);

        try
        {
            await app.Dispatcher.InvokeAsync(() => textArea.Scroll.ScrollBy(0, 3));
            await Task.Delay(60);

            var offset = await app.Dispatcher.InvokeAsync(() => textArea.Scroll.OffsetY);
            Assert.AreEqual(3, offset);
        }
        finally
        {
            backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Escape });
            await runTask.WaitAsync(TimeSpan.FromSeconds(2));
        }
    }
}
