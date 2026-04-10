// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Linq;
using XenoAtom.Terminal;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Geometry;
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
    public void TextArea_CtrlLeftRight_Moves_By_Word()
    {
        var textArea = new TextArea("Hello world");
        var root = new VStack { textArea };

        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(40, 10));
        driver.Tick();

        textArea.CaretIndex = 0;

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Right, Modifiers = TerminalModifiers.Ctrl });
        driver.Tick();
        Assert.AreEqual(5, textArea.CaretIndex);

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Right, Modifiers = TerminalModifiers.Ctrl });
        driver.Tick();
        Assert.AreEqual("Hello world".Length, textArea.CaretIndex);

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Left, Modifiers = TerminalModifiers.Ctrl });
        driver.Tick();
        Assert.AreEqual(6, textArea.CaretIndex);

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Left, Modifiers = TerminalModifiers.Ctrl });
        driver.Tick();
        Assert.AreEqual(0, textArea.CaretIndex);
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

    [TestMethod]
    public void TextArea_Tab_Insertion_Uses_Logical_Tab_Width_For_Rendering()
    {
        var textArea = new TextArea("abQw") { CaretIndex = 2 };
        var root = new VStack
        {
            new Padder(textArea).Padding(new Thickness(Left: 2, Top: 0, Right: 0, Bottom: 0)),
        }.Spacing(0);

        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(30, 6));
        driver.Tick();

        driver.App.Focus(textArea);
        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Tab });
        driver.TickUntil(() => textArea.Text == "ab\tQw");

        Assert.AreEqual(3, textArea.CaretIndex);
        Assert.IsTrue(textArea.TryGetCursorCell(out var caretX, out var caretY), "Expected caret to be visible.");

        var screen = new AnsiTestScreen(30, 6);
        screen.Apply(driver.Backend.GetOutText());
        var lines = screen.GetText().Split('\n');
        Assert.IsTrue((uint)caretY < (uint)lines.Length, $"Caret row {caretY} is outside rendered lines.");

        var line = lines[caretY];
        var textStart = line.IndexOf("Qw", StringComparison.Ordinal);
        Assert.IsTrue(textStart >= 0, $"Expected 'Qw' to be visible on caret row. Row: `{line}`");
        Assert.AreEqual(textStart, caretX, "Rendered text is not aligned with the caret after inserting a tab.");
    }

    [TestMethod]
    public void TextArea_WrappedLine_HomeEnd_Use_TwoStep_Navigation()
    {
        var textArea = new TextArea("ABCDEFGHIJKLMNOPQRSTUVWXYZ");
        var root = new VStack { textArea };

        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(8, 6));
        driver.App.Focus(textArea);
        driver.Tick();

        textArea.CaretIndex = 10;
        driver.Tick();

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Home });
        driver.Tick();
        var visualHomeIndex = textArea.CaretIndex;
        Assert.IsTrue(visualHomeIndex >= 0 && visualHomeIndex < 10, $"Expected first Home to move to the wrapped-row start. Actual caret: {visualHomeIndex}.");

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Home });
        driver.TickUntil(() => textArea.CaretIndex == 0);

        textArea.CaretIndex = 10;
        driver.Tick();

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.End });
        driver.Tick();
        var visualEndIndex = textArea.CaretIndex;
        Assert.IsTrue(visualEndIndex > 10 && visualEndIndex < textArea.Text!.Length, $"Expected first End to move to the wrapped-row end. Actual caret: {visualEndIndex}.");

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.End });
        driver.TickUntil(() => textArea.CaretIndex == textArea.Text!.Length);
    }

    [TestMethod]
    public void TextArea_WrappedLine_HomeEnd_State_Resets_After_Other_Navigation()
    {
        var textArea = new TextArea("ABCDEFGHIJKLMNOPQRSTUVWXYZ");
        var root = new VStack { textArea };

        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(8, 6));
        driver.App.Focus(textArea);
        driver.Tick();

        textArea.CaretIndex = 10;
        driver.Tick();

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Home });
        driver.Tick();
        var visualHomeIndex = textArea.CaretIndex;
        Assert.IsTrue(visualHomeIndex >= 0 && visualHomeIndex < 10, $"Expected first Home to move to the wrapped-row start. Actual caret: {visualHomeIndex}.");

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Right });
        driver.Tick();
        var movedRightIndex = textArea.CaretIndex;
        Assert.IsTrue(movedRightIndex > visualHomeIndex, $"Expected Right to move away from the wrapped-row start. Actual caret: {movedRightIndex}, wrapped-row start: {visualHomeIndex}.");

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Home });
        driver.Tick();
        Assert.AreEqual(visualHomeIndex, textArea.CaretIndex, "Expected Home state to reset after another navigation key so the next Home returns only to the wrapped-row start.");
    }

    [TestMethod]
    public void TextArea_WrappedLine_End_Keeps_Caret_Visible_When_It_Moves_To_Next_Row()
    {
        var textArea = new TextArea("ABCDEFGHIJKLMNOPQRSTUVWXYZ");
        var root = new VStack { textArea };

        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(8, 6));
        driver.App.Focus(textArea);
        driver.Tick();

        textArea.CaretIndex = 10;
        driver.Tick();

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.End });
        driver.Tick();

        Assert.IsTrue(textArea.TryGetCursorCell(out var x, out var y), "Expected the caret to stay visible after End moves to the next wrapped row.");
        Assert.IsGreaterThanOrEqualTo(0, x);
        Assert.IsLessThan(8, x);
        Assert.IsGreaterThanOrEqualTo(0, y);
        Assert.IsLessThan(6, y);
    }
}
