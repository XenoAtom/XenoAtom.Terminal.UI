// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Layout;
using XenoAtom.Terminal.UI.Rendering;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class PlaceholderTests
{
    [TestMethod]
    public void Placeholder_WithoutText_Fills_Background()
    {
        var placeholder = new Placeholder()
            .Style(Theme.Default)
            .HorizontalAlignment(Align.Stretch)
            .Style(PlaceholderStyle.Default with { Background = Colors.SlateBlue });

        placeholder.Measure(new LayoutConstraints(0, 4, 0, 2));
        placeholder.Arrange(new Rectangle(0, 0, 4, 2));

        var buffer = new CellBuffer(4, 2);
        buffer.Clear(placeholder.GetTheme().BaseTextStyle());
        placeholder.RenderTree(buffer);

        for (var i = 0; i < buffer.UnsafeCells.Length; i++)
        {
            Assert.IsTrue(buffer.UnsafeCells[i].TryGetBackground(out var bg));
            Assert.AreEqual(Colors.SlateBlue, bg);
        }
    }

    [TestMethod]
    public void Placeholder_BackgroundBrush_Produces_Gradient()
    {
        var placeholder = new Placeholder()
            .Style(Theme.Default)
            .HorizontalAlignment(Align.Stretch)
            .Style(PlaceholderStyle.Default with
            {
                BackgroundBrush = Brush.LinearGradient(
                    new GradientPoint(0f, 0f),
                    new GradientPoint(1f, 0f),
                    new GradientStop(0f, Colors.Red),
                    new GradientStop(1f, Colors.Blue)),
            });

        placeholder.Measure(new LayoutConstraints(0, 4, 0, 1));
        placeholder.Arrange(new Rectangle(0, 0, 4, 1));

        var buffer = new CellBuffer(4, 1);
        buffer.Clear(placeholder.GetTheme().BaseTextStyle());
        placeholder.RenderTree(buffer);

        Assert.IsTrue(buffer.UnsafeCells[0].TryGetBackground(out var left));
        Assert.IsTrue(buffer.UnsafeCells[3].TryGetBackground(out var right));
        Assert.AreNotEqual(left.ToRgb(), right.ToRgb());
    }

    [TestMethod]
    public void Placeholder_ForegroundBrush_Produces_Gradient_On_Text()
    {
        var placeholder = new Placeholder("AB")
            .Style(Theme.Default)
            .HorizontalAlignment(Align.Stretch)
            .Style(PlaceholderStyle.Default with
            {
                ForegroundBrush = Brush.LinearGradient(
                    new GradientPoint(0f, 0f),
                    new GradientPoint(1f, 0f),
                    new GradientStop(0f, Colors.Red),
                    new GradientStop(1f, Colors.Blue)),
            });

        placeholder.Measure(new LayoutConstraints(0, 2, 0, 1));
        placeholder.Arrange(new Rectangle(0, 0, 2, 1));

        var buffer = new CellBuffer(2, 1);
        buffer.Clear(placeholder.GetTheme().BaseTextStyle());
        placeholder.RenderTree(buffer);

        Assert.IsTrue(buffer.UnsafeCells[0].TryGetForeground(out var first));
        Assert.IsTrue(buffer.UnsafeCells[1].TryGetForeground(out var second));
        Assert.AreNotEqual(first.ToRgb(), second.ToRgb());
    }

    [TestMethod]
    public void Placeholder_Centers_Text_Vertically_By_Default()
    {
        var placeholder = new Placeholder("X")
            .Style(Theme.Default)
            .HorizontalAlignment(Align.Stretch)
            .VerticalAlignment(Align.Stretch);

        placeholder.Measure(new LayoutConstraints(0, 5, 0, 3));
        placeholder.Arrange(new Rectangle(0, 0, 5, 3));

        var buffer = new CellBuffer(5, 3);
        buffer.Clear();
        placeholder.RenderTree(buffer);

        var index = (1 * 5) + 2;
        Assert.AreEqual('X', buffer.UnsafeScalars[index]);
    }

    [TestMethod]
    public void Placeholder_Measure_WithoutText_Returns_AtLeast_One_Cell()
    {
        var placeholder = new Placeholder();
        placeholder.Measure(new LayoutConstraints(0, LayoutConstants.Infinite, 0, LayoutConstants.Infinite));
        Assert.IsGreaterThanOrEqualTo(1, placeholder.DesiredSize.Width);
        Assert.IsGreaterThanOrEqualTo(1, placeholder.DesiredSize.Height);
    }

    [TestMethod]
    public void Placeholder_VerticalTextAlignment_End_Renders_On_Last_Line()
    {
        var placeholder = new Placeholder("X")
            .Style(Theme.Default)
            .HorizontalAlignment(Align.Stretch)
            .VerticalAlignment(Align.Stretch)
            .VerticalTextAlignment(Align.End);

        placeholder.Measure(new LayoutConstraints(0, 5, 0, 3));
        placeholder.Arrange(new Rectangle(0, 0, 5, 3));

        var buffer = new CellBuffer(5, 3);
        buffer.Clear();
        placeholder.RenderTree(buffer);

        var index = (2 * 5) + 2;
        Assert.AreEqual('X', buffer.UnsafeScalars[index]);
    }

    [TestMethod]
    public void Placeholder_FillBackgroundFalse_DoesNotFill_Outside_Text()
    {
        var placeholder = new Placeholder("A")
            .Style(Theme.Default)
            .HorizontalAlignment(Align.Stretch)
            .Style(PlaceholderStyle.Default with
            {
                Background = Colors.Blue,
                FillBackground = false,
            });

        placeholder.Measure(new LayoutConstraints(0, 3, 0, 1));
        placeholder.Arrange(new Rectangle(0, 0, 3, 1));

        var buffer = new CellBuffer(3, 1);
        buffer.Clear(Style.None.WithBackground(Colors.Black));
        placeholder.RenderTree(buffer);

        Assert.IsTrue(buffer.UnsafeCells[0].TryGetBackground(out var left));
        Assert.IsTrue(buffer.UnsafeCells[1].TryGetBackground(out var center));
        Assert.IsTrue(buffer.UnsafeCells[2].TryGetBackground(out var right));
        Assert.AreEqual(Colors.Black, left);
        Assert.AreEqual(Colors.Blue, center);
        Assert.AreEqual(Colors.Black, right);
    }
}
