// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Hosting;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class TextEditorUndoRedoTests
{
    [TestMethod]
    public void TextBox_Supports_Undo_And_Redo_With_Typing_Coalescing()
    {
        var textBox = new TextBox();
        textBox.UndoManager.SetClockForTests(new ConstantClock());

        using var driver = new TerminalAppTestDriver(new VStack { textBox }, TerminalHostKind.Inline, new TerminalSize(40, 10));
        driver.Tick();

        driver.Backend.PushEvent(new TerminalTextEvent { Text = "a" });
        driver.Backend.PushEvent(new TerminalTextEvent { Text = "b" });
        driver.Backend.PushEvent(new TerminalTextEvent { Text = "c" });
        driver.TickUntil(() => textBox.Text == "abc");

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Unknown, Char = TerminalChar.CtrlZ, Modifiers = TerminalModifiers.Ctrl });
        driver.TickUntil(() => textBox.Text == string.Empty);

        Assert.IsFalse(textBox.CanUndo);
        Assert.IsTrue(textBox.CanRedo);

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Unknown, Char = TerminalChar.CtrlR, Modifiers = TerminalModifiers.Ctrl });
        driver.TickUntil(() => textBox.Text == "abc");
    }

    [TestMethod]
    public void TextBox_Undo_Restores_Replaced_Selection()
    {
        var textBox = new TextBox();
        textBox.UndoManager.SetClockForTests(new ConstantClock());

        using var driver = new TerminalAppTestDriver(new VStack { textBox }, TerminalHostKind.Inline, new TerminalSize(40, 10));
        driver.Tick();

        driver.Backend.PushEvent(new TerminalTextEvent { Text = "abcd" });
        driver.TickUntil(() => textBox.Text == "abcd");

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Home });
        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Right });
        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Right, Modifiers = TerminalModifiers.Shift });
        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Right, Modifiers = TerminalModifiers.Shift });
        driver.Tick();

        driver.Backend.PushEvent(new TerminalTextEvent { Text = "X" });
        driver.TickUntil(() => textBox.Text == "aXd");

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Unknown, Char = TerminalChar.CtrlZ, Modifiers = TerminalModifiers.Ctrl });
        driver.TickUntil(() => textBox.Text == "abcd");

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Unknown, Char = TerminalChar.CtrlR, Modifiers = TerminalModifiers.Ctrl });
        driver.TickUntil(() => textBox.Text == "aXd");
    }

    [TestMethod]
    public void TextArea_ReplaceAll_Is_Single_Undo_Entry()
    {
        var textArea = new TextArea("a a a");
        textArea.UndoManager.SetClockForTests(new ConstantClock());

        using var driver = new TerminalAppTestDriver(new VStack { textArea }, TerminalHostKind.Inline, new TerminalSize(60, 12));
        driver.Tick();

        var target = textArea.CreateSearchReplaceTarget();
        target.SetQuery(new SearchQuery("a", CaseSensitive: false, WholeWord: false, UseRegex: false));

        var replaced = target.ReplaceAll("xx");
        Assert.AreEqual(3, replaced);
        driver.TickUntil(() => textArea.Text == "xx xx xx");

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Unknown, Char = TerminalChar.CtrlZ, Modifiers = TerminalModifiers.Ctrl });
        driver.TickUntil(() => textArea.Text == "a a a");

        Assert.IsFalse(textArea.CanUndo);
        Assert.IsTrue(textArea.CanRedo);
    }

    [TestMethod]
    public void External_TextDocument_Changes_Clear_Undo_History()
    {
        var textBox = new TextBox();
        textBox.UndoManager.SetClockForTests(new ConstantClock());

        using var driver = new TerminalAppTestDriver(new VStack { textBox }, TerminalHostKind.Inline, new TerminalSize(40, 10));
        driver.Tick();

        driver.Backend.PushEvent(new TerminalTextEvent { Text = "abc" });
        driver.TickUntil(() => textBox.Text == "abc");
        Assert.IsTrue(textBox.CanUndo);

        // Modify the document directly (not through the editor core).
        textBox.TextDocument.Insert(0, "Z");
        driver.Tick();

        Assert.AreEqual("Zabc", textBox.Text);
        Assert.IsFalse(textBox.CanUndo);
        Assert.IsFalse(textBox.CanRedo);
    }

    private sealed class ConstantClock : TextUndoRedoManager.IUndoClock
    {
        public int NowMilliseconds => 0;
    }
}

