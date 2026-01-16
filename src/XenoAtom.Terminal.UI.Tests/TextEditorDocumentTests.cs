// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Text;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class TextEditorDocumentTests
{
    private static string ReadText(ITextDocument document)
    {
        var snapshot = document.CurrentSnapshot;
        if (snapshot.Length == 0)
        {
            return string.Empty;
        }

        var buffer = new char[snapshot.Length];
        snapshot.CopyTo(0, buffer);
        return new string(buffer);
    }

    [TestMethod]
    public void TextDocument_Preserves_LineEndings_And_Computes_Lines()
    {
        var doc = new TextDocument("A\r\nB\rC");
        var text = ReadText(doc);
        Assert.AreEqual("A\r\nB\rC", text);
        Assert.AreEqual(3, doc.CurrentSnapshot.LineCount);
    }

    [TestMethod]
    public void TextBox_Uses_Dynamic_TextDocument_By_Default()
    {
        var textBox = new TextBox("Hello\r\nWorld");
        var snapshot = textBox.TextDocument.CurrentSnapshot;
        Assert.AreEqual(2, snapshot.LineCount);
        Assert.AreEqual("Hello\r\nWorld", ReadText(textBox.TextDocument));
        Assert.AreEqual("Hello\r\nWorld", textBox.Text);

        textBox.TextDocument.Insert(0, "A".AsSpan());
        Assert.AreEqual("AHello\r\nWorld", textBox.Text);
    }

    [TestMethod]
    public void TextBox_TextDocument_Updates_Bound_State()
    {
        var state = new State<string?>("Hello");
        var textBox = new TextBox().Text(state);

        Assert.AreEqual("Hello", textBox.Text);

        textBox.TextDocument.Insert(0, "A".AsSpan());
        Assert.AreEqual("AHello", state.Value);

        state.Value = "World";
        Assert.AreEqual("World", textBox.Text);
        Assert.AreEqual("World", ReadText(textBox.TextDocument));
    }

    [TestMethod]
    public void TextArea_TextDocument_Updates_Bound_State()
    {
        var state = new State<string?>("Line1\r\nLine2");
        var textArea = new TextArea().Text(state);

        Assert.AreEqual("Line1\r\nLine2", textArea.Text);
        Assert.AreEqual("Line1\r\nLine2", ReadText(textArea.TextDocument));

        textArea.TextDocument.Insert(0, "A".AsSpan());
        Assert.AreEqual("ALine1\r\nLine2", state.Value);
    }
}
