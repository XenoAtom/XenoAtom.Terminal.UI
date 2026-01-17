// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Linq;
using XenoAtom.Terminal;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Hosting;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class TextAreaTests
{
    [TestMethod]
    public void TextArea_Edits_Multiple_Lines()
    {
        var textArea = new TextArea();
        var root = new VStack { textArea };

        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(40, 10));
        driver.Tick();

        driver.Backend.PushEvent(new TerminalTextEvent { Text = "Hello" });
        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Enter });
        driver.Backend.PushEvent(new TerminalTextEvent { Text = "World" });

        driver.TickUntil(() => textArea.Text == "Hello\nWorld");

        var screen = new AnsiTestScreen(40, 10);
        screen.Apply(driver.Backend.GetOutText());
        var rendered = screen.GetText();

        StringAssert.Contains(rendered, "Hello");
        StringAssert.Contains(rendered, "World");
    }

    [TestMethod]
    public void TextArea_Wraps_Text_By_Default()
    {
        var textArea = new TextArea("0123456789");
        var root = new VStack { textArea };

        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(10, 6));
        driver.Tick();

        var screen = new AnsiTestScreen(10, 6);
        screen.Apply(driver.Backend.GetOutText());
        var rendered = screen.GetText().Split('\n');

        Assert.IsGreaterThanOrEqualTo(2, rendered.Length, "Expected multiple lines of output.");
        Assert.IsTrue(rendered[0].Contains("01234567", StringComparison.Ordinal), "Expected first wrapped line.");
        Assert.IsTrue(rendered[1].Contains("89", StringComparison.Ordinal), "Expected second wrapped line.");
    }

    [TestMethod]
    public void TextArea_CtrlHomeEnd_Moves_Caret_To_Document_Edges()
    {
        var textArea = new TextArea("Line 1\nLine 2\nLine 3");
        var root = new VStack { textArea };

        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(40, 10));
        driver.Tick();

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.End, Modifiers = TerminalModifiers.Ctrl });
        driver.Backend.PushEvent(new TerminalTextEvent { Text = "Z" });
        driver.TickUntil(() => (textArea.Text ?? string.Empty).EndsWith("Z", StringComparison.Ordinal));

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Home, Modifiers = TerminalModifiers.Ctrl });
        driver.Backend.PushEvent(new TerminalTextEvent { Text = "A" });
        driver.TickUntil(() => (textArea.Text ?? string.Empty).StartsWith("A", StringComparison.Ordinal));
    }

    [TestMethod]
    public void TextArea_ScrollOffset_Does_Not_Reset_During_Layout()
    {
        var text = string.Join("\n", Enumerable.Range(0, 40).Select(i => $"Line {i:00}"));
        var textArea = new TextArea(text);
        var root = new VStack { textArea };

        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(40, 8));
        driver.Tick();

        textArea.Scroll.ScrollBy(0, 3);
        driver.Tick();

        Assert.AreEqual(3, textArea.Scroll.OffsetY);
    }
}
