// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Hosting;
using System.Linq;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class PromptEditorTests
{
    [TestMethod]
    public void PromptEditor_Renders_Prompt_And_Continuation()
    {
        var editor = new PromptEditor()
            .PromptMarkup("> ")
            .ContinuationPromptMarkup("| ")
            .Text("A\nB");

        var root = new VStack { editor };

        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(20, 5));
        driver.Tick();

        var screen = new AnsiTestScreen(20, 5);
        screen.Apply(driver.Backend.GetOutText());
        var lines = screen.GetText().Split('\n');

        Assert.IsTrue(lines.Any(l => ContainsPromptThenText(l, "> ", "A")), "Expected the first line prompt to render.");
        Assert.IsTrue(lines.Any(l => ContainsPromptThenText(l, "| ", "B")), "Expected the continuation prompt to render.");

        static bool ContainsPromptThenText(string line, string prompt, string text)
        {
            var promptIndex = line.IndexOf(prompt, StringComparison.Ordinal);
            if (promptIndex < 0)
            {
                return false;
            }

            var textIndex = line.IndexOf(text, StringComparison.Ordinal);
            return textIndex > promptIndex;
        }
    }

    [TestMethod]
    public void PromptEditor_EnterAccepts_And_CtrlJ_InsertsNewLine()
    {
        var accepted = false;
        var acceptedText = string.Empty;

        var editor = new PromptEditor()
            .PromptMarkup("> ")
            .ContinuationPromptMarkup("| ")
            .Accepted((_, e) =>
            {
                accepted = true;
                acceptedText = e.Text;
            })
            .AutoFocus(true);

        var root = new VStack { editor };

        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(40, 6));
        driver.Tick();

        driver.Backend.PushEvent(new TerminalTextEvent { Text = "Hello" });
        driver.TickUntil(() => editor.Text == "Hello");

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Unknown, Char = TerminalChar.CtrlJ, Modifiers = TerminalModifiers.Ctrl });
        driver.TickUntil(() => editor.Text == "Hello\n");

        driver.Backend.PushEvent(new TerminalTextEvent { Text = "World" });
        driver.TickUntil(() => editor.Text == "Hello\nWorld");

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Enter });
        driver.TickUntil(() => accepted);

        Assert.AreEqual("Hello\nWorld", acceptedText);
    }

    [TestMethod]
    public void PromptEditor_EnterInsertsNewLine_And_CtrlJ_Accepts()
    {
        var accepted = false;

        var editor = new PromptEditor()
            .EnterMode(PromptEditorEnterMode.EnterInsertsNewLine)
            .Accepted((_, _) => accepted = true)
            .AutoFocus(true);

        var root = new VStack { editor };

        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(40, 6));
        driver.Tick();

        driver.Backend.PushEvent(new TerminalTextEvent { Text = "A" });
        driver.TickUntil(() => editor.Text == "A");

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Enter });
        driver.TickUntil(() => editor.Text == "A\n");

        driver.Backend.PushEvent(new TerminalTextEvent { Text = "B" });
        driver.TickUntil(() => editor.Text == "A\nB");

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Unknown, Char = TerminalChar.CtrlJ, Modifiers = TerminalModifiers.Ctrl });
        driver.TickUntil(() => accepted);
    }

    [TestMethod]
    public void PromptEditor_TabCompletion_Applies_Candidate_In_Inline_Mode()
    {
        static PromptEditorCompletion Complete(in PromptEditorCompletionRequest request)
        {
            var replaceStart = Math.Max(0, request.CaretIndex - 1);
            return new PromptEditorCompletion(
                Handled: true,
                Candidates: new[] { "hello" },
                ReplaceStart: replaceStart,
                ReplaceLength: request.CaretIndex - replaceStart);
        }

        var editor = new PromptEditor()
            .CompletionPresentation(PromptEditorCompletionPresentation.InlineCycle)
            .CompletionHandler(Complete)
            .AutoFocus(true);

        var root = new VStack { editor };

        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(40, 6));
        driver.Tick();

        driver.Backend.PushEvent(new TerminalTextEvent { Text = "h" });
        driver.TickUntil(() => editor.Text == "h");

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Tab });
        driver.TickUntil(() => editor.Text == "hello");
    }
}
