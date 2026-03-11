using System.Text;
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
        context.AllowPageScrollViewer = false;

        static TreeNode Node(string label, Rune icon, bool isExpanded = false)
            => new TreeNode(label) { Data = label, Icon = icon, IsExpanded = isExpanded };

        static string GetNodePath(TreeNode? node)
        {
            if (node is null)
            {
                return "<none>";
            }

            var parts = new Stack<string>();
            for (var current = node; current is not null; current = current.Parent)
            {
                parts.Push(current.Data as string ?? current.Header.ToString() ?? "<node>");
            }

            var builder = new StringBuilder();
            var isFirst = true;
            foreach (var part in parts)
            {
                if (!isFirst)
                {
                    builder.Append(" / ");
                }

                builder.Append(part);
                isFirst = false;
            }

            return builder.ToString();
        }

        static (TreeView Tree, TreeNode NestedNode) CreateTree()
        {
            var tree = new TreeView().MinHeight(12);

            var root = Node("Root", TreeNodeIcons.FolderGlyph, isExpanded: true);
            root.Children.Add(Node("File A", TreeNodeIcons.FileGlyph));
            root.Children.Add(Node("File B", TreeNodeIcons.FileGlyph));

            var nested = Node("Folder", TreeNodeIcons.FolderGlyph, isExpanded: true);
            nested.Children.Add(Node("Nested 1", TreeNodeIcons.DocumentGlyph));
            var nestedNode = Node("Nested 2", TreeNodeIcons.DocumentGlyph);
            nested.Children.Add(nestedNode);
            root.Children.Add(nested);

            tree.Roots.Add(root);
            tree.Roots.Add(Node("Other", TreeNodeIcons.FolderGlyph));
            return (tree, nestedNode);
        }

        static TreeView CreateLongTree()
        {
            var tree = new TreeView();
            for (var i = 0; i < 30; i++)
            {
                var node = Node($"Node {i:00}", TreeNodeIcons.FolderGlyph);
                if (i % 6 == 0)
                {
                    node.IsExpanded = true;
                    for (var j = 0; j < 4; j++)
                    {
                        node.Children.Add(Node($"Child {i:00}.{j}", TreeNodeIcons.FileGlyph));
                    }
                }

                tree.Roots.Add(node);
            }

            return tree;
        }

        var (defaultTree, nestedNode) = CreateTree();
        var (noLinesTree, _) = CreateTree();
        noLinesTree.Style(TreeViewStyle.NoLines);
        var (heavyLinesTree, _) = CreateTree();
        heavyLinesTree.Style(TreeViewStyle.HeavyLines);
        var longTree = CreateLongTree();

        return new VStack(
                DemoUi.Hint("Use arrows to navigate. Use Left/Right to collapse/expand."),
                new HStack(
                        new Button("Select Nested 2").Click(() => defaultTree.TrySelectNode(nestedNode)),
                        new TextBlock(() => $"SelectedIndex: {defaultTree.SelectedIndex}"),
                        new TextBlock(() => $"SelectedNode: {GetNodePath(defaultTree.SelectedNode)}")
                    )
                    .Spacing(2),
                new HStack(
                        new Group().TopLeftText("Default").Content(defaultTree),
                        new Group().TopLeftText("No lines").Content(noLinesTree),
                        new Group().TopLeftText("Heavy").Content(heavyLinesTree)
                    )
                    .Spacing(2),
                DemoUi.Hint("Large trees can be hosted in a ScrollViewer."),
                new Border(new ScrollViewer(longTree)).MinHeight(12).MaxHeight(12)
            )
            .Spacing(1);
    }
}
