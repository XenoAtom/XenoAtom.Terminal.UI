// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal;
using XenoAtom.Terminal.Backends;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Hosting;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class TextBlockRenderingTests
{
    [TestMethod]
    public async Task TextBlock_EndEllipsis_Trims_To_Width()
    {
        var backend = new InMemoryTerminalBackend(new TerminalSize(10, 2));
        using var session = Terminal.Open(backend, new TerminalOptions { ImplicitStartInput = true }, force: true);

        var tb = new TextBlock("HelloWorld")
        {
            Wrap = false,
            Trimming = TextTrimming.EndEllipsis,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            MaxWidth = 5,
        };

        var root = new VStack(tb);
        var app = new TerminalApp(root, session.Instance, new TerminalAppOptions { HostKind = TerminalHostKind.Fullscreen });
        var runTask = app.RunInBackgroundAsync();

        await Task.Delay(30);
        backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Escape });
        await runTask.WaitAsync(TimeSpan.FromSeconds(2));

        var outText = backend.GetOutText();
        StringAssert.Contains(outText, "Hell…");
    }

    [TestMethod]
    public async Task TextBlock_StartEllipsis_Trims_To_Width()
    {
        var backend = new InMemoryTerminalBackend(new TerminalSize(10, 2));
        using var session = Terminal.Open(backend, new TerminalOptions { ImplicitStartInput = true }, force: true);

        var tb = new TextBlock("HelloWorld")
        {
            Wrap = false,
            Trimming = TextTrimming.StartEllipsis,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            MaxWidth = 5,
        };

        var root = new VStack(tb);
        var app = new TerminalApp(root, session.Instance, new TerminalAppOptions { HostKind = TerminalHostKind.Fullscreen });
        var runTask = app.RunInBackgroundAsync();

        await Task.Delay(30);
        backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Escape });
        await runTask.WaitAsync(TimeSpan.FromSeconds(2));

        var outText = backend.GetOutText();
        StringAssert.Contains(outText, "…orld");
    }

    [TestMethod]
    public async Task TextBlock_Can_Center_Align_Text_When_Stretched()
    {
        var backend = new InMemoryTerminalBackend(new TerminalSize(10, 2));
        using var session = Terminal.Open(backend, new TerminalOptions { ImplicitStartInput = true }, force: true);

        var tb = new TextBlock("Hi")
        {
            Wrap = false,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            TextAlignment = TextAlignment.Center,
        };

        var root = new VStack(tb);
        var app = new TerminalApp(root, session.Instance, new TerminalAppOptions { HostKind = TerminalHostKind.Fullscreen });
        var runTask = app.RunInBackgroundAsync();

        await Task.Delay(30);
        backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Escape });
        await runTask.WaitAsync(TimeSpan.FromSeconds(2));

        var outText = backend.GetOutText();
        StringAssert.Contains(outText, "    Hi");
    }
}
