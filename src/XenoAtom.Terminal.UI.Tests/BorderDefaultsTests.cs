// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Hosting;
using XenoAtom.Terminal.UI.Styling;
using XenoAtom.Terminal.Backends;
using XenoAtom.Terminal;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class BorderDefaultsTests
{
    [TestMethod]
    public void ButtonStyle_Defaults_To_No_Border()
    {
        Assert.IsFalse(ButtonStyle.Default.ShowBorder);
    }

    [TestMethod]
    public void ListBox_Defaults_To_No_Border()
    {
        Assert.IsFalse(ListBoxStyle.Default.ShowBorder);
        Assert.IsFalse(new ListBox().Get<ListBoxStyle>().ShowBorder);
    }

    [TestMethod]
    public async Task Button_Border_Is_OptIn_Via_Style()
    {
        var backend = new InMemoryTerminalBackend(new TerminalSize(24, 6));
        using var session = Terminal.Open(backend, new TerminalOptions { ImplicitStartInput = true }, force: true);

        var root = new VStack(new Button("OK"));
        var app = new TerminalApp(root, session.Instance, new TerminalAppOptions { HostKind = TerminalHostKind.Fullscreen });
        var runTask = app.RunInBackgroundAsync();

        await Task.Delay(30);
        backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Escape });
        await runTask.WaitAsync(TimeSpan.FromSeconds(2));

        var outText = backend.GetOutText();
        Assert.DoesNotContain("▁", outText);

        backend = new InMemoryTerminalBackend(new TerminalSize(24, 6));
        using var session2 = Terminal.Open(backend, new TerminalOptions { ImplicitStartInput = true }, force: true);

        var borderedRoot = new VStack(new Button("OK"));
        borderedRoot.Set(ButtonStyle.Key, new ButtonStyle { ShowBorder = true });

        var app2 = new TerminalApp(borderedRoot, session2.Instance, new TerminalAppOptions { HostKind = TerminalHostKind.Fullscreen });
        var runTask2 = app2.RunInBackgroundAsync();

        await Task.Delay(30);
        backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Escape });
        await runTask2.WaitAsync(TimeSpan.FromSeconds(2));

        var outText2 = backend.GetOutText();
        Assert.Contains("▁", outText2);
    }
}
