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
        var tree = new TreeView().Height(10);

        var root = new TreeNode("Root") { IsExpanded = true, Icon = TreeNodeIcon.Folder };
        root.Children.Add(new TreeNode("File A") { Icon = TreeNodeIcon.File });
        root.Children.Add(new TreeNode("File B") { Icon = TreeNodeIcon.File });

        var nested = new TreeNode("Folder") { IsExpanded = true, Icon = TreeNodeIcon.Folder };
        nested.Children.Add(new TreeNode("Nested 1") { Icon = TreeNodeIcon.Document });
        nested.Children.Add(new TreeNode("Nested 2") { Icon = TreeNodeIcon.Document });
        root.Children.Add(nested);

        tree.Roots.Add(root);

        return new VStack(
                DemoUi.Hint("Use arrows to navigate. Use Left/Right to collapse/expand."),
                tree,
                new TextBlock(() => $"SelectedIndex: {tree.SelectedIndex}"),
                new Button("Log selected").Click(() => context.Log($"SelectedIndex: {tree.SelectedIndex}")))
            .Spacing(1);
    }
}
