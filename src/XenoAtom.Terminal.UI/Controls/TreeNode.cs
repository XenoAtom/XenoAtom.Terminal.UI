// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Text;
using XenoAtom.Terminal.UI.Collections;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.Controls;

/// <summary>
/// Represents a node within a <see cref="TreeView"/>.
/// </summary>
public sealed partial class TreeNode: IVisualElement
{
    private TreeView? _owner;

    /// <summary>
    /// Initializes a new instance of the <see cref="TreeNode"/> class.
    /// </summary>
    /// <param name="header">The node header visual.</param>
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

    /// <summary>
    /// Gets the header visual for the node.
    /// </summary>
    public Visual Header { get; }

    /// <inheritdoc />
    public TerminalApp? App => Header.App;

    /// <summary>
    /// Gets the child nodes collection.
    /// </summary>
    public BindableList<TreeNode> Children { get; }

    /// <summary>
    /// Gets the parent node, if any.
    /// </summary>
    public TreeNode? Parent { get; private set; }

    /// <summary>
    /// Gets or sets a value indicating whether the node is expanded.
    /// </summary>
    [Bindable]
    public partial bool IsExpanded { get; set; }

    /// <summary>
    /// Gets or sets the node icon glyph.
    /// </summary>
    [Bindable]
    public partial Rune? Icon { get; set; }

    /// <summary>
    /// Gets or sets arbitrary data associated with the node.
    /// </summary>
    [Bindable]
    public partial object? Data { get; set; }

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
