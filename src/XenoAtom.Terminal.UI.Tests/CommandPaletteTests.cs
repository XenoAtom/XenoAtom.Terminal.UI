// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.Backends;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Hosting;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class CommandPaletteTests
{
    [TestMethod]
    public async Task CommandPalette_Filters_Items_Based_On_Query()
    {
        var backend = new InMemoryTerminalBackend(new TerminalSize(60, 12));
        using var session = Terminal.Open(backend, new TerminalOptions { ImplicitStartInput = true }, force: true);

        var palette = new CommandPalette();
        palette.Items.AddRange(
            new CommandPaletteItem("Open"),
            new CommandPaletteItem("Build"));

        var root = new VStack { palette };
        var app = new TerminalApp(root, session.Instance, new TerminalAppOptions { HostKind = TerminalHostKind.Fullscreen });
        var runTask = app.RunInBackgroundAsync();

        await Task.Delay(80);

        backend.PushEvent(new TerminalTextEvent { Text = "op" });

        await Task.Delay(150);

        backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Escape });
        await runTask.WaitAsync(TimeSpan.FromSeconds(2));

        var outText = backend.GetOutText();
        var screen = new AnsiTestScreen(60, 12);
        screen.Apply(outText);
        var rendered = screen.GetText();

        StringAssert.Contains(rendered, "Open");
        Assert.IsFalse(rendered.Contains("Build", StringComparison.Ordinal), "Filtered results should no longer contain non-matching entries.");
    }

    [TestMethod]
    public async Task CommandPalette_Invokes_Action_On_Activated_Item()
    {
        var backend = new InMemoryTerminalBackend(new TerminalSize(60, 12));
        using var session = Terminal.Open(backend, new TerminalOptions { ImplicitStartInput = true }, force: true);

        var invoked = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        var palette = new CommandPalette();
        palette.Items.AddRange(
            new CommandPaletteItem("Open", () => invoked.TrySetResult(true)),
            new CommandPaletteItem("Build"));

        var root = new VStack { palette };
        var app = new TerminalApp(root, session.Instance, new TerminalAppOptions { HostKind = TerminalHostKind.Fullscreen });
        var runTask = app.RunInBackgroundAsync();

        await Task.Delay(80);

        backend.PushEvent(new TerminalTextEvent { Text = "op" });
        backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Tab });
        backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Enter });

        Assert.IsTrue(await invoked.Task.WaitAsync(TimeSpan.FromSeconds(2)));

        backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Escape });
        await runTask.WaitAsync(TimeSpan.FromSeconds(2));
    }
}

