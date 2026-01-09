// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.Backends;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Hosting;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class AppChromeTests
{
    [TestMethod]
    public async Task Header_And_Footer_Render()
    {
        var backend = new InMemoryTerminalBackend(new TerminalSize(40, 6));
        using var session = Terminal.Open(backend, new TerminalOptions { ImplicitStartInput = true }, force: true);

        var header = new Header { Left = "Left", Center = "Center", Right = "Right" };
        var footer = new Footer { Left = "FLeft", Center = "FCenter", Right = "FRight" };

        var root = new DockLayout
        {
            Top = header,
            Bottom = footer,
            Content = new TextBlock("Body"),
        };

        var app = new TerminalApp(root, session.Instance, new TerminalAppOptions { HostKind = TerminalHostKind.Fullscreen });
        var runTask = app.RunInBackgroundAsync();

        await Task.Delay(80);
        backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Escape });
        await runTask.WaitAsync(TimeSpan.FromSeconds(2));

        var outText = backend.GetOutText();
        var screen = new AnsiTestScreen(40, 6);
        screen.Apply(outText);
        var rendered = screen.GetText();

        StringAssert.Contains(rendered, "Left");
        StringAssert.Contains(rendered, "Center");
        StringAssert.Contains(rendered, "Right");
        StringAssert.Contains(rendered, "Body");
        StringAssert.Contains(rendered, "FLeft");
        StringAssert.Contains(rendered, "FCenter");
        StringAssert.Contains(rendered, "FRight");
    }
}

