// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Collections;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.Controls;

public sealed class TreeNode
{
    private TreeView? _owner;

    public TreeNode(Visual header)
    {
        Header = header ?? throw new ArgumentNullException(nameof(header));
        Children = new BindableList<TreeNode>(
            owner: this,
            name: "TreeNode.Children",
            onAdding: child =>
            {
                child.Parent = this;
                _owner?.AttachNode(child);
            },
            onRemoving: child =>
            {
                _owner?.DetachNode(child);
                if (ReferenceEquals(child.Parent, this))
                {
                    child.Parent = null;
                }
            });
    }

    public Visual Header { get; }

    public BindableList<TreeNode> Children { get; }

    public TreeNode? Parent { get; private set; }

    public bool IsExpanded { get; set; }

    public TreeNodeIcon Icon { get; set; }

    internal void Attach(TreeView owner)
    {
        _owner = owner;
        for (var i = 0; i < Children.Count; i++)
        {
            owner.AttachNode(Children[i]);
        }
    }

    internal void Detach(TreeView owner)
    {
        _owner = null;
        for (var i = 0; i < Children.Count; i++)
        {
            owner.DetachNode(Children[i]);
        }
    }
}

