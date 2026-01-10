// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.Backends;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Hosting;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class MenuTests
{
    [TestMethod]
    public async Task MenuBar_Enter_Invokes_Menu_Item_Action()
    {
        var backend = new InMemoryTerminalBackend(new TerminalSize(50, 12));
        using var session = Terminal.Open(backend, new TerminalOptions { ImplicitStartInput = true }, force: true);

        var invoked = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        var file = new MenuItem("File");
        file.Items.Add(new MenuItem("Open", () => invoked.TrySetResult(true)));

        var bar = new MenuBar();
        bar.Items.Add(file);

        var root = new VStack { bar };
        var app = new TerminalApp(root, session.Instance, new TerminalAppOptions { HostKind = TerminalHostKind.Fullscreen });
        var runTask = app.RunInBackgroundAsync();

        await Task.Delay(80);

        backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Enter }); // open
        backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Enter }); // invoke first item

        Assert.IsTrue(await invoked.Task.WaitAsync(TimeSpan.FromSeconds(2)));

        backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Escape }); // exit app
        await runTask.WaitAsync(TimeSpan.FromSeconds(2));
    }

    [TestMethod]
    public async Task MenuBar_Right_Opens_Submenu_And_Invokes_Action()
    {
        var backend = new InMemoryTerminalBackend(new TerminalSize(60, 14));
        using var session = Terminal.Open(backend, new TerminalOptions { ImplicitStartInput = true }, force: true);

        var invoked = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        var file = new MenuItem("File");
        var recent = new MenuItem("Recent");
        recent.Items.Add(new MenuItem("Entry 1", () => invoked.TrySetResult(true)));
        file.Items.Add(recent);

        var bar = new MenuBar();
        bar.Items.Add(file);

        var root = new VStack { bar };
        var app = new TerminalApp(root, session.Instance, new TerminalAppOptions { HostKind = TerminalHostKind.Fullscreen });
        var runTask = app.RunInBackgroundAsync();

        await Task.Delay(80);

        backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Enter }); // open root menu
        backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Right }); // open submenu
        backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Enter }); // invoke submenu item

        Assert.IsTrue(await invoked.Task.WaitAsync(TimeSpan.FromSeconds(2)));

        backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Escape }); // exit app
        await runTask.WaitAsync(TimeSpan.FromSeconds(2));
    }
}

