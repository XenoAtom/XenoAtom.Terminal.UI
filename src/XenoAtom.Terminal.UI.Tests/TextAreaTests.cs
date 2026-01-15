// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.Backends;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Hosting;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class TextAreaTests
{
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
}
