// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class BrushTests
{
    [TestMethod]
    public void SolidBrush_Rejects_Default_Color()
    {
        Assert.ThrowsExactly<ArgumentException>(() => Brush.Solid(Color.Default));
    }

    [TestMethod]
    public void LinearGradient_Rejects_Invalid_Stops()
    {
        Assert.ThrowsExactly<ArgumentException>(() => Brush.LinearGradient(
            new GradientPoint(0f, 0f),
            new GradientPoint(1f, 0f),
            new GradientStop(0f, Colors.Red)));

        Assert.ThrowsExactly<ArgumentException>(() => Brush.LinearGradient(
            new GradientPoint(0f, 0f),
            new GradientPoint(1f, 0f),
            new GradientStop(0f, Colors.Red),
            new GradientStop(1.2f, Colors.Blue)));

        Assert.ThrowsExactly<ArgumentException>(() => Brush.LinearGradient(
            new GradientPoint(0f, 0f),
            new GradientPoint(1f, 0f),
            new GradientStop(0f, Colors.Red),
            new GradientStop(1f, Color.Default)));
    }

    [TestMethod]
    public void LinearGradient_Clamp_Uses_Edge_Stops_Outside_Rect()
    {
        var brush = Brush.LinearGradient(
            new GradientPoint(0f, 0f),
            new GradientPoint(1f, 0f),
            new GradientStop(0f, Colors.Red),
            new GradientStop(1f, Colors.Blue));

        var rect = new Rectangle(0, 0, 10, 1);
        var left = brush.Sample(-20, 0, rect, ColorMixSpace.Oklab);
        var right = brush.Sample(20, 0, rect, ColorMixSpace.Oklab);

        Assert.AreEqual(Colors.Red.ToRgb(), left.ToRgb());
        Assert.AreEqual(Colors.Blue.ToRgb(), right.ToRgb());
    }

    [TestMethod]
    public void LinearGradient_TileModes_Repeat_And_Mirror()
    {
        var repeatBrush = Brush.LinearGradient(
            new GradientPoint(0f, 0f),
            new GradientPoint(1f, 0f),
            new[]
            {
                new GradientStop(0f, Colors.Red),
                new GradientStop(1f, Colors.Blue),
            },
            tileMode: BrushTileMode.Repeat);

        var mirrorBrush = Brush.LinearGradient(
            new GradientPoint(0f, 0f),
            new GradientPoint(1f, 0f),
            new[]
            {
                new GradientStop(0f, Colors.Red),
                new GradientStop(1f, Colors.Blue),
            },
            tileMode: BrushTileMode.Mirror);

        var rect = new Rectangle(0, 0, 10, 1);

        var repeatA = repeatBrush.Sample(12, 0, rect, ColorMixSpace.Srgb);
        var repeatB = repeatBrush.Sample(2, 0, rect, ColorMixSpace.Srgb);
        Assert.AreEqual(repeatB.ToRgb(), repeatA.ToRgb());

        var mirrorA = mirrorBrush.Sample(12, 0, rect, ColorMixSpace.Srgb);
        var mirrorB = mirrorBrush.Sample(7, 0, rect, ColorMixSpace.Srgb);
        Assert.AreEqual(mirrorB.ToRgb(), mirrorA.ToRgb());
    }
}
