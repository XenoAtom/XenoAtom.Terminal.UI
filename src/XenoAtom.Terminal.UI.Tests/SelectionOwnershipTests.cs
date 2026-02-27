// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Hosting;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class SelectionOwnershipTests
{
    [TestMethod]
    public void SelectionOwner_ClearsParagraph_When_Clicking_LogControl()
    {
        var paragraph = new Paragraph("hello world").HorizontalAlignment(Align.Stretch);
        var log = new LogControl();
        log.AppendLine("First");
        log.AppendLine("Second");

        var root = new VStack(paragraph, log).Spacing(1).HorizontalAlignment(Align.Stretch);
        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(50, 10));
        driver.Tick();

        DragSelectToken(driver, paragraph, "world");
        Assert.IsTrue(paragraph.HasSelection);

        Click(driver, log);
        driver.Tick();

        Assert.IsFalse(paragraph.HasSelection);
    }

    [TestMethod]
    public void SelectionOwner_ClearsLogControl_When_Selecting_Paragraph()
    {
        var log = new LogControl();
        log.AppendLine("First");
        log.AppendLine("Second");

        var paragraph = new Paragraph("hello world").HorizontalAlignment(Align.Stretch);
        var root = new VStack(log, paragraph).Spacing(1).HorizontalAlignment(Align.Stretch);

        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(50, 10));
        driver.Tick();

        Click(driver, log);
        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Unknown, Char = TerminalChar.CtrlA, Modifiers = TerminalModifiers.Ctrl });
        driver.Tick();
        Assert.IsTrue(log.HasSelection);

        DragSelectToken(driver, paragraph, "world");
        Assert.IsTrue(paragraph.HasSelection);
        Assert.IsFalse(log.HasSelection);
    }

    [TestMethod]
    public void TextBlock_MouseDragSelection_CtrlC_CopiesSelectedText()
    {
        var textBlock = new TextBlock("hello world").HorizontalAlignment(Align.Stretch);

        using var driver = new TerminalAppTestDriver(textBlock, TerminalHostKind.Fullscreen, new TerminalSize(30, 4));
        driver.Tick();

        DragSelectToken(driver, textBlock, "world");
        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Unknown, Char = TerminalChar.CtrlC, Modifiers = TerminalModifiers.Ctrl });
        driver.Tick();

        Assert.AreEqual("world", driver.Terminal.Clipboard.Text);
    }

    [TestMethod]
    public void TextBlock_CanSelect_LastCharacter_When_Dragging_Past_RightEdge()
    {
        var textBlock = new TextBlock("ab").HorizontalAlignment(Align.Stretch);

        using var driver = new TerminalAppTestDriver(textBlock, TerminalHostKind.Fullscreen, new TerminalSize(2, 1));
        driver.Tick();

        var y = textBlock.Bounds.Y;
        var startX = textBlock.Bounds.X + 1;
        var endX = textBlock.Bounds.X + 2;

        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Down, Button = TerminalMouseButton.Left, X = startX, Y = y });
        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Drag, Button = TerminalMouseButton.Left, X = endX, Y = y });
        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Up, Button = TerminalMouseButton.Left, X = endX, Y = y });

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Unknown, Char = TerminalChar.CtrlC, Modifiers = TerminalModifiers.Ctrl });
        driver.Tick();

        Assert.AreEqual("b", driver.Terminal.Clipboard.Text);
    }

    [TestMethod]
    public void SelectionOwner_ClearsParagraph_When_Clicking_TextBlock()
    {
        var paragraph = new Paragraph("hello world").HorizontalAlignment(Align.Stretch);
        var textBlock = new TextBlock("click me").HorizontalAlignment(Align.Stretch);
        var root = new VStack(paragraph, textBlock).Spacing(1).HorizontalAlignment(Align.Stretch);

        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(40, 8));
        driver.Tick();

        DragSelectToken(driver, paragraph, "world");
        Assert.IsTrue(paragraph.HasSelection);

        Click(driver, textBlock);
        driver.Tick();

        Assert.IsFalse(paragraph.HasSelection);
    }

    [TestMethod]
    public void SelectionOwner_ClearsParagraph_When_Clicking_TextEditor()
    {
        var paragraph = new Paragraph("hello world").HorizontalAlignment(Align.Stretch);
        var editor = new TextBox("alpha beta gamma").HorizontalAlignment(Align.Stretch);
        var root = new VStack(paragraph, editor).Spacing(1).HorizontalAlignment(Align.Stretch);

        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(60, 10));
        driver.Tick();

        DragSelectToken(driver, paragraph, "world");
        Assert.IsTrue(paragraph.HasSelection);

        Click(driver, editor);
        driver.Tick();

        Assert.IsFalse(paragraph.HasSelection);
    }

    [TestMethod]
    public void Markup_MouseDragSelection_CtrlC_CopiesSelectedText()
    {
        var markup = new Markup("[bold]hello[/] [red]world[/]").HorizontalAlignment(Align.Stretch);

        using var driver = new TerminalAppTestDriver(markup, TerminalHostKind.Fullscreen, new TerminalSize(30, 3));
        driver.Tick();

        DragSelectToken(driver, markup, "world");
        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Unknown, Char = TerminalChar.CtrlC, Modifiers = TerminalModifiers.Ctrl });
        driver.Tick();

        Assert.AreEqual("world", driver.Terminal.Clipboard.Text);
    }

    private static void Click(TerminalAppTestDriver driver, Visual visual)
    {
        var x = visual.Bounds.X + 1;
        var y = visual.Bounds.Y;
        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Down, Button = TerminalMouseButton.Left, X = x, Y = y });
        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Up, Button = TerminalMouseButton.Left, X = x, Y = y });
    }

    private static void DragSelectToken(TerminalAppTestDriver driver, Paragraph paragraph, string token)
    {
        var text = paragraph.Text ?? string.Empty;
        var start = text.IndexOf(token, StringComparison.Ordinal);
        Assert.IsTrue(start >= 0, $"Token `{token}` was not found in paragraph text.");

        var y = paragraph.Bounds.Y;
        var startX = paragraph.Bounds.X + start;
        var endX = startX + token.Length;

        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Down, Button = TerminalMouseButton.Left, X = startX, Y = y });
        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Drag, Button = TerminalMouseButton.Left, X = endX, Y = y });
        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Up, Button = TerminalMouseButton.Left, X = endX, Y = y });
        driver.Tick();
    }

    private static void DragSelectToken(TerminalAppTestDriver driver, TextBlock textBlock, string token)
    {
        var text = textBlock.Text ?? string.Empty;
        var start = text.IndexOf(token, StringComparison.Ordinal);
        Assert.IsTrue(start >= 0, $"Token `{token}` was not found in TextBlock text.");

        var y = textBlock.Bounds.Y;
        var startX = textBlock.Bounds.X + start;
        var endX = startX + token.Length;

        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Down, Button = TerminalMouseButton.Left, X = startX, Y = y });
        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Drag, Button = TerminalMouseButton.Left, X = endX, Y = y });
        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Up, Button = TerminalMouseButton.Left, X = endX, Y = y });
        driver.Tick();
    }

    private static void DragSelectToken(TerminalAppTestDriver driver, Markup markup, string token)
    {
        // Token position is based on the rendered plain text (markup tags removed).
        const string plainText = "hello world";
        var start = plainText.IndexOf(token, StringComparison.Ordinal);
        Assert.IsTrue(start >= 0, $"Token `{token}` was not found in Markup text.");

        var y = markup.Bounds.Y;
        var startX = markup.Bounds.X + start;
        var endX = startX + token.Length;

        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Down, Button = TerminalMouseButton.Left, X = startX, Y = y });
        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Drag, Button = TerminalMouseButton.Left, X = endX, Y = y });
        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Up, Button = TerminalMouseButton.Left, X = endX, Y = y });
        driver.Tick();
    }
}
