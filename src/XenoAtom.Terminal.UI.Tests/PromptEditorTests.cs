// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Linq;
using XenoAtom.Terminal;
using XenoAtom.Terminal.UI.Commands;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Hosting;
using XenoAtom.Terminal.UI.Input;

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

    [TestMethod]
    public void PromptEditor_Escape_Cancels_By_Default()
    {
        var canceled = false;

        var editor = new PromptEditor()
            .Canceled((_, _) => canceled = true)
            .AutoFocus(true);

        var root = new VStack { editor };

        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(40, 6));
        driver.Tick();

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Escape });
        driver.TickUntil(() => canceled);
    }

    [TestMethod]
    public void PromptEditor_Can_Reserve_Escape_Only_While_Completion_Is_Active()
    {
        static PromptEditorCompletion Complete(in PromptEditorCompletionRequest request)
            => new(
                Handled: true,
                Candidates: ["hello", "help"],
                ReplaceStart: 0,
                ReplaceLength: request.CaretIndex);

        var canceled = false;
        var customEscapeCount = 0;

        var editor = new PromptEditor()
            .EscapeBehavior(PromptEditorEscapeBehavior.CancelCompletionOnly)
            .CompletionPresentation(PromptEditorCompletionPresentation.InlineCycle)
            .CompletionHandler(Complete)
            .Canceled((_, _) => canceled = true)
            .AutoFocus(true);

        editor.AddCommand(new Command
        {
            Id = "Custom.Close",
            LabelMarkup = "Close",
            Gesture = new KeyGesture(TerminalKey.Escape),
            Execute = _ => customEscapeCount++,
        });

        var root = new VStack { editor };

        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(40, 6));
        driver.Tick();

        driver.Backend.PushEvent(new TerminalTextEvent { Text = "h" });
        driver.TickUntil(() => editor.Text == "h");

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Escape });
        driver.TickUntil(() => customEscapeCount == 1);

        Assert.IsFalse(canceled);

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Tab });
        driver.TickUntil(() => editor.Text == "hello");

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Escape });
        driver.Tick();

        Assert.AreEqual(1, customEscapeCount);
        Assert.IsFalse(canceled);

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Tab });
        driver.Tick();

        Assert.AreEqual("hello", editor.Text);
    }

    [TestMethod]
    public void PromptEditor_Uses_Default_Command_Config_When_Config_Is_Null()
    {
        var editor = new PromptEditor((PromptEditorConfig?)null);
        var defaultConfig = PromptEditorConfig.Default;

        AssertCommand(editor, "PromptEditor.Accept", defaultConfig.AcceptCommand);
        AssertCommand(editor, "PromptEditor.Cancel", defaultConfig.CancelCommand);
        AssertCommand(editor, "PromptEditor.InsertNewLine", defaultConfig.InsertNewLineCommand);
        AssertCommand(editor, "PromptEditor.Complete", defaultConfig.CompleteCommand);
        AssertCommand(editor, "PromptEditor.HistoryPrevious", defaultConfig.HistoryPreviousCommand);
        AssertCommand(editor, "PromptEditor.HistoryNext", defaultConfig.HistoryNextCommand);
    }

    [TestMethod]
    public void PromptEditor_Applies_Custom_Command_Config()
    {
        var defaultConfig = PromptEditorConfig.Default;
        var config = defaultConfig with
        {
            AcceptCommand = defaultConfig.AcceptCommand with
            {
                LabelMarkup = "Submit",
                DescriptionMarkup = "Submit the current prompt text.",
                Gesture = new KeyGesture(TerminalChar.CtrlM, TerminalModifiers.Ctrl),
            },
            InsertNewLineCommand = defaultConfig.InsertNewLineCommand with
            {
                LabelMarkup = "Line break",
                DescriptionMarkup = "Insert a line break with Enter.",
                Gesture = new KeyGesture(TerminalKey.Enter),
            },
        };

        var editor = new PromptEditor(config)
            .EnterMode(PromptEditorEnterMode.EnterInsertsNewLine);

        AssertCommand(editor, "PromptEditor.Accept", config.AcceptCommand);
        AssertCommand(editor, "PromptEditor.InsertNewLine", config.InsertNewLineCommand);
    }

    private static void AssertCommand(PromptEditor editor, string id, PromptEditorCommandConfig expected)
    {
        var command = editor.Commands.FirstOrDefault(x => string.Equals(x.Id, id, StringComparison.Ordinal));
        Assert.IsNotNull(command, $"Expected command '{id}' to be registered.");
        Assert.AreEqual(expected.LabelMarkup, command.LabelMarkup);
        Assert.AreEqual(expected.DescriptionMarkup, command.DescriptionMarkup);
        Assert.AreEqual(expected.Gesture, command.Gesture);
    }
}
