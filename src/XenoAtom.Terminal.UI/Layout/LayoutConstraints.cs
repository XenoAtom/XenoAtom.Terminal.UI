// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Geometry;

namespace XenoAtom.Terminal.UI.Layout;

/// <summary>
/// Describes per-axis layout constraints used during a measure pass.
/// </summary>
/// <remarks>
/// Constraints are integer cell bounds. The maximum may be unbounded and is represented by <see cref="LayoutConstants.Infinite"/>.
/// </remarks>
public readonly record struct LayoutConstraints
{
    /// <summary>
    /// Gets the minimum width constraint.
    /// </summary>
    public int MinWidth { get; init; }

    /// <summary>
    /// Gets the maximum width constraint, or <see cref="LayoutConstants.Infinite"/> for unbounded.
    /// </summary>
    public int MaxWidth { get; init; }

    /// <summary>
    /// Gets the minimum height constraint.
    /// </summary>
    public int MinHeight { get; init; }

    /// <summary>
    /// Gets the maximum height constraint, or <see cref="LayoutConstants.Infinite"/> for unbounded.
    /// </summary>
    public int MaxHeight { get; init; }

    /// <summary>
    /// Gets a value indicating whether the width is bounded.
    /// </summary>
    public bool IsWidthBounded => MaxWidth < LayoutConstants.Infinite;

    /// <summary>
    /// Gets a value indicating whether the height is bounded.
    /// </summary>
    public bool IsHeightBounded => MaxHeight < LayoutConstants.Infinite;

    /// <summary>
    /// Initializes a new instance of the <see cref="LayoutConstraints"/> struct.
    /// </summary>
    /// <param name="minWidth">The minimum width.</param>
    /// <param name="maxWidth">The maximum width, or <see cref="LayoutConstants.Infinite"/> for unbounded.</param>
    /// <param name="minHeight">The minimum height.</param>
    /// <param name="maxHeight">The maximum height, or <see cref="LayoutConstants.Infinite"/> for unbounded.</param>
    public LayoutConstraints(int minWidth, int maxWidth, int minHeight, int maxHeight)
    {
        minWidth = Math.Max(0, minWidth);
        minHeight = Math.Max(0, minHeight);

        maxWidth = Math.Max(0, maxWidth);
        maxHeight = Math.Max(0, maxHeight);

        if (maxWidth > LayoutConstants.Infinite)
        {
            maxWidth = LayoutConstants.Infinite;
        }

        if (maxHeight > LayoutConstants.Infinite)
        {
            maxHeight = LayoutConstants.Infinite;
        }

        if (maxWidth < minWidth)
        {
            maxWidth = minWidth;
        }

        if (maxHeight < minHeight)
        {
            maxHeight = minHeight;
        }

        MinWidth = minWidth;
        MaxWidth = maxWidth;
        MinHeight = minHeight;
        MaxHeight = maxHeight;
    }

    /// <summary>
    /// Gets unbounded constraints in both axes.
    /// </summary>
    public static LayoutConstraints Unbounded { get; } = new(0, LayoutConstants.Infinite, 0, LayoutConstants.Infinite);

    /// <summary>
    /// Creates constraints with maximum bounds from a size.
    /// </summary>
    /// <param name="max">The maximum size.</param>
    /// <returns>The constraints instance.</returns>
    public static LayoutConstraints FromMaxSize(Size max)
        => new(0, max.Width, 0, max.Height);

    /// <summary>
    /// Clamps a size to be within the constraint bounds.
    /// </summary>
    /// <param name="s">The size to clamp.</param>
    /// <returns>The clamped size.</returns>
    public Size Clamp(Size s)
    {
        var maxW = MaxWidth == LayoutConstants.Infinite ? LayoutConstants.MaxFinite : MaxWidth;
        var maxH = MaxHeight == LayoutConstants.Infinite ? LayoutConstants.MaxFinite : MaxHeight;
        return new Size(
            Math.Clamp(Math.Max(0, s.Width), MinWidth, maxW),
            Math.Clamp(Math.Max(0, s.Height), MinHeight, maxH));
    }

    /// <inheritdoc />
    public override string ToString()
        => $"Min({MinWidth},{MinHeight}) Max({(MaxWidth == LayoutConstants.Infinite ? "ė" : MaxWidth)},{(MaxHeight == LayoutConstants.Infinite ? "ė" : MaxHeight)})";
}
