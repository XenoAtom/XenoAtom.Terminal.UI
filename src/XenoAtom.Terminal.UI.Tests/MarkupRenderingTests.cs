// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.Backends;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Hosting;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class MarkupRenderingTests
{
    [TestMethod]
    public async Task Markup_Respects_NewLines()
    {
        var backend = new InMemoryTerminalBackend(new TerminalSize(60, 8));
        using var session = Terminal.Open(backend, new TerminalOptions { ImplicitStartInput = true }, force: true);

        var root = new Markup("[bold]Markup[/] supports inline styling:\n- [green]success[/]\n- [yellow]warning[/]")
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .VerticalAlignment(VerticalAlignment.Stretch);

        var app = new TerminalApp(root, session.Instance, new TerminalAppOptions { HostKind = TerminalHostKind.Fullscreen });
        var runTask = app.RunInBackgroundAsync();

        await Task.Delay(60);
        backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Escape });
        await runTask.WaitAsync(TimeSpan.FromSeconds(2));

        var screen = new AnsiTestScreen(60, 8);
        screen.Apply(backend.GetOutText());
        var rows = screen.GetText().Split(Environment.NewLine);

        Assert.IsTrue(rows[0].Contains("Markup supports inline styling:", StringComparison.Ordinal));
        Assert.IsFalse(rows[0].Contains("success", StringComparison.Ordinal));
        Assert.IsTrue(rows[1].Contains("- success", StringComparison.Ordinal));
        Assert.IsTrue(rows[2].Contains("- warning", StringComparison.Ordinal));
    }
}

