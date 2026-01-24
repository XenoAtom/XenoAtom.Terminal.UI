// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI.Controls;

/// <summary>
/// Arranges children horizontally and wraps them onto new rows when they exceed the available width.
/// </summary>
public sealed partial class WrapHStack : WrapStackBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WrapHStack"/> class.
    /// </summary>
    public WrapHStack()
    {
        HorizontalAlignment = Align.Start;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="WrapHStack"/> class with children.
    /// </summary>
    /// <param name="children">The child visuals.</param>
    public WrapHStack(params Visual[] children)
    {
        HorizontalAlignment = Align.Start;
        AddRange(children);
    }

    /// <inheritdoc />
    protected override bool IsHorizontal => true;
}

