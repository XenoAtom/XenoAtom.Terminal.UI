---
title: TreeView
---

# TreeView

`TreeView` displays hierarchical nodes with expand/collapse interaction and selection.


![TreeView](../../img/controls/treeview.svg){.terminal}

## Basic usage

```csharp
var root = new TreeNode("Root") { IsExpanded = true };
root.Children.Add(new TreeNode("Child A"));
root.Children.Add(new TreeNode("Child B"));

new TreeView()
    .Roots([root]);
```

## Interaction

- Arrow keys move selection and expand/collapse nodes (when supported by the current node).
- Mouse click selects a row; glyph areas allow expanding/collapsing.
- `SelectedIndex` is the index in the current flattened list of visible rows, not an index within `Roots` or within a parent node's `Children`.
- The flattened list contains only nodes that are currently visible after applying expansion/collapse state.
- Because of that, `SelectedIndex` can change meaning when nodes are expanded or collapsed. It is mainly useful for keyboard navigation and scroll positioning.
- `SelectedNode` exposes the selected `TreeNode` directly, so application code does not need to map the index back to a node.
- `TrySelectNode(node)` selects a visible node by reference.

## Selection example

```csharp
var root = new TreeNode("Root") { IsExpanded = true, Data = "Root" };
var child = new TreeNode("Child") { Data = "Child" };
root.Children.Add(child);

var tree = new TreeView([root]);

tree.TrySelectNode(child);
var selectedNode = tree.SelectedNode;
var selectedLabel = selectedNode?.Data as string;
```

If the visible rows are:

```text
0 Root
1 Child
2 Other
```

then `SelectedIndex == 1` means `Child` is selected. If `Root` is collapsed, the visible rows become:

```text
0 Root
1 Other
```

and `SelectedIndex == 1` now refers to `Other`. For application logic, prefer `SelectedNode`.

## Defaults

- Default alignment: `HorizontalAlignment = Align.Stretch`, `VerticalAlignment = Align.Stretch`

## Node visuals

Nodes can use glyphs/icons (e.g. folder/file) and are composable visuals.

Each node is represented by a `TreeNode`:

- `Header` is a `Visual` (anything can be used: text, markup, composed layouts).
- `Children` is a bindable list of nodes.
- `IsExpanded` controls whether children are visible.

## Styling

`TreeViewStyle` controls indentation, glyphs, spacing, and selection colors.

## Related

- [Binding](../binding.md)
- [ScrollViewer](scrollviewer.md)
- [TreeView Specs](../specs/controls/treeview.md)
