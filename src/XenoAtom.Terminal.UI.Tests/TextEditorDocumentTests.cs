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
    public void TextDocument_Normalizes_LineEndings()
    {
        var doc = new TextDocument("A\r\nB\rC");
        var text = ReadText(doc);
        Assert.AreEqual("A\nB\nC", text);
        Assert.AreEqual(3, doc.CurrentSnapshot.LineCount);
    }

    [TestMethod]
    public void TextEditorBase_Syncs_Text_And_Document()
    {
        var textBox = new TextBox { Text = "Hello\r\nWorld" };
        var text = ReadText(textBox.TextDocument);
        Assert.AreEqual("Hello\nWorld", text);
        Assert.AreEqual("Hello\nWorld", textBox.Text);

        textBox.TextDocument.Insert(0, "A".AsSpan());
        Assert.AreEqual("AHello\nWorld", textBox.Text);
    }

    [TestMethod]
    public void TextEditorBase_Uses_Provided_Document()
    {
        var textBox = new TextBox { Text = "Initial" };
        var document = new TextDocument("External");

        textBox.TextDocument = document;

        Assert.AreEqual("External", textBox.Text);
        Assert.AreEqual("External", ReadText(document));

        textBox.Text = "Updated";
        Assert.AreEqual("Updated", ReadText(document));
    }
}
