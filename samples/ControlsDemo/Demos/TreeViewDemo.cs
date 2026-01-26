using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.ControlsDemo.Demos;

[Demo("TreeView", "Input", Description = "Hierarchical navigation with expand/collapse.")]
public sealed class TreeViewDemo : ControlsDemoBase
{
    public TreeViewDemo() : base(DemoSource.Get())
    {
    }

    public override Visual Build(DemoContext context)
    {
        static TreeView CreateTree()
        {
            var tree = new TreeView().MinHeight(12);

            var root = new TreeNode("Root") { IsExpanded = true, Icon = TreeNodeIcons.FolderGlyph };
            root.Children.Add(new TreeNode("File A") { Icon = TreeNodeIcons.FileGlyph });
            root.Children.Add(new TreeNode("File B") { Icon = TreeNodeIcons.FileGlyph });

            var nested = new TreeNode("Folder") { IsExpanded = true, Icon = TreeNodeIcons.FolderGlyph };
            nested.Children.Add(new TreeNode("Nested 1") { Icon = TreeNodeIcons.DocumentGlyph });
            nested.Children.Add(new TreeNode("Nested 2") { Icon = TreeNodeIcons.DocumentGlyph });
            root.Children.Add(nested);

            tree.Roots.Add(root);
            tree.Roots.Add(new TreeNode("Other") { Icon = TreeNodeIcons.FolderGlyph });
            return tree;
        }

        static TreeView CreateLongTree()
        {
            var tree = new TreeView();
            for (var i = 0; i < 30; i++)
            {
                var node = new TreeNode($"Node {i:00}") { Icon = TreeNodeIcons.FolderGlyph };
                if (i % 6 == 0)
                {
                    node.IsExpanded = true;
                    for (var j = 0; j < 4; j++)
                    {
                        node.Children.Add(new TreeNode($"Child {i:00}.{j}") { Icon = TreeNodeIcons.FileGlyph });
                    }
                }

                tree.Roots.Add(node);
            }

            return tree;
        }

        var defaultTree = CreateTree();
        var noLinesTree = CreateTree().Style(TreeViewStyle.NoLines);
        var heavyLinesTree = CreateTree().Style(TreeViewStyle.HeavyLines);
        var longTree = CreateLongTree();

        return new VStack(
                DemoUi.Hint("Use arrows to navigate. Use Left/Right to collapse/expand."),
                new HStack(
                        new Group().TopLeftText("Default").Content(defaultTree),
                        new Group().TopLeftText("No lines").Content(noLinesTree),
                        new Group().TopLeftText("Heavy").Content(heavyLinesTree)
                    )
                    .Spacing(2),
                DemoUi.Hint("Large trees can be hosted in a ScrollViewer."),
                new ScrollViewer(longTree).MinHeight(12).MaxHeight(12),
                new TextBlock(() => $"SelectedIndex: {defaultTree.SelectedIndex}")
            )
            .Spacing(1);
    }
}
