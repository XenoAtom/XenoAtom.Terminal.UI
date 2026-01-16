// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI.Geometry;

/// <summary>
/// Represents a thickness around a rectangle, expressed in cell units.
/// </summary>
public readonly record struct Thickness(int Left, int Top, int Right, int Bottom)
{
    /// <summary>
    /// Initializes a uniform thickness.
    /// </summary>
    /// <param name="uniform">The uniform thickness value applied to all sides.</param>
    public Thickness(int uniform) : this(uniform, uniform, uniform, uniform)
    {
    }

    /// <summary>
    /// Gets a zero thickness.
    /// </summary>
    public static readonly Thickness Zero = new(0);

    /// <summary>
    /// Converts a uniform integer thickness to a <see cref="Thickness"/> instance.
    /// </summary>
    /// <param name="uniform">The uniform thickness value applied to all sides.</param>
    public static implicit operator Thickness(int uniform) => new(uniform);

    /// <summary>
    /// Gets the sum of the left and right values.
    /// </summary>
    public int Horizontal => Left + Right;

    /// <summary>
    /// Gets the sum of the top and bottom values.
    /// </summary>
    public int Vertical => Top + Bottom;
}
