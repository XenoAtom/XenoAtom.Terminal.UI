// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.Backends;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Hosting;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class OptionListTests
{
    [TestMethod]
    public async Task OptionList_ArrowDown_Raises_SelectionChanged()
    {
        var backend = new InMemoryTerminalBackend(new TerminalSize(40, 10));
        using var session = Terminal.Open(backend, new TerminalOptions { ImplicitStartInput = true }, force: true);

        var list = new OptionList { Height = 4 };
        list.Items.AddRange(
            new OptionListItem("First"),
            new OptionListItem("Second"),
            new OptionListItem("Third"));

        var selectionChanged = new TaskCompletionSource<(int OldIndex, int NewIndex)>(TaskCreationOptions.RunContinuationsAsynchronously);
        list.SelectionChanged((_, e) => selectionChanged.TrySetResult((e.OldIndex, e.NewIndex)));

        var root = new VStack { list };
        var app = new TerminalApp(root, session.Instance, new TerminalAppOptions { HostKind = TerminalHostKind.Fullscreen });
        var runTask = app.RunInBackgroundAsync();

        await Task.Delay(60);

        backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Down });
        var result = await selectionChanged.Task.WaitAsync(TimeSpan.FromSeconds(2));

        backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Escape });
        await runTask.WaitAsync(TimeSpan.FromSeconds(2));

        Assert.AreEqual(0, result.OldIndex);
        Assert.AreEqual(1, result.NewIndex);
    }

    [TestMethod]
    public async Task OptionList_Enter_Raises_ItemActivated()
    {
        var backend = new InMemoryTerminalBackend(new TerminalSize(40, 10));
        using var session = Terminal.Open(backend, new TerminalOptions { ImplicitStartInput = true }, force: true);

        var list = new OptionList { Height = 4 };
        list.Items.AddRange(
            new OptionListItem("First"),
            new OptionListItem("Second"),
            new OptionListItem("Third"));

        var activated = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        list.ItemActivated((_, e) => activated.TrySetResult(e.Index));

        var root = new VStack { list };
        var app = new TerminalApp(root, session.Instance, new TerminalAppOptions { HostKind = TerminalHostKind.Fullscreen });
        var runTask = app.RunInBackgroundAsync();

        await Task.Delay(60);

        backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Down });
        backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Enter });

        var index = await activated.Task.WaitAsync(TimeSpan.FromSeconds(2));

        backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Escape });
        await runTask.WaitAsync(TimeSpan.FromSeconds(2));

        Assert.AreEqual(1, index);
    }

    [TestMethod]
    public async Task OptionList_Renders_Descriptions_On_Second_Line()
    {
        var backend = new InMemoryTerminalBackend(new TerminalSize(40, 10));
        using var session = Terminal.Open(backend, new TerminalOptions { ImplicitStartInput = true }, force: true);

        var list = new OptionList { Height = 4 };
        list.Items.AddRange(
            new OptionListItem("Build", "Ctrl+B") { Description = "Build the project" },
            new OptionListItem("Run", "F5") { Description = "Run the app" });

        var root = new VStack { list };
        var app = new TerminalApp(root, session.Instance, new TerminalAppOptions { HostKind = TerminalHostKind.Fullscreen });
        var runTask = app.RunInBackgroundAsync();

        await Task.Delay(120);

        backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Escape });
        await runTask.WaitAsync(TimeSpan.FromSeconds(2));

        var outText = backend.GetOutText();
        var screen = new AnsiTestScreen(40, 10);
        screen.Apply(outText);
        var rendered = screen.GetText();

        StringAssert.Contains(rendered, "Build the project");
        StringAssert.Contains(rendered, "Run the app");
    }
}

