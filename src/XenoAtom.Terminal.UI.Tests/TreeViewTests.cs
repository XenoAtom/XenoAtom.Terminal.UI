// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Linq;
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

    [TestMethod]
    public void TreeView_Renders_Hierarchy_Lines_By_Default()
    {
        var tree = new TreeView();

        var root1 = new TreeNode("Root1") { Icon = TreeNodeIcons.FolderGlyph, IsExpanded = true };
        root1.Children.Add(new TreeNode("Child1") { Icon = TreeNodeIcons.FileGlyph });
        root1.Children.Add(new TreeNode("Child2") { Icon = TreeNodeIcons.FileGlyph });

        var folder = new TreeNode("Folder") { Icon = TreeNodeIcons.FolderGlyph, IsExpanded = true };
        folder.Children.Add(new TreeNode("Nested1") { Icon = TreeNodeIcons.DocumentGlyph });
        folder.Children.Add(new TreeNode("Nested2") { Icon = TreeNodeIcons.DocumentGlyph });
        root1.Children.Add(folder);
        tree.Roots.Add(root1);

        // Adding a second root ensures continuation lines are visible under Root1's children.
        tree.Roots.Add(new TreeNode("Root2") { Icon = TreeNodeIcons.FolderGlyph });

        var root = new VStack { tree };
        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(40, 10));
        driver.Tick();

        var screen = new AnsiTestScreen(40, 10);
        screen.Apply(driver.Backend.GetOutText());
        var rendered = screen.GetText();

        StringAssert.Contains(rendered, "Root1");
        StringAssert.Contains(rendered, "Child1");
        StringAssert.Contains(rendered, "Child2");
        StringAssert.Contains(rendered, "Nested1");
        StringAssert.Contains(rendered, "Nested2");

        // Tree line glyphs (single line set).
        StringAssert.Contains(rendered, "│");
        StringAssert.Contains(rendered, "├");
        StringAssert.Contains(rendered, "└");
    }

    [TestMethod]
    public void TreeView_Renders_Connecting_Lines_For_Root_Siblings()
    {
        var tree = new TreeView();

        var root1 = new TreeNode("Root1") { Icon = TreeNodeIcons.FolderGlyph, IsExpanded = true };
        root1.Children.Add(new TreeNode("Child1") { Icon = TreeNodeIcons.FileGlyph });
        tree.Roots.Add(root1);
        tree.Roots.Add(new TreeNode("Root2") { Icon = TreeNodeIcons.FolderGlyph });

        var root = new VStack { tree };
        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(40, 8));
        driver.Tick();

        var screen = new AnsiTestScreen(40, 8);
        screen.Apply(driver.Backend.GetOutText());
        var lines = screen.GetText().Split('\n');

        var root1Line = lines.FirstOrDefault(l => l.Contains("Root1", StringComparison.Ordinal));
        Assert.IsNotNull(root1Line);
        StringAssert.Contains(root1Line, "├");

        var root2Line = lines.FirstOrDefault(l => l.Contains("Root2", StringComparison.Ordinal));
        Assert.IsNotNull(root2Line);
        StringAssert.Contains(root2Line, "└");
    }

    [TestMethod]
    public void TreeView_Does_Not_Render_Hierarchy_Lines_When_Disabled()
    {
        var tree = new TreeView().Style(TreeViewStyle.NoLines);

        var root1 = new TreeNode("Root1") { Icon = TreeNodeIcons.FolderGlyph, IsExpanded = true };
        root1.Children.Add(new TreeNode("Child1") { Icon = TreeNodeIcons.FileGlyph });
        root1.Children.Add(new TreeNode("Child2") { Icon = TreeNodeIcons.FileGlyph });
        tree.Roots.Add(root1);
        tree.Roots.Add(new TreeNode("Root2") { Icon = TreeNodeIcons.FolderGlyph });

        var root = new VStack { tree };
        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(40, 10));
        driver.Tick();

        var screen = new AnsiTestScreen(40, 10);
        screen.Apply(driver.Backend.GetOutText());
        var rendered = screen.GetText();

        Assert.DoesNotContain("│", rendered);
        Assert.DoesNotContain("├", rendered);
        Assert.DoesNotContain("└", rendered);
    }
}
