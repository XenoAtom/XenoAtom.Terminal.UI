// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal;
using XenoAtom.Terminal.Backends;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class TerminalAppTests
{
    [TestMethod]
    public async Task Renders_TextBlock_In_InlineHost()
    {
        var backend = new InMemoryTerminalBackend(new TerminalSize(20, 10));
        using var session = Terminal.Open(backend, new TerminalOptions { ImplicitStartInput = true }, force: true);

        var root = new VStack();
        root.Add(new TextBlock("Hello"));

        var app = new TerminalApp(root, session.Instance);

        var runTask = app.RunAsync();
        await Task.Delay(10);
        backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Escape });

        await runTask.WaitAsync(TimeSpan.FromSeconds(2));

        var outText = backend.GetOutText();
        StringAssert.Contains(outText, "Hello");
    }

    [TestMethod]
    public async Task VirtualBackend_Delivers_Pushed_KeyEvents()
    {
        var backend = new InMemoryTerminalBackend(new TerminalSize(20, 10));
        using var session = Terminal.Open(backend, new TerminalOptions { ImplicitStartInput = true }, force: true);

        session.Instance.StartInput();
        backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Escape });

        var ev = await session.Instance.ReadEventAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(2));
        Assert.IsInstanceOfType<TerminalKeyEvent>(ev);
    }

    [TestMethod]
    public async Task Button_Raises_Click_On_Enter()
    {
        var backend = new InMemoryTerminalBackend(new TerminalSize(20, 10));
        using var session = Terminal.Open(backend, new TerminalOptions { ImplicitStartInput = true }, force: true);

        var button = new Button("OK");
        var clicked = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        button.Click += (_, _) => clicked.TrySetResult();

        var root = new VStack();
        root.Add(button);

        var app = new TerminalApp(root, session.Instance);
        var runTask = app.RunAsync();

        await Task.Delay(10);
        backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Enter });

        await clicked.Task.WaitAsync(TimeSpan.FromSeconds(2));

        backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Escape });
        await runTask.WaitAsync(TimeSpan.FromSeconds(2));
    }

    [TestMethod]
    public async Task CheckBox_Toggles_On_Space()
    {
        var backend = new InMemoryTerminalBackend(new TerminalSize(20, 10));
        using var session = Terminal.Open(backend, new TerminalOptions { ImplicitStartInput = true }, force: true);

        var checkBox = new CheckBox("A", isChecked: false);
        var changed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        void Handler(object owner, string name)
        {
            if (ReferenceEquals(owner, checkBox) && name == "IsChecked")
            {
                changed.TrySetResult();
            }
        }

        BindingManager.Current.ValueChanged += Handler;
        try
        {
            var root = new VStack();
            root.Add(checkBox);

            var app = new TerminalApp(root, session.Instance);
            var runTask = app.RunAsync();

            await Task.Delay(10);
            backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Space });

            await changed.Task.WaitAsync(TimeSpan.FromSeconds(2));
            Assert.IsTrue(checkBox.IsChecked);

            backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Escape });
            await runTask.WaitAsync(TimeSpan.FromSeconds(2));
        }
        finally
        {
            BindingManager.Current.ValueChanged -= Handler;
        }
    }
}
