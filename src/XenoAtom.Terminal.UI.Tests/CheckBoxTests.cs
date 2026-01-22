// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Reflection;
using XenoAtom.Terminal;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Hosting;
using XenoAtom.Terminal.UI.Rendering;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class CheckBoxTests
{
    [TestMethod]
    public void CheckBox_Toggles_On_Space()
    {
        var checkBox = new CheckBox("A", isChecked: false);
        var root = new VStack { checkBox };

        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(20, 6));
        driver.Tick();

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Space });
        driver.TickUntil(() => checkBox.IsChecked);
    }

    [TestMethod]
    public void CheckBox_Renders_Space_Between_Glyph_And_Text()
    {
        var checkBox = new CheckBox("A", isChecked: true);

        // Use a wide glyph to ensure the label offset accounts for rune width.
        var wideGlyph = new Rune(0x1F600); // 😀
        checkBox.SetStyle(CheckBoxStyle.Key, new CheckBoxStyle
        {
            CheckedGlyph = wideGlyph,
        });

        checkBox.Measure(new Size(10, 1));
        checkBox.Arrange(new Rectangle(0, 0, 10, 1));

        var buffer = new CellBuffer(10, 1);
        buffer.Clear();
        typeof(Visual).GetMethod("RenderTree", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(checkBox, new object[] { buffer });

        var scalars = (int[])typeof(CellBuffer).GetField("_scalars", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(buffer)!;
        var cells = (Style[])typeof(CellBuffer).GetField("_cells", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(buffer)!;

        Assert.AreEqual(wideGlyph.Value, scalars[0]);

        var glyphWidth = Math.Max(1, TerminalTextUtility.GetRuneWidth(wideGlyph));
        if (glyphWidth > 1)
        {
            Assert.IsTrue((bool)typeof(Style).GetProperty("IsContinuation", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(cells[1])!);
        }

        var labelIndex = Array.IndexOf(scalars, 'A');
        Assert.IsGreaterThanOrEqualTo(0, labelIndex, "Expected the label text to be rendered.");
        Assert.IsGreaterThanOrEqualTo(glyphWidth, labelIndex, "Expected a gap between the glyph and the label text.");
    }
}
