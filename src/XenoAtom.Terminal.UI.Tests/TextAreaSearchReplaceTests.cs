// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Hosting;
using System.Linq;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class TextAreaSearchReplaceTests
{
    private static bool IsDescendantOf(Visual? visual, Visual ancestor)
    {
        while (visual is not null)
        {
            if (ReferenceEquals(visual, ancestor))
            {
                return true;
            }
            visual = visual.Parent;
        }

        return false;
    }

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
    public void TextArea_Find_Popup_Tab_Stays_Inside_Popup()
    {
        var editor = new TextArea("foo bar foo");
        using var driver = new TerminalAppTestDriver(editor, TerminalHostKind.Fullscreen, new TerminalSize(60, 10));
        driver.Tick();

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Unknown, Char = TerminalChar.CtrlF, Modifiers = TerminalModifiers.Ctrl });
        driver.Tick();

        var popup = driver.App.Root.EnumerateVisualsDepthFirst().OfType<Popup>().FirstOrDefault();
        Assert.IsNotNull(popup);
        Assert.IsFalse(popup.CloseOnTab);

        var focused = driver.App.FocusedElement;
        Assert.IsNotNull(focused);
        Assert.IsTrue(IsDescendantOf(focused, popup));

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Tab });
        driver.Tick();

        var popupAfter = driver.App.Root.EnumerateVisualsDepthFirst().OfType<Popup>().FirstOrDefault();
        Assert.AreSame(popup, popupAfter);

        var focusedAfter = driver.App.FocusedElement;
        Assert.IsNotNull(focusedAfter);
        Assert.IsTrue(IsDescendantOf(focusedAfter, popup));
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
    public void TextArea_Closing_Find_Popup_Clears_Search_Query()
    {
        var editor = new TextArea("foo bar foo");
        using var driver = new TerminalAppTestDriver(editor, TerminalHostKind.Fullscreen, new TerminalSize(60, 10));
        driver.Tick();

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Unknown, Char = TerminalChar.CtrlF, Modifiers = TerminalModifiers.Ctrl });
        driver.Tick();

        driver.Backend.PushEvent(new TerminalTextEvent { Text = "foo" });
        driver.Tick();

        var target = editor.CreateSearchReplaceTarget();
        Assert.AreNotEqual("No search", target.GetStatusText(), "Expected an active search query once text is entered.");

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Escape });
        driver.Tick();

        Assert.AreEqual("No search", target.GetStatusText(), "Closing the find popup should clear match highlighting.");
    }

    [TestMethod]
    public void TextArea_SearchReplace_Popup_Restores_Focus_After_Mode_Toggle_And_Close()
    {
        var editor = new TextArea("foo bar foo");
        using var driver = new TerminalAppTestDriver(editor, TerminalHostKind.Fullscreen, new TerminalSize(60, 10));
        driver.Tick();

        Assert.AreSame(editor, driver.App.FocusedElement);

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Unknown, Char = TerminalChar.CtrlF, Modifiers = TerminalModifiers.Ctrl });
        driver.Tick();

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Unknown, Char = TerminalChar.CtrlH, Modifiers = TerminalModifiers.Ctrl });
        driver.Tick();

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Escape });
        driver.Tick();

        Assert.AreSame(editor, driver.App.FocusedElement);
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
