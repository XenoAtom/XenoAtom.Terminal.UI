// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal;
using XenoAtom.Terminal.Backends;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Hosting;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class TerminalAppThemePropagationTests
{
    [TestMethod]
    public async Task FullscreenApp_UsesThemeFromUserRoot_WhenRootIsWrappedInWindowLayer()
    {
        var backend = new InMemoryTerminalBackend(new TerminalSize(10, 4));
        using var session = Terminal.Open(backend, new TerminalOptions { ImplicitStartInput = true }, force: true);

        var root = new TabControl(new TabPage("Tab1", "Content"));
        root.Style(Theme.DefaultLight);

        var options = new TerminalAppOptions { HostKind = TerminalHostKind.Fullscreen };
        await using var app = new TerminalApp(root, session.Instance, options);

        Assert.IsInstanceOfType<WindowLayer>(app.Root);
        Assert.AreSame(Theme.DefaultLight, app.Root.GetTheme());
    }
}
