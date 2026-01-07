// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.Backends;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Hosting;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class ScrollViewerRenderingTests
{
    [TestMethod]
    public async Task ScrollViewer_Renders_Content()
    {
        var backend = new InMemoryTerminalBackend(new TerminalSize(60, 12));
        using var session = Terminal.Open(backend, new TerminalOptions { ImplicitStartInput = true }, force: true);

        var content = new VStack
        {
            "Log line 0",
            "Log line 1",
            "Log line 2",
            "Log line 3",
            "Log line 4",
        };

        var root = new ScrollViewer { Height = 6, Content = content };

        var app = new TerminalApp(root, session.Instance, new TerminalAppOptions { HostKind = TerminalHostKind.Fullscreen });
        var runTask = app.RunAsync();

        await Task.Delay(20);
        backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Escape });

        await runTask.WaitAsync(TimeSpan.FromSeconds(2));

        var outText = backend.GetOutText();
        StringAssert.Contains(outText, "Log line 0");
    }
}

