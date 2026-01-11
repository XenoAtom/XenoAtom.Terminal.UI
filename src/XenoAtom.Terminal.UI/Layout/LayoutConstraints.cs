// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Geometry;

namespace XenoAtom.Terminal.UI.Layout;

public readonly record struct LayoutConstraints
{
    public int MinWidth { get; init; }
    public int MaxWidth { get; init; }
    public int MinHeight { get; init; }
    public int MaxHeight { get; init; }

    public bool IsWidthBounded => MaxWidth < LayoutConstants.Infinite;
    public bool IsHeightBounded => MaxHeight < LayoutConstants.Infinite;

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

    public static LayoutConstraints Unbounded { get; } = new(0, LayoutConstants.Infinite, 0, LayoutConstants.Infinite);

    public static LayoutConstraints FromMaxSize(Size max)
        => new(0, max.Width, 0, max.Height);

    public Size Clamp(Size s)
    {
        var maxW = MaxWidth == LayoutConstants.Infinite ? LayoutConstants.MaxFinite : MaxWidth;
        var maxH = MaxHeight == LayoutConstants.Infinite ? LayoutConstants.MaxFinite : MaxHeight;
        return new Size(
            Math.Clamp(Math.Max(0, s.Width), MinWidth, maxW),
            Math.Clamp(Math.Max(0, s.Height), MinHeight, maxH));
    }

    public override string ToString()
        => $"Min({MinWidth},{MinHeight}) Max({(MaxWidth == LayoutConstants.Infinite ? "∞" : MaxWidth)},{(MaxHeight == LayoutConstants.Infinite ? "∞" : MaxHeight)})";
}

