// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Hosting;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class TextEditorAutoScrollTests
{
    [TestMethod]
    public void TextArea_AutoScrolls_View_When_Typing_Past_Viewport()
    {
        var textArea = new TextArea();
        var root = new VStack { textArea };

        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(30, 8));
        driver.Tick();

        driver.App.Focus(textArea);

        for (var i = 0; i < 24; i++)
        {
            driver.Backend.PushEvent(new TerminalTextEvent { Text = $"L{i:00}" });
            driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Enter });
        }

        driver.Backend.PushEvent(new TerminalTextEvent { Text = "LAST" });

        driver.TickUntil(() => (textArea.Text ?? string.Empty).EndsWith("LAST", StringComparison.Ordinal));
        driver.TickUntil(() => textArea.Scroll.OffsetY > 0);

        var screen = new AnsiTestScreen(30, 8);
        screen.Apply(driver.Backend.GetOutText());
        var rendered = screen.GetText();
        StringAssert.Contains(rendered, "LAST", "Expected the final line to be visible after auto-scroll.");

        var caretVisible = textArea.TryGetCursorCell(out _, out _);
        Assert.IsTrue(caretVisible, "Expected the caret to remain visible after auto-scrolling.");
    }

    [TestMethod]
    public void TextBox_AutoScrolls_View_When_Typing_Past_Viewport()
    {
        var textBox = new TextBox();
        var root = new VStack { textBox };

        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(30, 6));
        driver.Tick();

        driver.App.Focus(textBox);

        driver.Backend.PushEvent(new TerminalTextEvent { Text = new string('a', 64) + "TAIL" });
        driver.TickUntil(() => (textBox.Text ?? string.Empty).EndsWith("TAIL", StringComparison.Ordinal));
        driver.TickUntil(() => textBox.Scroll.OffsetX > 0);

        var screen = new AnsiTestScreen(30, 6);
        screen.Apply(driver.Backend.GetOutText());
        var rendered = screen.GetText();
        StringAssert.Contains(rendered, "TAIL", "Expected the end of the text to be visible after horizontal auto-scroll.");

        var caretVisible = textBox.TryGetCursorCell(out _, out _);
        Assert.IsTrue(caretVisible, "Expected the caret to remain visible after horizontal auto-scroll.");
    }
}

