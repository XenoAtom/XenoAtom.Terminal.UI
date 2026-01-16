// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI.Geometry;

/// <summary>
/// Represents a rectangle in integer cell coordinates.
/// </summary>
/// <param name="X">The x coordinate.</param>
/// <param name="Y">The y coordinate.</param>
/// <param name="Width">The width.</param>
/// <param name="Height">The height.</param>
public readonly record struct Rectangle(int X, int Y, int Width, int Height)
{
    /// <summary>
    /// Gets the left edge coordinate.
    /// </summary>
    public int Left => X;
    /// <summary>
    /// Gets the top edge coordinate.
    /// </summary>
    public int Top => Y;
    /// <summary>
    /// Gets the right edge coordinate.
    /// </summary>
    public int Right => X + Width;
    /// <summary>
    /// Gets the bottom edge coordinate.
    /// </summary>
    public int Bottom => Y + Height;

    /// <summary>
    /// Returns a value indicating whether the specified point is contained in this rectangle.
    /// </summary>
    /// <param name="x">The x coordinate.</param>
    /// <param name="y">The y coordinate.</param>
    /// <returns><c>true</c> if the point is inside; otherwise <c>false</c>.</returns>
    public bool Contains(int x, int y) => x >= X && x < Right && y >= Y && y < Bottom;

    /// <inheritdoc />
    public override string ToString() => $"({X},{Y}) {Width}x{Height}";
}

