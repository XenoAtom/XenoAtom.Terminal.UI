// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Rendering;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class TextElementWidthTests
{
    [TestMethod]
    public void CellBuffer_Uses_TwoCells_For_EmojiPresentation_TextElement()
    {
        // U+2328 KEYBOARD + U+FE0F VARIATION SELECTOR-16 => emoji presentation.
        const string text = "⌨️";

        var buffer = new CellBuffer(10, 1);
        buffer.WriteText(0, 0, text, Style.None);

        var scalar = buffer.UnsafeScalars[0];
        Assert.IsLessThan(0, scalar, "Expected a text element token to be stored for a multi-codepoint grapheme.");

        Assert.IsTrue(buffer.TryGetTextElement(scalar, out var stored, out var width), "Expected the text element to be registered in the buffer.");
        Assert.AreEqual(text, stored);
        Assert.AreEqual(2, width, "Expected emoji presentation grapheme to occupy 2 terminal cells.");
    }

    [TestMethod]
    public void CellBuffer_Uses_TwoCells_For_Keycap_TextElement()
    {
        // U+0031 DIGIT ONE + U+FE0F VS16 + U+20E3 COMBINING ENCLOSING KEYCAP.
        const string text = "1️⃣";

        var buffer = new CellBuffer(10, 1);
        buffer.WriteText(0, 0, text, Style.None);

        var scalar = buffer.UnsafeScalars[0];
        Assert.IsLessThan(0, scalar, "Expected a text element token to be stored for a multi-codepoint grapheme.");

        Assert.IsTrue(buffer.TryGetTextElement(scalar, out var stored, out var width), "Expected the text element to be registered in the buffer.");
        Assert.AreEqual(text, stored);
        Assert.AreEqual(2, width, "Expected keycap grapheme to occupy 2 terminal cells.");
    }
}
