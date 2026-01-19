// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Reflection;
using XenoAtom.Terminal.UI.Rendering;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class EmojiWidthTests
{
    [TestMethod]
    public void CellBuffer_WriteText_Treats_Common_Emoji_As_Wide()
    {
        // 🗃️ (U+1F5C3 + U+FE0F) is commonly rendered as a wide glyph (2 terminal cells).
        // If it is treated as narrow, the following space will visually be "eaten".
        var text = "🗃️ Download";

        var buffer = new CellBuffer(20, 1);
        buffer.Clear();
        buffer.WriteText(0, 0, text.AsSpan(), Style.None);

        var scalars = (int[])typeof(CellBuffer).GetField("_scalars", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(buffer)!;
        var cells = (Style[])typeof(CellBuffer).GetField("_cells", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(buffer)!;

        Assert.IsLessThan(0, scalars[0], "Expected a grapheme cluster token for the emoji text element.");
        Assert.IsTrue(buffer.TryGetTextElement(scalars[0], out var element, out var elementWidth));
        Assert.AreEqual("🗃️", element);
        Assert.AreEqual(2, elementWidth);

        // Wide glyphs occupy a leading cell + a continuation cell.
        Assert.IsTrue((bool)typeof(Style).GetProperty("IsContinuation", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(cells[1])!);

        // The first typed space must appear after the wide glyph (cell 2).
        Assert.AreEqual(' ', scalars[2]);
        Assert.AreEqual('D', scalars[3]);
    }

    [TestMethod]
    public void TerminalTextUtility_GetWidth_Uses_Grapheme_Clusters()
    {
        // ZWJ sequence: runner + ZWJ + female sign + VS16.
        // Terminals typically render this as a single wide glyph (2 cells).
        Assert.AreEqual(2, TerminalTextUtility.GetWidth("🏃‍♀️".AsSpan()));

        // Flags are also grapheme clusters (regional indicator pairs).
        Assert.AreEqual(2, TerminalTextUtility.GetWidth("🇫🇷".AsSpan()));
    }
}
