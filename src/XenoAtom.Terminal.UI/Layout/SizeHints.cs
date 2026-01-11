// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Geometry;

namespace XenoAtom.Terminal.UI.Layout;

public readonly record struct SizeHints
{
    public Size Min { get; init; }
    public Size Natural { get; init; }
    public Size Max { get; init; }

    public int FlexGrowX { get; init; }
    public int FlexGrowY { get; init; }
    public int FlexShrinkX { get; init; }
    public int FlexShrinkY { get; init; }

    public static SizeHints Fixed(Size size)
    {
        var s = ClampFinite(size);
        return new SizeHints
        {
            Min = s,
            Natural = s,
            Max = s,
            FlexGrowX = 0,
            FlexGrowY = 0,
            FlexShrinkX = 0,
            FlexShrinkY = 0,
        };
    }

    public static SizeHints Flex(Size min, Size natural, Size max, int growX, int growY, int shrinkX, int shrinkY)
        => new()
        {
            Min = ClampFinite(min),
            Natural = ClampFinite(natural),
            Max = ClampMax(max),
            FlexGrowX = Math.Max(0, growX),
            FlexGrowY = Math.Max(0, growY),
            FlexShrinkX = Math.Max(0, shrinkX),
            FlexShrinkY = Math.Max(0, shrinkY),
        };

    public static SizeHints FlexX(Size min, Size natural, int growX = 1, int shrinkX = 1)
        => Flex(min, natural, new Size(LayoutConstants.Infinite, natural.Height), growX, 0, shrinkX, 0);

    public static SizeHints FlexY(Size min, Size natural, int growY = 1, int shrinkY = 1)
        => Flex(min, natural, new Size(natural.Width, LayoutConstants.Infinite), 0, growY, 0, shrinkY);

    public SizeHints Normalize()
    {
        var min = ClampFinite(Min);
        var nat = ClampFinite(Natural);
        nat = new Size(Math.Max(min.Width, nat.Width), Math.Max(min.Height, nat.Height));

        var max = ClampMax(Max);
        var maxW = max.Width == LayoutConstants.Infinite ? LayoutConstants.Infinite : Math.Max(nat.Width, max.Width);
        var maxH = max.Height == LayoutConstants.Infinite ? LayoutConstants.Infinite : Math.Max(nat.Height, max.Height);

        return this with
        {
            Min = min,
            Natural = nat,
            Max = new Size(maxW, maxH),
            FlexGrowX = Math.Max(0, FlexGrowX),
            FlexGrowY = Math.Max(0, FlexGrowY),
            FlexShrinkX = Math.Max(0, FlexShrinkX),
            FlexShrinkY = Math.Max(0, FlexShrinkY),
        };
    }

    private static Size ClampFinite(Size size)
        => new(
            Math.Clamp(size.Width, 0, LayoutConstants.MaxFinite),
            Math.Clamp(size.Height, 0, LayoutConstants.MaxFinite));

    private static Size ClampMax(Size size)
    {
        var w = size.Width;
        var h = size.Height;

        if (w < 0) w = 0;
        if (h < 0) h = 0;

        if (w >= LayoutConstants.Infinite) w = LayoutConstants.Infinite;
        if (h >= LayoutConstants.Infinite) h = LayoutConstants.Infinite;

        return new Size(w, h);
    }
}

