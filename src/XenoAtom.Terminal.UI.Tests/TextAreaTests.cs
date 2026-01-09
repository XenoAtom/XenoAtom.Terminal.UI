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
}

