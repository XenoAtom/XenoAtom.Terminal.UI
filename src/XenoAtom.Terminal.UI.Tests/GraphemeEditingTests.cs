// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Hosting;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class GraphemeEditingTests
{
    [TestMethod]
    public void TextBox_LeftRight_Backspace_Delete_Treats_Grapheme_As_Unit()
    {
        var textBox = new TextBox();
        var root = new VStack { textBox };

        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Inline, new TerminalSize(40, 6));
        driver.Tick();

        driver.Backend.PushEvent(new TerminalTextEvent { Text = "🗃️" });
        driver.TickUntil(() => textBox.Text == "🗃️");

        // Caret should be at the end of the grapheme cluster.
        Assert.AreEqual(textBox.Text!.Length, textBox.CaretIndex);

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Left });
        driver.TickUntil(() => textBox.CaretIndex == 0);

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Right });
        driver.TickUntil(() => textBox.CaretIndex == textBox.Text!.Length);

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Backspace });
        driver.TickUntil(() => string.IsNullOrEmpty(textBox.Text));

        driver.Backend.PushEvent(new TerminalTextEvent { Text = "🗃️" });
        driver.TickUntil(() => textBox.Text == "🗃️");

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Home });
        driver.TickUntil(() => textBox.CaretIndex == 0);

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Delete });
        driver.TickUntil(() => string.IsNullOrEmpty(textBox.Text));
    }

    [TestMethod]
    public void TextArea_Backspace_Removes_Entire_Zwj_Grapheme()
    {
        // 🏃‍♀️ is a ZWJ sequence. Backspace should remove the entire grapheme.
        var textArea = new TextArea("A\n🏃‍♀️\nB");
        var root = new VStack { textArea };

        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(40, 10));
        driver.Tick();

        var text = textArea.Text!;
        var caretIndex = text.IndexOf("🏃‍♀️", StringComparison.Ordinal) + "🏃‍♀️".Length;
        textArea.CaretIndex = caretIndex;
        driver.Tick();

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Backspace });
        driver.TickUntil(() => textArea.Text == "A\n\nB");
    }

    [TestMethod]
    public void TextArea_LeftRight_Treats_Crlf_As_Single_Text_Element()
    {
        var textArea = new TextArea("A\r\nB");
        var root = new VStack { textArea };

        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(40, 10));
        driver.App.Focus(textArea);
        driver.Tick();

        textArea.CaretIndex = 1;
        driver.Tick();

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Right });
        driver.TickUntil(() => textArea.CaretIndex == 3);

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Left });
        driver.TickUntil(() => textArea.CaretIndex == 1);
    }
}

