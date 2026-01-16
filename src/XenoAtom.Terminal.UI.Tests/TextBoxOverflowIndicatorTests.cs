// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Reflection;
using System.Text;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Rendering;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class TextBoxOverflowIndicatorTests
{
    [TestMethod]
    public void Renders_Right_Overflow_Indicator_When_Text_Overflows_And_Scrolled_To_Start()
    {
        var tb = new TextBox("ABCDEFGHIJK");

        tb.Measure(new Size(10, 1));
        tb.Arrange(new Rectangle(0, 0, 10, 1));

        var buffer = Render(tb, 10, 1);
        Assert.AreEqual('→', buffer[8]);
        Assert.AreEqual('A', buffer[1]);
    }

    [TestMethod]
    public void Renders_Left_Overflow_Indicator_When_Text_Overflows_And_Scrolled_Right()
    {
        var tb = new TextBox("ABCDEFGHIJK")
        {
            CaretIndex = 11
        };

        tb.Measure(new Size(10, 1));
        tb.Arrange(new Rectangle(0, 0, 10, 1));

        var buffer = Render(tb, 10, 1);
        Assert.AreEqual('←', buffer[1]);
        Assert.AreEqual('K', buffer[8]);
    }

    [TestMethod]
    public void Renders_Ellipsis_Indicators_When_Configured_In_Style()
    {
        var tb = new TextBox("ABCDEFGHIJK")
            .Style(TextBoxStyle.Ellipsis);

        tb.Measure(new Size(10, 1));
        tb.Arrange(new Rectangle(0, 0, 10, 1));

        var buffer = Render(tb, 10, 1);
        Assert.AreEqual('…', buffer[8]);
    }

    private static char[] Render(Visual visual, int width, int height)
    {
        var buffer = new CellBuffer(width, height);
        buffer.Clear();
        typeof(Visual).GetMethod("RenderTree", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(visual, new object[] { buffer });

        var scalars = (int[])typeof(CellBuffer).GetField("_scalars", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(buffer)!;
        var chars = new char[scalars.Length];
        for (var i = 0; i < scalars.Length; i++)
        {
            chars[i] = (char)scalars[i];
        }

        return chars;
    }
}
