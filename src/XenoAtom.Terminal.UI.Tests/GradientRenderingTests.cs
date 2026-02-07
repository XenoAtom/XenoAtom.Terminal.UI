// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Figlet;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class GradientRenderingTests
{
    [TestMethod]
    public void TextBlock_Applies_Foreground_Brush()
    {
        var brush = Brush.LinearGradient(
            new GradientPoint(0f, 0f),
            new GradientPoint(1f, 0f),
            new GradientStop(0f, Colors.Red),
            new GradientStop(1f, Colors.Blue));

        var textBlock = new TextBlock("AB")
            .Style(Theme.Default)
            .HorizontalAlignment(Align.Stretch)
            .Style(TextBlockStyle.Default with { ForegroundBrush = brush });

        textBlock.Measure(new LayoutConstraints(0, 2, 0, 1));
        textBlock.Arrange(new Rectangle(0, 0, 2, 1));

        var buffer = new CellBuffer(2, 1);
        buffer.Clear(textBlock.GetTheme().BaseTextStyle());
        textBlock.RenderTree(buffer);

        var c0 = buffer.UnsafeCells[0];
        var c1 = buffer.UnsafeCells[1];
        Assert.IsTrue(c0.TryGetForeground(out var fg0));
        Assert.IsTrue(c1.TryGetForeground(out var fg1));
        Assert.AreNotEqual(fg0.ToRgb(), fg1.ToRgb());
    }

    [TestMethod]
    public void TextBlock_Uses_PerLine_Restart_For_Brushes()
    {
        var brush = Brush.LinearGradient(
            new GradientPoint(0f, 0f),
            new GradientPoint(1f, 1f),
            new GradientStop(0f, Colors.Red),
            new GradientStop(1f, Colors.Blue));

        var textBlock = new TextBlock("ABCD")
            .Style(Theme.Default)
            .HorizontalAlignment(Align.Stretch)
            .Wrap(true)
            .Style(TextBlockStyle.Default with { ForegroundBrush = brush });

        textBlock.Measure(new LayoutConstraints(0, 2, 0, 2));
        textBlock.Arrange(new Rectangle(0, 0, 2, 2));

        var buffer = new CellBuffer(2, 2);
        buffer.Clear(textBlock.GetTheme().BaseTextStyle());
        textBlock.RenderTree(buffer);

        var line0 = buffer.UnsafeCells[0];
        var line1 = buffer.UnsafeCells[2];
        Assert.IsTrue(line0.TryGetForeground(out var fg0));
        Assert.IsTrue(line1.TryGetForeground(out var fg1));
        Assert.AreEqual(fg0.ToRgb(), fg1.ToRgb());
    }

    [TestMethod]
    public void TextFiglet_Applies_Diagonal_Foreground_Brush()
    {
        var brush = Brush.LinearGradient(
            new GradientPoint(0f, 0f),
            new GradientPoint(1f, 1f),
            new GradientStop(0f, Colors.Red),
            new GradientStop(1f, Colors.Blue));

        var figlet = new TextFiglet("A")
            .Font(FigletFont.CreateBlockFont(height: 3, width: 3))
            .Style(Theme.Default)
            .Style(TextFigletStyle.Default with { ForegroundBrush = brush });

        figlet.Measure(new LayoutConstraints(0, 3, 0, 3));
        figlet.Arrange(new Rectangle(0, 0, 3, 3));

        var buffer = new CellBuffer(3, 3);
        buffer.Clear(figlet.GetTheme().BaseTextStyle());
        figlet.RenderTree(buffer);

        var topLeft = buffer.UnsafeCells[0];
        var bottomRight = buffer.UnsafeCells[(2 * 3) + 2];
        Assert.IsTrue(topLeft.TryGetForeground(out var fgTopLeft));
        Assert.IsTrue(bottomRight.TryGetForeground(out var fgBottomRight));
        Assert.AreNotEqual(fgTopLeft.ToRgb(), fgBottomRight.ToRgb());
    }

    [TestMethod]
    public void TextBox_Background_Brush_Fills_Input_Area()
    {
        var backgroundBrush = Brush.LinearGradient(
            new GradientPoint(0f, 0f),
            new GradientPoint(1f, 0f),
            new GradientStop(0f, Colors.Red),
            new GradientStop(1f, Colors.Blue));

        var textBox = new TextBox("AB")
            .Style(Theme.Default)
            .Style(TextBoxStyle.Default with { BackgroundBrush = backgroundBrush });

        textBox.Measure(new Size(8, 1));
        textBox.Arrange(new Rectangle(0, 0, 8, 1));

        var buffer = new CellBuffer(8, 1);
        buffer.Clear(textBox.GetTheme().BaseTextStyle());
        textBox.RenderTree(buffer);

        var left = buffer.UnsafeCells[1];
        var right = buffer.UnsafeCells[6];
        Assert.IsTrue(left.TryGetBackground(out var bgLeft));
        Assert.IsTrue(right.TryGetBackground(out var bgRight));
        Assert.AreNotEqual(bgLeft.ToRgb(), bgRight.ToRgb());
    }

    [TestMethod]
    public void TextBox_Foreground_Brush_Colors_Text()
    {
        var foregroundBrush = Brush.LinearGradient(
            new GradientPoint(0f, 0f),
            new GradientPoint(1f, 0f),
            new GradientStop(0f, Colors.Red),
            new GradientStop(1f, Colors.Blue));

        var textBox = new TextBox("AB")
            .Style(Theme.Default)
            .Style(TextBoxStyle.Default with { ForegroundBrush = foregroundBrush });

        textBox.Measure(new Size(8, 1));
        textBox.Arrange(new Rectangle(0, 0, 8, 1));

        var buffer = new CellBuffer(8, 1);
        buffer.Clear(textBox.GetTheme().BaseTextStyle());
        textBox.RenderTree(buffer);

        var firstGlyph = buffer.UnsafeCells[1];
        var secondGlyph = buffer.UnsafeCells[2];
        Assert.IsTrue(firstGlyph.TryGetForeground(out var fgFirst));
        Assert.IsTrue(secondGlyph.TryGetForeground(out var fgSecond));
        Assert.AreNotEqual(fgFirst.ToRgb(), fgSecond.ToRgb());
    }
}
