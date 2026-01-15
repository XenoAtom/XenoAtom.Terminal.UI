// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Text;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class TextEditorDocumentTests
{
    [TestMethod]
    public void TextDocument_Normalizes_LineEndings()
    {
        var doc = new TextDocument("A\r\nB\rC");
        var snapshot = doc.CurrentSnapshot;
        var buffer = new char[snapshot.Length];
        snapshot.CopyTo(0, buffer);

        var text = new string(buffer);
        Assert.AreEqual("A\nB\nC", text);
        Assert.AreEqual(3, snapshot.LineCount);
    }

    [TestMethod]
    public void TextEditorBase_Syncs_Text_And_Document()
    {
        var textBox = new TextBox { Text = "Hello\r\nWorld" };
        var snapshot = textBox.TextDocument.CurrentSnapshot;
        var buffer = new char[snapshot.Length];
        snapshot.CopyTo(0, buffer);

        var text = new string(buffer);
        Assert.AreEqual("Hello\nWorld", text);
        Assert.AreEqual("Hello\nWorld", textBox.Text);

        textBox.TextDocument.Insert(0, "A".AsSpan());
        Assert.AreEqual("AHello\nWorld", textBox.Text);
    }
}
