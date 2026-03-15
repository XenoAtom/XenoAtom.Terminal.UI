// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI.Controls;

/// <summary>
/// Specifies when a <see cref="TreeNodeRightVisual"/> is shown.
/// </summary>
public enum TreeNodeRightVisualVisibility
{
    /// <summary>
    /// The visual is always shown when its node is visible.
    /// </summary>
    Always = 0,

    /// <summary>
    /// The visual is shown only while its node row is hovered.
    /// </summary>
    Hover = 1,
}

/// <summary>
/// Represents a visual displayed at the right edge of a <see cref="TreeNode"/> row.
/// </summary>
public sealed partial class TreeNodeRightVisual : IVisualElement
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TreeNodeRightVisual"/> class.
    /// </summary>
    /// <param name="visual">The visual displayed at the row end.</param>
    /// <exception cref="ArgumentNullException"><paramref name="visual"/> is <see langword="null"/>.</exception>
    public TreeNodeRightVisual(Visual visual)
        : this(visual, TreeNodeRightVisualVisibility.Always)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TreeNodeRightVisual"/> class.
    /// </summary>
    /// <param name="visual">The visual displayed at the row end.</param>
    /// <param name="visibility">Controls when the visual is shown.</param>
    /// <exception cref="ArgumentNullException"><paramref name="visual"/> is <see langword="null"/>.</exception>
    public TreeNodeRightVisual(Visual visual, TreeNodeRightVisualVisibility visibility)
    {
        Visual = visual ?? throw new ArgumentNullException(nameof(visual));
        Visibility = visibility;
    }

    /// <summary>
    /// Gets the visual displayed at the row end.
    /// </summary>
    public Visual Visual { get; }

    /// <inheritdoc />
    public TerminalApp? App => Visual.App;

    /// <summary>
    /// Gets or sets when the visual is shown.
    /// </summary>
    [Bindable]
    public partial TreeNodeRightVisualVisibility Visibility { get; set; }
}
