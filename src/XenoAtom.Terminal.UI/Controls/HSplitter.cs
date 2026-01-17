// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI.Controls;

/// <summary>
/// Represents a horizontal splitter that arranges panes side by side.
/// </summary>
public sealed partial class HSplitter : SplitterBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HSplitter"/> class.
    /// </summary>
    public HSplitter()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="HSplitter"/> class with two panes.
    /// </summary>
    /// <param name="first">The first pane.</param>
    /// <param name="second">The second pane.</param>
    public HSplitter(Visual first, Visual second)
    {
        First = first;
        Second = second;
    }

    /// <inheritdoc />
    protected override Orientation SplitOrientation => Orientation.Horizontal;
}
