// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.Backends;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Hosting;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class TreeViewTests
{
    [TestMethod]
    public async Task TreeView_Expands_And_Shows_Children()
    {
        var backend = new InMemoryTerminalBackend(new TerminalSize(40, 10));
        using var session = Terminal.Open(backend, new TerminalOptions { ImplicitStartInput = true }, force: true);

        var tree = new TreeView();
        var rootNode = new TreeNode("Root") { Icon = TreeNodeIcons.FolderGlyph };
        rootNode.Children.Add(new TreeNode("Child") { Icon = TreeNodeIcons.FileGlyph });
        tree.Roots.Add(rootNode);

        var root = new VStack { tree };
        var app = new TerminalApp(root, session.Instance, new TerminalAppOptions { HostKind = TerminalHostKind.Fullscreen });
        var runTask = app.RunInBackgroundAsync();

        await Task.Delay(60);

        backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Right }); // expand
        backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Down });  // select child

        await Task.Delay(80);

        backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Escape });
        await runTask.WaitAsync(TimeSpan.FromSeconds(2));

        var outText = backend.GetOutText();
        var screen = new AnsiTestScreen(40, 10);
        screen.Apply(outText);
        var rendered = screen.GetText();

        StringAssert.Contains(rendered, "Root");
        StringAssert.Contains(rendered, "Child");
    }
}

