// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using XenoAtom.Terminal;
using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Commands;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Extensions.Markdown;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Hosting;
using XenoAtom.Terminal.UI.Input;
using XenoAtom.Terminal.UI.Rendering;
using XenoAtom.Terminal.UI.Styling;
using XenoAtom.Terminal.UI.Text;

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
    public void PromptEditor_EnterAccepts_And_CtrlN_InsertsNewLine()
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

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Unknown, Char = TerminalChar.CtrlN, Modifiers = TerminalModifiers.Ctrl });
        driver.TickUntil(() => editor.Text == "Hello\n");

        driver.Backend.PushEvent(new TerminalTextEvent { Text = "World" });
        driver.TickUntil(() => editor.Text == "Hello\nWorld");

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Enter });
        driver.TickUntil(() => accepted);

        Assert.AreEqual("Hello\nWorld", acceptedText);
    }

    [TestMethod]
    public void PromptEditor_EnterInsertsNewLine_And_CtrlN_Accepts()
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

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Unknown, Char = TerminalChar.CtrlN, Modifiers = TerminalModifiers.Ctrl });
        driver.TickUntil(() => accepted);
    }

    [TestMethod]
    public void PromptEditor_SingleLineMode_Discards_NewLines_From_Input_And_Paste()
    {
        var accepted = false;

        var editor = new PromptEditor()
            .LineMode(PromptEditorLineMode.SingleLine)
            .Accepted((_, _) => accepted = true)
            .AutoFocus(true);

        var root = new VStack { editor };

        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(40, 6));
        driver.Tick();

        driver.Backend.PushEvent(new TerminalTextEvent { Text = "Hello" });
        driver.TickUntil(() => editor.Text == "Hello");

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Unknown, Char = TerminalChar.CtrlN, Modifiers = TerminalModifiers.Ctrl });
        driver.Tick();

        Assert.AreEqual("Hello", editor.Text);
        Assert.IsFalse(accepted);

        driver.Backend.PushEvent(new TerminalPasteEvent { Text = "\nWorld\r\n!" });
        driver.TickUntil(() => editor.Text == "HelloWorld!");
    }

    [TestMethod]
    public void PromptEditor_SingleLineMode_With_EnterInsertsNewLine_Discards_Enter_And_Keeps_CtrlN_As_Accept()
    {
        var accepted = false;

        var editor = new PromptEditor()
            .LineMode(PromptEditorLineMode.SingleLine)
            .EnterMode(PromptEditorEnterMode.EnterInsertsNewLine)
            .Accepted((_, _) => accepted = true)
            .AutoFocus(true);

        var root = new VStack { editor };

        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(40, 6));
        driver.Tick();

        driver.Backend.PushEvent(new TerminalTextEvent { Text = "A" });
        driver.TickUntil(() => editor.Text == "A");

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Enter });
        driver.Tick();

        Assert.AreEqual("A", editor.Text);
        Assert.IsFalse(accepted);

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Unknown, Char = TerminalChar.CtrlN, Modifiers = TerminalModifiers.Ctrl });
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
    public void PromptEditor_TabCompletion_Applies_Single_Candidate_In_Popup_Mode_Without_Opening_Popup()
    {
        static PromptEditorCompletion Complete(in PromptEditorCompletionRequest request)
        {
            var replaceStart = Math.Max(0, request.CaretIndex - 1);
            return new PromptEditorCompletion(
                Handled: true,
                Candidates: new[] { "help" },
                ReplaceStart: replaceStart,
                ReplaceLength: request.CaretIndex - replaceStart);
        }

        var editor = new PromptEditor()
            .CompletionPresentation(PromptEditorCompletionPresentation.PopupList)
            .CompletionHandler(Complete)
            .AutoFocus(true);

        var root = new VStack { editor };

        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(40, 6));
        driver.Tick();

        driver.Backend.PushEvent(new TerminalTextEvent { Text = "h" });
        driver.TickUntil(() => editor.Text == "h");

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Tab });
        driver.TickUntil(() => editor.Text == "help");

        var popupCount = driver.App.Root.EnumerateVisualsDepthFirst().OfType<Popup>().Count();
        Assert.AreEqual(0, popupCount, "Single-candidate completion should apply directly instead of opening a popup.");
    }

    [TestMethod]
    public void PromptEditor_Tab_And_ShiftTab_Move_Focus_When_Completion_Is_Not_Handled()
    {
        static PromptEditorCompletion Complete(in PromptEditorCompletionRequest request)
            => new(
                Handled: false,
                Candidates: null,
                ReplaceStart: request.CaretIndex,
                ReplaceLength: 0);

        var previous = new Button("Previous");
        var editor = new PromptEditor()
            .CompletionHandler(Complete)
            .AutoFocus(true);
        var next = new Button("Next");

        var root = new VStack(previous, editor, next).Spacing(0);

        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(40, 6));
        driver.Tick();

        Assert.AreSame(editor, driver.App.FocusedElement);

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Tab });
        driver.TickUntil(() => ReferenceEquals(driver.App.FocusedElement, next));

        driver.App.Focus(editor);
        driver.Tick();

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Tab, Modifiers = TerminalModifiers.Shift });
        driver.TickUntil(() => ReferenceEquals(driver.App.FocusedElement, previous));
    }

    [TestMethod]
    public void PromptEditor_Completion_Gesture_Can_Be_Rebound_Off_Tab()
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

        var config = PromptEditorConfig.Default with
        {
            CompleteCommand = PromptEditorConfig.Default.CompleteCommand with
            {
                Gesture = new KeyGesture(TerminalKey.F4),
            },
        };

        var editor = new PromptEditor(config)
            .CompletionPresentation(PromptEditorCompletionPresentation.InlineCycle)
            .CompletionHandler(Complete)
            .AutoFocus(true);
        var next = new Button("Next");

        var root = new VStack(editor, next).Spacing(0);

        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(40, 6));
        driver.Tick();

        Assert.AreSame(editor, driver.App.FocusedElement);

        driver.Backend.PushEvent(new TerminalTextEvent { Text = "h" });
        driver.TickUntil(() => editor.Text == "h");

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Tab });
        driver.TickUntil(() => ReferenceEquals(driver.App.FocusedElement, next));

        driver.App.Focus(editor);
        driver.Tick();

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.F4 });
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
        var root = new VStack { editor };

        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(40, 6));
        driver.Tick();

        AssertCommand(editor, "PromptEditor.Accept", defaultConfig.AcceptCommand);
        AssertCommand(editor, "PromptEditor.Cancel", defaultConfig.CancelCommand);
        AssertCommand(editor, "PromptEditor.InsertNewLine", defaultConfig.InsertNewLineCommand with { Gesture = defaultConfig.InsertNewLineFallbackGesture });
        AssertCommand(editor, "PromptEditor.Complete", defaultConfig.CompleteCommand);
        AssertCommand(editor, "PromptEditor.HistoryPrevious", defaultConfig.HistoryPreviousCommand);
        AssertCommand(editor, "PromptEditor.HistoryNext", defaultConfig.HistoryNextCommand);
    }

    [TestMethod]
    public void PromptEditor_Default_Config_Prefers_ShiftEnter_With_CtrlN_Fallback()
    {
        var defaultConfig = PromptEditorConfig.Default;

        Assert.AreEqual(new KeyGesture(TerminalKey.Enter, TerminalModifiers.Shift), defaultConfig.InsertNewLineCommand.Gesture);
        Assert.AreEqual(new KeyGesture(TerminalChar.CtrlN, TerminalModifiers.Ctrl), defaultConfig.InsertNewLineFallbackGesture);
    }

    [TestMethod]
    public void PromptEditor_Uses_ShiftEnter_For_NewLine_When_Extended_Keys_Are_Supported()
    {
        var editor = new PromptEditor()
            .AutoFocus(true);
        var root = new VStack { editor };

        using var driver = new TerminalAppTestDriver(
            root,
            TerminalHostKind.Fullscreen,
            new TerminalSize(40, 6),
            capabilities: CreateTerminalCapabilities(supportsExtendedKeys: true));
        driver.Tick();

        AssertCommandGesture(editor, "PromptEditor.InsertNewLine", new KeyGesture(TerminalKey.Enter, TerminalModifiers.Shift));

        driver.Backend.PushEvent(new TerminalTextEvent { Text = "Hello" });
        driver.TickUntil(() => editor.Text == "Hello");

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Enter, Modifiers = TerminalModifiers.Shift });
        driver.TickUntil(() => editor.Text == "Hello\n");

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Unknown, Char = TerminalChar.CtrlN, Modifiers = TerminalModifiers.Ctrl });
        driver.Tick();

        Assert.AreEqual("Hello\n", editor.Text, "Ctrl+N should not remain the default insert-newline shortcut when Shift+Enter is available.");
    }

    [TestMethod]
    public void PromptEditor_Uses_CtrlN_Fallback_When_Extended_Keys_Are_Not_Supported()
    {
        var editor = new PromptEditor()
            .AutoFocus(true);
        var root = new VStack { editor };

        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(40, 6));
        driver.Tick();

        AssertCommandGesture(editor, "PromptEditor.InsertNewLine", new KeyGesture(TerminalChar.CtrlN, TerminalModifiers.Ctrl));

        driver.Backend.PushEvent(new TerminalTextEvent { Text = "Hello" });
        driver.TickUntil(() => editor.Text == "Hello");

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Unknown, Char = TerminalChar.CtrlN, Modifiers = TerminalModifiers.Ctrl });
        driver.TickUntil(() => editor.Text == "Hello\n");
    }

    [TestMethod]
    public void PromptEditor_InsertNewLine_Fallback_Gesture_Is_Configurable()
    {
        var config = PromptEditorConfig.Default with
        {
            InsertNewLineFallbackGesture = new KeyGesture(TerminalKey.F4),
        };
        var editor = new PromptEditor(config)
            .AutoFocus(true);
        var root = new VStack { editor };

        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(40, 6));
        driver.Tick();

        AssertCommandGesture(editor, "PromptEditor.InsertNewLine", new KeyGesture(TerminalKey.F4));

        driver.Backend.PushEvent(new TerminalTextEvent { Text = "Hello" });
        driver.TickUntil(() => editor.Text == "Hello");

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.F4 });
        driver.TickUntil(() => editor.Text == "Hello\n");
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

    [TestMethod]
    public void PromptEditor_LineMode_Change_Normalizes_Existing_Text_And_Hides_InsertNewLine_Command()
    {
        var editor = new PromptEditor("hello\nworld")
            .LineMode(PromptEditorLineMode.SingleLine);

        Assert.AreEqual("helloworld", editor.Text);

        var insertNewLineCommand = editor.Commands.First(x => string.Equals(x.Id, "PromptEditor.InsertNewLine", StringComparison.Ordinal));
        Assert.IsFalse(insertNewLineCommand.IsVisibleFor(editor));
        Assert.IsFalse(insertNewLineCommand.CanExecuteFor(editor));
    }

    [TestMethod]
    public void PromptEditor_SingleLineMode_Reports_Single_Row_Default_Size()
    {
        var editor = new PromptEditor()
            .LineMode(PromptEditorLineMode.SingleLine);

        editor.Measure(new LayoutConstraints(0, 120, 0, 10));

        Assert.AreEqual(new Size(48, 1), editor.DesiredSize);
    }

    [TestMethod]
    public void PromptEditor_AutoSizeHeight_Uses_Editor_Column_After_Prompt()
    {
        var editor = new PromptEditor("123456789012")
            .AutoSizeMode(TextEditorAutoSizeMode.Height)
            .PromptMarkup(">> ")
            .ContinuationPromptMarkup(".. ");

        editor.Measure(new LayoutConstraints(0, 12, 0, 10));

        Assert.AreEqual(new Size(12, 2), editor.DesiredSize);
    }

    [TestMethod]
    public void PromptEditor_MouseWheel_Does_Not_Move_Caret_Or_Scroll()
    {
        var editor = new PromptEditor()
            .PromptMarkup("> ")
            .ContinuationPromptMarkup("| ")
            .Text(string.Join("\n", Enumerable.Range(0, 30).Select(i => $"Line {i:00}")))
            .MinHeight(5)
            .MaxHeight(5);

        var root = new VStack { editor };

        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(40, 10));
        driver.Tick();

        driver.App.Focus(editor);
        editor.CaretIndex = 0;
        driver.Tick();

        var initialCaretIndex = editor.CaretIndex;
        var initialOffsetY = editor.Scroll.OffsetY;

        driver.Backend.PushEvent(new TerminalMouseEvent
        {
            Kind = TerminalMouseKind.Wheel,
            Button = TerminalMouseButton.Wheel,
            X = editor.Bounds.X + 1,
            Y = editor.Bounds.Y + 2,
            WheelDelta = -1,
        });

        driver.Tick();

        Assert.AreEqual(initialCaretIndex, editor.CaretIndex, "Wheel scrolling should not move the prompt editor caret.");
        Assert.AreEqual(initialOffsetY, editor.Scroll.OffsetY, "PromptEditor should not scroll itself on mouse-wheel input.");
        Assert.AreSame(editor, driver.App.FocusedElement, "Wheel scrolling should not change focus.");
    }

    [TestMethod]
    public void PromptEditor_MarkdownSelection_RemainsVisible_On_Code_Syntax()
    {
        var markdown = """
            Inline `code`
            ```cs
            int x = 1;
            ```
            """;

        var theme = Theme.FromScheme(ColorScheme.ElderberryDarkSoft);
        var converter = new MarkdownMarkupConverter
        {
            Theme = theme,
        };

        var editor = new PromptEditor()
            .PromptMarkup(string.Empty)
            .ContinuationPromptMarkup(string.Empty)
            .Text(markdown)
            .Highlighter((in PromptEditorHighlightRequest request, List<StyledRun> runs) =>
                converter.Highlight(GetSnapshotText(request.Snapshot), runs));

        var root = new VStack { editor }
            .Style(theme);

        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(40, 8));
        driver.Tick();

        driver.App.Focus(editor);
        driver.Tick();

        var bufferBeforeSelection = GetRenderBuffer(driver.App);
        var inlineCodeStyleBeforeSelection = GetStyleAtText(bufferBeforeSelection, "Inline `code`", 'c');
        Assert.IsTrue(inlineCodeStyleBeforeSelection.TryGetBackground(out var inlineCodeBackground), "Expected markdown inline code to render with an explicit background.");

        var selectionStyle = editor.GetStyle<PromptEditorStyle>().SelectionStyle(theme);
        Assert.IsTrue(selectionStyle.TryGetBackground(out var selectionBackground), "Expected prompt editor selection to define a background.");
        Assert.AreNotEqual(selectionBackground.ToRgb(), inlineCodeBackground.ToRgb(), "Test requires inline code and selection backgrounds to differ.");

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Unknown, Char = TerminalChar.CtrlA, Modifiers = TerminalModifiers.Ctrl });
        driver.TickUntil(() => editor.HasSelection);

        var buffer = GetRenderBuffer(driver.App);

        var selectedPlainTextStyle = GetStyleAtText(buffer, "Inline `code`", 'I');
        var inlineBacktickStyle = GetStyleAtText(buffer, "Inline `code`", '`');
        var inlineCodeStyle = GetStyleAtText(buffer, "Inline `code`", 'c');
        var fencedCodeStyle = GetStyleAtText(buffer, "int x = 1;", 'i');
        var fenceMarkerStyle = GetStyleAtText(buffer, "```cs", '`');

        var expectedSelectionBackground = GetBackground(selectedPlainTextStyle, "Expected selected plain text to render with a background.");

        AssertBackgroundEquals(expectedSelectionBackground, inlineBacktickStyle, "Expected inline code markers to keep the selection background.");
        AssertBackgroundEquals(expectedSelectionBackground, inlineCodeStyle, "Expected inline code content to keep the selection background.");
        AssertBackgroundEquals(expectedSelectionBackground, fenceMarkerStyle, "Expected fenced code markers to keep the selection background.");
        AssertBackgroundEquals(expectedSelectionBackground, fencedCodeStyle, "Expected fenced code content to keep the selection background.");
    }

    private static void AssertCommand(PromptEditor editor, string id, PromptEditorCommandConfig expected)
    {
        var command = editor.Commands.FirstOrDefault(x => string.Equals(x.Id, id, StringComparison.Ordinal));
        Assert.IsNotNull(command, $"Expected command '{id}' to be registered.");
        Assert.AreEqual(expected.LabelMarkup, command.LabelMarkup);
        Assert.AreEqual(expected.DescriptionMarkup, command.DescriptionMarkup);
        Assert.AreEqual(expected.Gesture, command.Gesture);
    }

    private static void AssertCommandGesture(PromptEditor editor, string id, KeyGesture expected)
    {
        var command = editor.Commands.FirstOrDefault(x => string.Equals(x.Id, id, StringComparison.Ordinal));
        Assert.IsNotNull(command, $"Expected command '{id}' to be registered.");
        Assert.AreEqual(expected, command.Gesture);
    }

    private static TerminalCapabilities CreateTerminalCapabilities(bool supportsExtendedKeys)
    {
        return new TerminalCapabilities
        {
            AnsiEnabled = true,
            ColorLevel = TerminalColorLevel.TrueColor,
            SupportsAlternateScreen = true,
            SupportsCursorVisibility = true,
            SupportsMouse = true,
            SupportsPrivateModes = true,
            SupportsRawMode = true,
            SupportsExtendedKeys = supportsExtendedKeys,
            ExtendedKeyProtocol = supportsExtendedKeys ? TerminalExtendedKeyProtocol.KittyKeyboard : TerminalExtendedKeyProtocol.None,
            SupportsCursorPositionGet = true,
            SupportsCursorPositionSet = true,
            SupportsTitleSet = true,
            SupportsWindowSize = true,
            SupportsBufferSize = true,
            SupportsBeep = true,
            IsOutputRedirected = false,
            IsInputRedirected = false,
            TerminalName = "Test",
        };
    }

    private static CellBuffer GetRenderBuffer(TerminalApp app)
        => (CellBuffer)typeof(TerminalApp).GetField("_renderBuffer", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(app)!;

    private static Style GetStyleAtText(CellBuffer buffer, string rowTextFragment, char target)
    {
        for (var y = 0; y < buffer.Height; y++)
        {
            var row = SnapshotRow(buffer, y);
            var rowIndex = row.IndexOf(rowTextFragment, StringComparison.Ordinal);
            if (rowIndex < 0)
            {
                continue;
            }

            var targetOffset = rowTextFragment.IndexOf(target);
            Assert.IsTrue(targetOffset >= 0, $"Could not find target character `{target}` inside fragment `{rowTextFragment}`.");
            return GetCellStyle(buffer, rowIndex + targetOffset, y);
        }

        Assert.Fail($"Could not find rendered fragment `{rowTextFragment}`.");
        return Style.None;
    }

    private static Style GetCellStyle(CellBuffer buffer, int x, int y)
        => buffer.UnsafeCells[(y * buffer.Width) + x];

    private static string SnapshotRow(CellBuffer buffer, int y)
    {
        var scalars = buffer.UnsafeScalars;
        var chars = new char[buffer.Width];
        for (var x = 0; x < buffer.Width; x++)
        {
            chars[x] = (char)scalars[(y * buffer.Width) + x];
        }

        return new string(chars);
    }

    private static string GetSnapshotText(ITextSnapshot snapshot)
    {
        if (snapshot.Length == 0)
        {
            return string.Empty;
        }

        var chars = new char[snapshot.Length];
        snapshot.CopyTo(0, chars);
        return new string(chars);
    }

    private static Color GetBackground(Style style, string message)
    {
        Assert.IsTrue(style.TryGetBackground(out var background), $"{message} Expected an explicit background.");
        return background;
    }

    private static void AssertBackgroundEquals(Color expected, Style actualStyle, string message)
    {
        Assert.AreEqual(expected.ToRgb(), GetBackground(actualStyle, message).ToRgb(), message);
    }
}
