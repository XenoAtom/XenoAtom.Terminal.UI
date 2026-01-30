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

    /// <summary>
    /// Returns a value indicating whether this rectangle intersects the specified <paramref name="other"/> rectangle.
    /// </summary>
    /// <param name="other">The other rectangle.</param>
    /// <returns><c>true</c> if the rectangles overlap; otherwise <c>false</c>.</returns>
    public bool Intersects(in Rectangle other)
        => X < other.Right && Right > other.X && Y < other.Bottom && Bottom > other.Y;

    /// <summary>
    /// Returns a value indicating whether rectangles <paramref name="a"/> and <paramref name="b"/> intersect.
    /// </summary>
    /// <param name="a">The first rectangle.</param>
    /// <param name="b">The second rectangle.</param>
    /// <returns><c>true</c> if the rectangles overlap; otherwise <c>false</c>.</returns>
    public static bool Intersects(in Rectangle a, in Rectangle b) => a.Intersects(b);

    /// <summary>
    /// Returns the bounding rectangle that contains both this rectangle and <paramref name="other"/>.
    /// </summary>
    /// <param name="other">The other rectangle.</param>
    /// <returns>The union rectangle.</returns>
    public Rectangle Union(in Rectangle other) => Union(this, other);

    /// <summary>
    /// Returns the bounding rectangle that contains rectangles <paramref name="a"/> and <paramref name="b"/>.
    /// </summary>
    /// <param name="a">The first rectangle.</param>
    /// <param name="b">The second rectangle.</param>
    /// <returns>The union rectangle.</returns>
    public static Rectangle Union(in Rectangle a, in Rectangle b)
    {
        var x0 = Math.Min(a.X, b.X);
        var y0 = Math.Min(a.Y, b.Y);
        var x1 = Math.Max(a.Right, b.Right);
        var y1 = Math.Max(a.Bottom, b.Bottom);
        return new Rectangle(x0, y0, Math.Max(0, x1 - x0), Math.Max(0, y1 - y0));
    }

    /// <inheritdoc />
    public override string ToString() => $"({X},{Y}) {Width}x{Height}";
}

