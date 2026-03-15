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
public sealed partial class TreeNode : IVisualElement
{
    private TreeView? _owner;
    private readonly BindableList<TreeNodeRightVisual> _rightVisuals;

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
        _rightVisuals = new BindableList<TreeNodeRightVisual>(
            owner: this,
            name: "TreeNode.RightVisuals",
            onAdding: rightVisual => _owner?.AttachNodeRightVisual(rightVisual),
            onRemoving: rightVisual => _owner?.DetachNodeRightVisual(rightVisual));
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
    [Bindable]
    public BindableList<TreeNode> Children { get; }

    /// <summary>
    /// Gets the collection of visuals displayed at the right edge of this node row.
    /// </summary>
    /// <remarks>
    /// Right visuals are arranged from left to right as a trailing group aligned to the row end.
    /// Hover-only visuals appear to the left of always-visible visuals when the row is hovered.
    /// </remarks>
    [Bindable]
    public BindableList<TreeNodeRightVisual> RightVisuals => _rightVisuals;

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
    /// Gets or sets the optional style applied to the node icon.
    /// </summary>
    [Bindable]
    public partial Style? IconStyle { get; set; }

    /// <summary>
    /// Gets or sets arbitrary data associated with the node.
    /// </summary>
    [Bindable]
    public partial object? Data { get; set; }

    /// <summary>
    /// Adds an always-visible right-aligned visual to the node.
    /// </summary>
    /// <param name="visual">The visual to add.</param>
    /// <returns>The current node.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="visual"/> is <see langword="null"/>.</exception>
    public TreeNode AddRightVisual(Visual visual)
        => AddRightVisual(visual, TreeNodeRightVisualVisibility.Always);

    /// <summary>
    /// Adds a right-aligned visual to the node with the specified visibility mode.
    /// </summary>
    /// <param name="visual">The visual to add.</param>
    /// <param name="visibility">Controls when the visual is shown.</param>
    /// <returns>The current node.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="visual"/> is <see langword="null"/>.</exception>
    public TreeNode AddRightVisual(Visual visual, TreeNodeRightVisualVisibility visibility)
    {
        ArgumentNullException.ThrowIfNull(visual);
        RightVisuals.Add(new TreeNodeRightVisual(visual, visibility));
        return this;
    }

    internal void Attach(TreeView owner)
    {
        _owner = owner;
        for (var i = 0; i < RightVisuals.Count; i++)
        {
            owner.AttachNodeRightVisual(RightVisuals[i]);
        }

        for (var i = 0; i < Children.Count; i++)
        {
            owner.AttachNode(Children[i]);
        }
    }

    internal void Detach(TreeView owner)
    {
        _owner = null;
        for (var i = 0; i < RightVisuals.Count; i++)
        {
            owner.DetachNodeRightVisual(RightVisuals[i]);
        }

        for (var i = 0; i < Children.Count; i++)
        {
            owner.DetachNode(Children[i]);
        }
    }
}
