// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Hosting;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class TextAreaSearchReplaceTests
{
    [TestMethod]
    public void TextArea_CtrlF_Opens_Find_Popup()
    {
        var editor = new TextArea("foo bar foo");
        using var driver = new TerminalAppTestDriver(editor, TerminalHostKind.Fullscreen, new TerminalSize(60, 10));
        driver.Tick();

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Unknown, Char = TerminalChar.CtrlF, Modifiers = TerminalModifiers.Ctrl });
        driver.Tick();

        var screen = new AnsiTestScreen(60, 10);
        screen.Apply(driver.Backend.GetOutText());
        var rendered = screen.GetText();

        StringAssert.Contains(rendered, "Find");
        StringAssert.Contains(rendered, "Case");
    }

    [TestMethod]
    public void TextArea_CtrlH_Opens_FindReplace_Popup()
    {
        var editor = new TextArea("foo bar foo");
        using var driver = new TerminalAppTestDriver(editor, TerminalHostKind.Fullscreen, new TerminalSize(60, 10));
        driver.Tick();

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Unknown, Char = TerminalChar.CtrlH, Modifiers = TerminalModifiers.Ctrl });
        driver.Tick();

        var screen = new AnsiTestScreen(60, 10);
        screen.Apply(driver.Backend.GetOutText());
        var rendered = screen.GetText();

        StringAssert.Contains(rendered, "Find / Replace");
        StringAssert.Contains(rendered, "Replace");
    }

    [TestMethod]
    public void TextArea_Find_Next_Selects_Next_Match()
    {
        var editor = new TextArea("foo bar foo");
        using var driver = new TerminalAppTestDriver(editor, TerminalHostKind.Fullscreen, new TerminalSize(60, 10));
        driver.Tick();

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Unknown, Char = TerminalChar.CtrlF, Modifiers = TerminalModifiers.Ctrl });
        driver.Tick();

        driver.Backend.PushEvent(new TerminalTextEvent { Text = "foo" });
        driver.Tick();

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Enter });
        driver.Tick();

        Assert.AreEqual(11, editor.CaretIndex);
    }

    [TestMethod]
    public void TextArea_ReplaceAll_Updates_Document_Text()
    {
        var editor = new TextArea("foo bar foo");
        using var driver = new TerminalAppTestDriver(editor, TerminalHostKind.Fullscreen, new TerminalSize(60, 10));
        driver.Tick();

        var target = editor.CreateSearchReplaceTarget();
        target.SetQuery(new SearchQuery("foo", CaseSensitive: false, WholeWord: false, UseRegex: false));
        target.ReplaceAll("baz");
        driver.Tick();

        Assert.AreEqual("baz bar baz", editor.Text);
    }
}
