// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI.Controls;

/// <summary>
/// Represents a vertical splitter that arranges panes stacked vertically.
/// </summary>
public sealed partial class VSplitter : SplitterBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="VSplitter"/> class.
    /// </summary>
    public VSplitter()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="VSplitter"/> class with two panes.
    /// </summary>
    /// <param name="first">The first pane.</param>
    /// <param name="second">The second pane.</param>
    public VSplitter(Visual first, Visual second)
    {
        First = first;
        Second = second;
    }

    /// <inheritdoc />
    protected override Orientation SplitOrientation => Orientation.Vertical;
}
