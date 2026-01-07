// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Text;
using XenoAtom.Terminal.Backends;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Hosting;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class SpinnerTests
{
    [TestMethod]
    public async Task Spinner_Animates_Without_User_Input()
    {
        var backend = new InMemoryTerminalBackend(new TerminalSize(10, 3));
        using var session = Terminal.Open(backend, new TerminalOptions { ImplicitStartInput = true }, force: true);

        var spinner = new Spinner();
        spinner.SetEnvironmentValue(SpinnerStyle.Key, new SpinnerStyle
        {
            Name = "Test",
            Interval = TimeSpan.FromMilliseconds(10),
            Frames = [new Rune('a'), new Rune('b')],
            TextStyle = TextStyle.None,
        });

        var app = new TerminalApp(spinner, session.Instance, new TerminalAppOptions { HostKind = TerminalHostKind.Fullscreen });

        var runTask = app.RunInBackgroundAsync();
        await Task.Delay(150);
        backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Escape });

        await runTask.WaitAsync(TimeSpan.FromSeconds(2));

        var outText = backend.GetOutText();
        StringAssert.Contains(outText, "a");
        StringAssert.Contains(outText, "b");
    }
}
