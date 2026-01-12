using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.ControlsDemo.Demos;

[Demo("TreeView", "Navigation", Description = "Hierarchical navigation with expand/collapse.")]
public sealed class TreeViewDemo : ControlsDemoBase
{
    public TreeViewDemo() : base(DemoSource.Get())
    {
    }

    public override Visual Build(DemoContext context)
    {
        var tree = new TreeView();

        var root = new TreeNode("Root") { IsExpanded = true, Icon = TreeNodeIcons.FolderGlyph };
        root.Children.Add(new TreeNode("File A") { Icon = TreeNodeIcons.FileGlyph });
        root.Children.Add(new TreeNode("File B") { Icon = TreeNodeIcons.FileGlyph });

        var nested = new TreeNode("Folder") { IsExpanded = true, Icon = TreeNodeIcons.FolderGlyph };
        nested.Children.Add(new TreeNode("Nested 1") { Icon = TreeNodeIcons.DocumentGlyph });
        nested.Children.Add(new TreeNode("Nested 2") { Icon = TreeNodeIcons.DocumentGlyph });
        root.Children.Add(nested);

        tree.Roots.Add(root);

        return new VStack(
                DemoUi.Hint("Use arrows to navigate. Use Left/Right to collapse/expand."),
                tree,
                new TextBlock(() => $"SelectedIndex: {tree.SelectedIndex}")
            )
            .Spacing(1);
    }
}
