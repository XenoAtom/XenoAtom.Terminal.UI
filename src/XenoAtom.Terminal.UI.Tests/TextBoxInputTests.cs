// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Hosting;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class TextBoxInputTests
{
    [TestMethod]
    public void TextBox_Edits_Text_And_Uses_Clipboard_Paste()
    {
        var textBox = new TextBox();
        var root = new VStack { textBox };

        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Inline, new TerminalSize(40, 10));
        driver.Terminal.Clipboard.Text = "xyz";
        driver.Tick();

        driver.Backend.PushEvent(new TerminalTextEvent { Text = "a" });
        driver.Backend.PushEvent(new TerminalTextEvent { Text = "b" });
        driver.Backend.PushEvent(new TerminalTextEvent { Text = "c" });
        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Left });
        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Backspace });
        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Unknown, Char = TerminalChar.CtrlV, Modifiers = TerminalModifiers.Ctrl });

        driver.TickUntil(() => textBox.Text == "axyzc");
    }

    [TestMethod]
    public void TextBox_Can_Select_And_Copy()
    {
        var textBox = new TextBox();
        var root = new VStack { textBox };

        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Inline, new TerminalSize(40, 10));
        driver.Tick();

        driver.Backend.PushEvent(new TerminalTextEvent { Text = "a" });
        driver.Backend.PushEvent(new TerminalTextEvent { Text = "b" });
        driver.Backend.PushEvent(new TerminalTextEvent { Text = "c" });

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Left, Modifiers = TerminalModifiers.Shift });
        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Unknown, Char = TerminalChar.CtrlC, Modifiers = TerminalModifiers.Ctrl });
        driver.Tick();

        Assert.AreEqual("c", driver.Terminal.Clipboard.Text);
    }

    [TestMethod]
    public void TextBox_Handles_TerminalPasteEvent()
    {
        var textBox = new TextBox();
        var root = new VStack { textBox };

        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Inline, new TerminalSize(40, 10));
        driver.Tick();

        driver.Backend.PushEvent(new TerminalPasteEvent { Text = "hello" });
        driver.TickUntil(() => textBox.Text == "hello");
    }

    [TestMethod]
    public void TextBox_Keeps_Placeholder_Visible_While_Focused_Until_Text_Is_Entered()
    {
        var textBox = new TextBox().Placeholder("Search");
        var root = new VStack { textBox };

        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(20, 4));
        driver.Tick();

        Assert.AreSame(textBox, driver.App.FocusedElement, "Expected the text box to take initial focus.");

        var initialScreen = new AnsiTestScreen(20, 4);
        initialScreen.Apply(driver.Backend.GetOutText());
        var initialRendered = initialScreen.GetText();

        StringAssert.Contains(initialRendered, "Search", "Expected the placeholder to remain visible while the empty text box is focused.");

        driver.Backend.PushEvent(new TerminalTextEvent { Text = "a" });
        driver.TickUntil(() => textBox.Text == "a");

        var typedScreen = new AnsiTestScreen(20, 4);
        typedScreen.Apply(driver.Backend.GetOutText());
        var typedRendered = typedScreen.GetText();

        StringAssert.Contains(typedRendered, "a", "Expected typed text to render.");
        Assert.IsFalse(typedRendered.Contains("Search", StringComparison.Ordinal), "Expected the placeholder to disappear after text is entered.");
    }

    [TestMethod]
    public void TextBox_Supports_Ctrl_Kill_And_Yank()
    {
        var textBox = new TextBox("hello world") { CaretIndex = 6 };
        var root = new VStack { textBox };

        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(40, 10));
        driver.Tick();

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Unknown, Char = TerminalChar.CtrlK, Modifiers = TerminalModifiers.Ctrl });
        driver.TickUntil(() => textBox.Text == "hello ");

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Unknown, Char = TerminalChar.CtrlY, Modifiers = TerminalModifiers.Ctrl });
        driver.TickUntil(() => textBox.Text == "hello world");
    }
}

