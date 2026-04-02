using System.Text;
using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.ControlsDemo.Demos;

[Demo("TreeView", "Input", Description = "Hierarchical navigation with expand/collapse and row actions.")]
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

        static TreeView CreateStyledIconTree()
        {
            var tree = new TreeView().MinHeight(8);

            var services = Node("Services", NerdFont.CodFolder, isExpanded: true)
                .StyleIcon(Colors.DodgerBlue);
            services.Children.Add(Node("API", NerdFont.CodFileCode).StyleIcon(Colors.MediumSeaGreen));
            services.Children.Add(Node("Worker", NerdFont.CodFile).StyleIcon(Colors.Goldenrod));
            services.Children.Add(Node("Alerts", NerdFont.OctAlert).StyleIcon(Colors.OrangeRed));

            tree.Roots.Add(services);
            tree.Roots.Add(Node("Archive", NerdFont.OctArchive).StyleIcon(Colors.MediumPurple));
            return tree;
        }

        TreeView CreateActionTree(DemoContext context)
        {
            var tree = new TreeView().MinHeight(8);

            var inbox = Node("Inbox", NerdFont.OctInbox, isExpanded: true);

            inbox.Children.Add(
                Node("Build pipeline", NerdFont.OctAlert)
                    .AddRightVisual(TreeNodeDemoExtensions.CreateStatusGlyph("!").Margin(new Thickness(1, 0, 0, 0)))
                    .AddRightVisual(TreeNodeDemoExtensions.CreateActionButton("x", ControlTone.Error, () => context.Log("Dismissed: Build pipeline")).Margin(new Thickness(1, 0, 0, 0)), TreeNodeRightVisualVisibility.Hover));

            inbox.Children.Add(
                Node("Release notes", NerdFont.FaFileTextO)
                    .AddRightVisual(TreeNodeDemoExtensions.CreateStatusGlyph("●", Colors.MediumSeaGreen).Margin(new Thickness(1, 0, 0, 0))));

            inbox.Children.Add(
                Node("Deploy preview", NerdFont.CodCloud)
                    .AddRightVisual(TreeNodeDemoExtensions.CreateStatusGlyph("…", Colors.Goldenrod).Margin(new Thickness(1, 0, 0, 0)))
                    .AddRightVisual(TreeNodeDemoExtensions.CreateActionButton("↻", ControlTone.Primary, () => context.Log("Retry deploy preview")).Margin(new Thickness(1, 0, 0, 0)), TreeNodeRightVisualVisibility.Hover));

            tree.Roots.Add(inbox);
            tree.Roots.Add(Node("Archive", NerdFont.OctArchive));
            return tree;
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

        static TreeView CreateTrimmedRightVisualTree()
        {
            static TreeNode TrimmedNode(string label, Rune icon, string statusGlyph, Color statusColor)
            {
                var node = new TreeNode(new TextBlock(label) { Trimming = TextTrimming.EndEllipsis })
                {
                    Data = label,
                    Icon = icon,
                };

                var rightVisual = TreeNodeDemoExtensions.CreateStatusGlyph(statusGlyph, statusColor);
                rightVisual.Margin = new Thickness(1, 0, 0, 0);
                node.AddRightVisual(rightVisual);
                return node;
            }

            var tree = new TreeView().MinHeight(7).HorizontalAlignment(Align.Stretch).VerticalAlignment(Align.Stretch);

            var workspaces = TrimmedNode("Workspace services / backend / release candidate validation", NerdFont.CodFolderLibrary, "●", Colors.MediumSeaGreen);
            workspaces.IsExpanded = true;
            workspaces.Children.Add(TrimmedNode("Feature flag rollout configuration for customer preview", NerdFont.CodFileCode, "⚙", Colors.Goldenrod));
            workspaces.Children.Add(TrimmedNode("Incident timeline and remediation notes for April hotfix", NerdFont.CodFile, "⚠", Colors.OrangeRed));
            workspaces.Children.Add(TrimmedNode("Post-deployment smoke tests and production checklist", NerdFont.CodChecklist, "✓", Colors.DeepSkyBlue));

            tree.Roots.Add(workspaces);
            tree.Roots.Add(TrimmedNode("Archive / historical builds / 2026 quarterly compliance package", NerdFont.OctArchive, "○", Colors.MediumPurple));
            return tree;
        }

        static Dialog CreateTrimmedDialog()
        {
            Dialog? dialog = null;
            var tree = CreateTrimmedRightVisualTree();

            dialog = new Dialog()
                .Title("TreeView trimming with right visuals")
                .TopRightText(new TextBlock("Resizable").Style(TextBlockStyle.Default with { Foreground = Colors.DeepSkyBlue }))
                .BottomLeftText("Resize narrower to trigger ellipsis")
                .BottomRightText("Drag edge")
                .IsModal(false)
                .Padding(1)
                .MinWidth(38)
                .MinHeight(10)
                .Width(56)
                .Height(13)
                .Content(new VStack(
                        DemoUi.Hint("Node headers use TextBlock.Trimming(TextTrimming.EndEllipsis)."),
                        new Border(tree)
                            .Padding(new Thickness(1, 0, 1, 0))
                            .HorizontalAlignment(Align.Stretch)
                            .VerticalAlignment(Align.Stretch),
                        new Button("Close").Click(() => dialog!.Close()))
                    .Spacing(1)
                    .HorizontalAlignment(Align.Stretch)
                    .VerticalAlignment(Align.Stretch));

            return dialog;
        }

        var (defaultTree, nestedNode) = CreateTree();
        var (noLinesTree, _) = CreateTree();
        noLinesTree.Style(TreeViewStyle.NoLines);
        var (heavyLinesTree, _) = CreateTree();
        heavyLinesTree.Style(TreeViewStyle.HeavyLines);
        var styledIconTree = CreateStyledIconTree();
        var actionTree = CreateActionTree(context);
        var longTree = CreateLongTree();
        var showTrimmedDialog = new Button("Show trimming dialog").Click(() => CreateTrimmedDialog().Show());

        var root = new VStack(
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
                new HStack(
                        new Group().TopLeftText("Styled icons").Content(styledIconTree),
                        new Group().TopLeftText("Row actions").Content(actionTree)
                    )
                    .Spacing(2),
                DemoUi.Hint("Hover a row in the action tree to reveal hover-only buttons; always-visible indicators stay pinned to the far right."),
                new HStack(
                        DemoUi.Hint("Open the dialog to test header trimming with long labels and right-aligned icons."),
                        showTrimmedDialog)
                    .Spacing(2),
                DemoUi.Hint("Large trees can be hosted in a ScrollViewer."),
                new Border(new ScrollViewer(longTree)).MinHeight(12).MaxHeight(12)
            )
            .Spacing(1);

        return root.InScreenshot(context, () =>
        {
            var dialog = CreateTrimmedDialog();
            dialog.Show();
        });
    }
}

file static class TreeNodeDemoExtensions
{
    public static TreeNode StyleIcon(this TreeNode node, Color color)
    {
        node.IconStyle = Style.None.WithForeground(color);
        return node;
    }

    public static Visual CreateStatusGlyph(string text, Color? color = null)
    {
        var visual = (Visual)text;
        if (color is { } resolved)
        {
            visual.Style(TextBlockStyle.Default with { Foreground = resolved });
        }

        return visual;
    }

    public static Button CreateActionButton(string text, ControlTone tone, Action action)
        => new Button(text)
            .Style(ButtonStyle.Default with { Padding = Thickness.Zero })
            .Tone(tone)
            .Click(action);
}
