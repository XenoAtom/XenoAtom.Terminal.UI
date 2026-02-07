// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Text;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class TextEditorDocumentTests
{
    private static string ReadText(ITextSnapshot snapshot)
    {
        if (snapshot.Length == 0)
        {
            return string.Empty;
        }

        var buffer = new char[snapshot.Length];
        snapshot.CopyTo(0, buffer);
        return new string(buffer);
    }

    private static string ReadText(ITextDocument document)
    {
        return ReadText(document.CurrentSnapshot);
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

    [TestMethod]
    public void TextDocument_Snapshot_Remains_Stable_After_Mutations()
    {
        var doc = new TextDocument("abc");
        var snapshotBefore = doc.CurrentSnapshot;

        doc.Replace(1, 1, "X".AsSpan());
        doc.Insert(3, "Z".AsSpan());

        Assert.AreEqual("abc", ReadText(snapshotBefore));
        Assert.AreEqual("aXcZ", ReadText(doc));
    }

    [TestMethod]
    public void TextDocument_Handles_Insert_Remove_Replace_Composition()
    {
        var doc = new TextDocument("Hello World");

        doc.Remove(5, 1);
        doc.Insert(5, ", ".AsSpan());
        doc.Replace(7, 5, "Terminal".AsSpan());

        Assert.AreEqual("Hello, Terminal", ReadText(doc));
        Assert.AreEqual(1, doc.CurrentSnapshot.LineCount);
    }

    [TestMethod]
    public void DynamicTextDocument_Uses_Internal_Edits_When_Setter_Echoes_Value()
    {
        var backing = "abc";
        var document = new DynamicTextDocument(
            getter: () => backing,
            setter: value => backing = value);

        var snapshotBefore = document.CurrentSnapshot;
        document.Replace(1, 1, "X".AsSpan());

        Assert.AreEqual("aXc", backing);
        Assert.AreEqual("abc", ReadText(snapshotBefore));
        Assert.AreEqual("aXc", ReadText(document));
    }

    [TestMethod]
    public void DynamicTextDocument_Reconciles_When_Setter_Transforms_Value()
    {
        var backing = "abc";
        var document = new DynamicTextDocument(
            getter: () => backing,
            setter: value => backing = value.ToUpperInvariant());

        document.Insert(1, "z".AsSpan());

        Assert.AreEqual("AZBC", backing);
        Assert.AreEqual("AZBC", ReadText(document));
    }

    [TestMethod]
    public void TextDocument_Tracks_Crlf_Across_Separate_Inserts()
    {
        var doc = new TextDocument("A\rB");
        doc.Insert(2, "\n".AsSpan());

        Assert.AreEqual("A\r\nB", ReadText(doc));
        Assert.AreEqual(2, doc.CurrentSnapshot.LineCount);
    }
}
