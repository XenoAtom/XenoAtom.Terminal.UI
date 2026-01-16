// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI.Geometry;

/// <summary>
/// Represents an integer size in cell coordinates.
/// </summary>
/// <param name="Width">The width.</param>
/// <param name="Height">The height.</param>
public readonly record struct Size(int Width, int Height)
{
    /// <summary>
    /// Gets a size with both dimensions set to 0.
    /// </summary>
    public static readonly Size Zero = new(0, 0);

    /// <inheritdoc />
    public override string ToString() => $"{Width}x{Height}";
}

