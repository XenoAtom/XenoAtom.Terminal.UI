// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Reflection;
using XenoAtom.Terminal.UI.Rendering;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class ColorAlphaBlendingTests
{
    [TestMethod]
    public void CellBuffer_Blends_Rgba_Background_Over_Rgb_Background()
    {
        var buffer = new CellBuffer(1, 1);
        buffer.Clear();

        // Base: opaque black.
        buffer.SetCell(0, 0, new Rune('X'), Style.None.WithBackground(Color.Rgb(0, 0, 0)));

        // Overlay: semi-transparent white.
        buffer.SetCell(0, 0, new Rune('X'), Style.None.WithBackground(Color.RgbA(255, 255, 255, 128)));

        var cells = (Style[])typeof(CellBuffer).GetField("_cells", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(buffer)!;
        Assert.IsTrue(cells[0].TryGetBackground(out var bg));

        Assert.AreEqual(ColorKind.Rgb, bg.Kind, "Expected alpha blending to resolve to an opaque RGB color.");

        var expected = BlendLinear(Color.RgbA(255, 255, 255, 128), Color.Rgb(0, 0, 0));
        AssertClose(expected.R, bg.R);
        AssertClose(expected.G, bg.G);
        AssertClose(expected.B, bg.B);
    }

    [TestMethod]
    public void CellBuffer_Blends_Rgba_Foreground_Over_Rgb_Background()
    {
        var buffer = new CellBuffer(1, 1);
        buffer.Clear();

        // Base: opaque blue background.
        buffer.SetCell(0, 0, new Rune('X'), Style.None.WithBackground(Color.Rgb(0, 0, 255)));

        // Overlay: semi-transparent red foreground. Background is inherited from the base.
        buffer.SetCell(0, 0, new Rune('X'), Style.None.WithForeground(Color.RgbA(255, 0, 0, 128)));

        var cells = (Style[])typeof(CellBuffer).GetField("_cells", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(buffer)!;
        Assert.IsTrue(cells[0].TryGetForeground(out var fg));
        Assert.IsTrue(cells[0].TryGetBackground(out var bg));

        Assert.AreEqual(ColorKind.Rgb, fg.Kind, "Expected alpha blending to resolve to an opaque RGB color.");
        Assert.AreEqual(ColorKind.Rgb, bg.Kind);

        var expected = BlendLinear(Color.RgbA(255, 0, 0, 128), bg);
        AssertClose(expected.R, fg.R);
        AssertClose(expected.G, fg.G);
        AssertClose(expected.B, fg.B);
    }

    private static void AssertClose(byte expected, byte actual)
    {
        // The production code uses LUTs for speed; allow a small tolerance.
        Assert.IsLessThanOrEqualTo(1, Math.Abs(expected - actual), $"Expected {expected} but got {actual}.");
    }

    private static Color BlendLinear(Color src, Color dst)
    {
        var sa = src.A / 255.0;
        var invSa = 1.0 - sa;

        var srcR = SrgbToLinear(src.R);
        var srcG = SrgbToLinear(src.G);
        var srcB = SrgbToLinear(src.B);

        var dstR = SrgbToLinear(dst.R);
        var dstG = SrgbToLinear(dst.G);
        var dstB = SrgbToLinear(dst.B);

        var outR = (srcR * sa) + (dstR * invSa);
        var outG = (srcG * sa) + (dstG * invSa);
        var outB = (srcB * sa) + (dstB * invSa);

        return Color.Rgb(LinearToSrgb(outR), LinearToSrgb(outG), LinearToSrgb(outB));
    }

    private static double SrgbToLinear(byte value)
    {
        var srgb = value / 255.0;
        return srgb <= 0.04045 ? srgb / 12.92 : Math.Pow((srgb + 0.055) / 1.055, 2.4);
    }

    private static byte LinearToSrgb(double linear)
    {
        linear = Math.Clamp(linear, 0.0, 1.0);
        var srgb = linear <= 0.0031308 ? 12.92 * linear : (1.055 * Math.Pow(linear, 1.0 / 2.4)) - 0.055;
        var value = (int)Math.Round(srgb * 255.0);
        return (byte)Math.Clamp(value, 0, 255);
    }
}
