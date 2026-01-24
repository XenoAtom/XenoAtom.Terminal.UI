// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI.Controls;

/// <summary>
/// Arranges children vertically and wraps them onto new columns when they exceed the available height.
/// </summary>
public sealed partial class WrapVStack : WrapStackBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WrapVStack"/> class.
    /// </summary>
    public WrapVStack()
    {
        VerticalAlignment = Align.Start;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="WrapVStack"/> class with children.
    /// </summary>
    /// <param name="children">The child visuals.</param>
    public WrapVStack(params Visual[] children)
    {
        VerticalAlignment = Align.Start;
        AddRange(children);
    }

    /// <inheritdoc />
    protected override bool IsHorizontal => false;
}

