// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Hosting;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class TreeViewTests
{
    [TestMethod]
    public void TreeView_Expands_And_Shows_Children()
    {
        var tree = new TreeView();
        var rootNode = new TreeNode("Root") { Icon = TreeNodeIcons.FolderGlyph };
        rootNode.Children.Add(new TreeNode("Child") { Icon = TreeNodeIcons.FileGlyph });
        tree.Roots.Add(rootNode);

        var root = new VStack { tree };
        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(40, 10));
        driver.Tick();

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Right }); // expand
        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Down });  // select child
        driver.Tick();

        var screen = new AnsiTestScreen(40, 10);
        screen.Apply(driver.Backend.GetOutText());
        var rendered = screen.GetText();

        StringAssert.Contains(rendered, "Root");
        StringAssert.Contains(rendered, "Child");
    }
}

