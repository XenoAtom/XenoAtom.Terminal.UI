// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Hosting;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class TreeViewScrollViewerTests
{
    [TestMethod]
    public void TreeView_Allows_ScrollViewer_To_Adjust_Offset()
    {
        var tree = CreateTree(rows: 20);
        var scrollViewer = new ScrollViewer(tree) { MinHeight = 3, MaxHeight = 3 };
        var root = new VStack(scrollViewer).Spacing(0);
        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(30, 8));
        driver.Tick();

        scrollViewer.VerticalOffset = 5;
        driver.Tick();

        Assert.IsGreaterThanOrEqualTo(5, tree.Scroll.OffsetY, "Expected the scroll model offset to update when the ScrollViewer is scrolled.");

        var screen = new AnsiTestScreen(30, 8);
        screen.Apply(driver.Backend.GetOutText());
        var rendered = screen.GetText();

        Assert.IsFalse(rendered.Contains("Node 00", StringComparison.Ordinal), "Expected the viewport to scroll past the first node.");
        StringAssert.Contains(rendered, "Node 05", "Expected the scrolled node to be visible.");
    }

    [TestMethod]
    public void TreeView_Scrolls_On_Wheel_When_Hosted_In_ScrollViewer()
    {
        var tree = CreateTree(rows: 30);
        var scrollViewer = new ScrollViewer(tree) { MinHeight = 3, MaxHeight = 3 };
        var root = new VStack(scrollViewer).Spacing(0);
        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(30, 8));
        driver.Tick();

        driver.Backend.PushEvent(new TerminalMouseEvent
        {
            Kind = TerminalMouseKind.Wheel,
            Button = TerminalMouseButton.Wheel,
            WheelDelta = -1,
            X = 1,
            Y = 1,
        });
        driver.TickUntil(() => tree.Scroll.OffsetY > 0);
    }

    [TestMethod]
    public void TreeView_Reports_Horizontal_Extent_For_Long_Content()
    {
        var tree = CreateTree(rows: 3, suffix: " - a very long node label");
        var scrollViewer = new ScrollViewer(tree) { MinHeight = 3, MaxHeight = 3, MinWidth = 10, MaxWidth = 10 };
        var root = new VStack(scrollViewer).Spacing(0);
        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(12, 8));
        driver.Tick();

        Assert.IsGreaterThan(tree.Scroll.ViewportWidth, tree.Scroll.ExtentWidth, "Expected a horizontal extent larger than the viewport for long node labels.");

        scrollViewer.HorizontalOffset = 1;
        driver.TickUntil(() => tree.Scroll.OffsetX == 1);
    }

    private static TreeView CreateTree(int rows, string suffix = "")
    {
        var tree = new TreeView();
        for (var i = 0; i < rows; i++)
        {
            tree.Roots.Add(new TreeNode($"Node {i:00}{suffix}") { Icon = TreeNodeIcons.FileGlyph });
        }

        return tree;
    }
}
