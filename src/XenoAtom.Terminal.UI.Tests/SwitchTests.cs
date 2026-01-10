// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Input;
using XenoAtom.Terminal.Backends;
using XenoAtom.Terminal.UI.Hosting;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class SwitchTests
{
    [TestMethod]
    public void Toggled_Event_Is_Raised_With_Old_And_New_Values()
    {
        var sw = new Switch();

        ToggleChangedEventArgs? args = null;
        sw.ToggledRouted += (_, e) => args = e;

        sw.IsOn = true;

        Assert.IsNotNull(args);
        Assert.AreEqual(false, args.OldValue);
        Assert.AreEqual(true, args.NewValue);
    }

    [TestMethod]
    public async Task Space_Key_Toggles_Switch()
    {
        var backend = new InMemoryTerminalBackend(new TerminalSize(30, 6));
        using var session = Terminal.Open(backend, new TerminalOptions { ImplicitStartInput = true }, force: true);

        var sw = new Switch();
        var root = new VStack { sw };

        var app = new TerminalApp(root, session.Instance, new TerminalAppOptions { HostKind = TerminalHostKind.Fullscreen });
        var runTask = app.RunInBackgroundAsync();

        await Task.Delay(50);
        backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Space });
        await Task.Delay(50);
        backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Escape });

        await runTask.WaitAsync(TimeSpan.FromSeconds(2));
        Assert.AreEqual(true, sw.IsOn);
    }
}
