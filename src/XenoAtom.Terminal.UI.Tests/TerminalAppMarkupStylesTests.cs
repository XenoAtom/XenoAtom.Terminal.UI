// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Ansi;
using XenoAtom.Terminal;
using XenoAtom.Terminal.Backends;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Hosting;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class TerminalAppMarkupStylesTests
{
    [TestMethod]
    public async Task TerminalApp_SetsAndRestores_TerminalInstanceMarkupStyles()
    {
        var backend = new InMemoryTerminalBackend(new TerminalSize(20, 5));
        using var session = Terminal.Open(backend, new TerminalOptions { ImplicitStartInput = true }, force: true);

        var sentinel = new Dictionary<string, AnsiStyle>(StringComparer.Ordinal)
        {
            ["sentinel"] = new AnsiStyle { Foreground = AnsiColor.Basic16(1) },
        };
        session.Instance.MarkupStyles = sentinel;

        var root = new TextBlock("Hello").Style(Theme.Default);
        var options = new TerminalAppOptions { HostKind = TerminalHostKind.Inline };

        await using var app = new TerminalApp(root, session.Instance, options);
        app.BeginRun();

        Assert.AreNotSame(sentinel, session.Instance.MarkupStyles);
        Assert.AreSame(root.GetTheme().GetMarkupStyles(), session.Instance.MarkupStyles);

        app.EndRun();
        Assert.AreSame(sentinel, session.Instance.MarkupStyles);
    }
}

