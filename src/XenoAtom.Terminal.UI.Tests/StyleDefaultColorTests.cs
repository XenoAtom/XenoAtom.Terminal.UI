// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using XenoAtom.Terminal.UI.Rendering;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class StyleDefaultColorTests
{
    [TestMethod]
    public void Style_WithForeground_Default_Is_Explicit()
    {
        var style = Style.None.WithForeground(Color.Default);

        Assert.IsTrue(style.TryGetForeground(out var fg));
        Assert.AreEqual(ColorKind.Default, fg.Kind);
    }

    [TestMethod]
    public void CellBuffer_Can_Overlay_Default_Foreground_Over_Colored_Underlay()
    {
        var buffer = new CellBuffer(1, 1);
        buffer.Clear();

        buffer.SetCell(0, 0, new Rune('X'), Style.None.WithForeground(Color.Rgb(255, 0, 0)));

        buffer.SetCell(
            0,
            0,
            new Rune(' '),
            Style.None.WithForeground(Color.Default).WithBackground(Color.Rgb(0, 0, 0)));

        Assert.IsTrue(buffer.UnsafeCells[0].TryGetForeground(out var fg));
        Assert.AreEqual(ColorKind.Default, fg.Kind);
    }

    [TestMethod]
    public void Theme_ForegroundTextStyle_Uses_Terminal_Default_When_Foreground_Is_Null()
    {
        var theme = Theme.Terminal;
        var style = theme.ForegroundTextStyle();

        Assert.IsTrue(style.TryGetForeground(out var fg));
        Assert.AreEqual(ColorKind.Default, fg.Kind);
    }
}

