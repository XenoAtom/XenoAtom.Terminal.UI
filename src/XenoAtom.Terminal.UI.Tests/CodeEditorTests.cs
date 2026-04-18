// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using XenoAtom.Terminal;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Extensions.CodeEditor.TextMateSharp;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Hosting;
using XenoAtom.Terminal.UI.Input;
using XenoAtom.Terminal.UI.Rendering;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class CodeEditorTests
{
    [TestMethod]
    public void CodeEditor_Shows_LineNumbers_By_Default_And_Blanks_Continuation_Rows()
    {
        var editor = new CodeEditor("1234567890\nB")
        {
            MinHeight = 4,
            MaxHeight = 4,
        };

        var root = new VStack { editor };
        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(10, 6));
        driver.Tick();

        var screen = new AnsiTestScreen(10, 6);
        screen.Apply(driver.Backend.GetOutText());
        var lines = screen.GetText().Split('\n');

        Assert.IsTrue(lines[0].StartsWith("  1", StringComparison.Ordinal), $"Expected line number on the first wrapped row. Row: `{lines[0]}`");
        Assert.IsTrue(lines[1].StartsWith("  ", StringComparison.Ordinal), $"Expected continuation row gutter to stay blank. Row: `{lines[1]}`");
        Assert.IsTrue(lines.Any(l => l.StartsWith("  2", StringComparison.Ordinal)), "Expected second logical line number to render.");
    }

    [TestMethod]
    public void CodeEditor_TogglingLineNumbers_Rerenders_Through_Bindable_State()
    {
        var editor = new CodeEditor("Alpha\nBeta")
        {
            MinHeight = 3,
            MaxHeight = 3,
            ShowLineNumbers = false,
        };

        var root = new VStack { editor };
        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(14, 5));
        driver.Tick();

        var withoutNumbers = new AnsiTestScreen(14, 5);
        withoutNumbers.Apply(driver.Backend.GetOutText());
        Assert.IsFalse(
            withoutNumbers.GetText().Split('\n').Any(line => line.Contains("1│", StringComparison.Ordinal)),
            "Did not expect line-number gutter before enabling the bindable property.");

        editor.ShowLineNumbers = true;
        driver.Tick();

        var withNumbers = new AnsiTestScreen(14, 5);
        withNumbers.Apply(driver.Backend.GetOutText());
        Assert.IsTrue(
            withNumbers.GetText().Split('\n').Any(line => line.Contains("1│", StringComparison.Ordinal)),
            "Expected the editor to rerender after toggling ShowLineNumbers without manual render requests.");
    }

    [TestMethod]
    public void CodeEditor_LineNumberWidth_Adapts_To_Visible_Range()
    {
        var text = string.Join("\n", Enumerable.Range(1, 150).Select(i => $"Line {i:000}"));
        var editor = new CodeEditor(text)
        {
            MinHeight = 6,
            MaxHeight = 6,
        };

        var root = new VStack { editor };
        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(20, 8));
        driver.Tick();
        driver.App.Focus(editor);

        var startScreen = new AnsiTestScreen(20, 8);
        startScreen.Apply(driver.Backend.GetOutText());
        var startLine = startScreen.GetText().Split('\n')[0];
        var startTextIndex = startLine.IndexOf("Line 001", StringComparison.Ordinal);
        Assert.IsTrue(startTextIndex >= 3, $"Expected a compact gutter near the start of the file. Row: `{startLine}`");

        for (var i = 0; i < 118; i++)
        {
            driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Down });
        }

        driver.TickUntil(() => editor.Scroll.OffsetY > 0);

        var laterScreen = new AnsiTestScreen(20, 8);
        laterScreen.Apply(driver.Backend.GetOutText());
        var laterLine = laterScreen.GetText().Split('\n')[0];
        Assert.IsTrue(laterLine.Length >= 5, $"Expected rendered row to be long enough to inspect gutter width. Row: `{laterLine}`");
        Assert.IsTrue(char.IsDigit(laterLine[1]) && char.IsDigit(laterLine[2]) && char.IsDigit(laterLine[3]), $"Expected a three-digit visible line number after scrolling. Row: `{laterLine}`");
        Assert.AreEqual('│', laterLine[4], $"Expected the margin separator to shift right once the gutter expands. Row: `{laterLine}`");
    }

    [TestMethod]
    public void CodeEditor_Custom_Right_Margin_Stays_Aligned_While_Scrolling()
    {
        var margin = new TestRightMargin();
        var editor = new CodeEditor(string.Join("\n", Enumerable.Range(1, 20).Select(i => $"Row {i:00}")))
        {
            MinHeight = 5,
            MaxHeight = 5,
        };
        editor.RightMargins.Add(margin);

        var root = new VStack { editor };
        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(20, 8));
        driver.Tick();

        driver.App.Post(() => editor.Scroll.SetOffset(0, 4));
        driver.Tick();

        CollectionAssert.AreEqual(new[] { 4, 5, 6, 7, 8 }, margin.RenderedLineIndices.ToArray());
    }

    [TestMethod]
    public void CodeEditor_Simple_Highlighter_Applies_Style_To_Visible_Segments()
    {
        var style = CodeEditorStyle.Default with
        {
            Background = Color.Basic16(0),
        };

        var editor = new CodeEditor("alpha beta\ngamma")
            .Style(style)
            .Highlighter(static (in CodeEditorLineHighlightRequest request, List<StyledRun> runs) =>
            {
                if (request.LineIndex == 0)
                {
                    runs.Add(new StyledRun(0, 5, Style.None.WithForeground(Color.Basic16(1))));
                }
            });

        var buffer = VisualSnapshotRenderer.Render(editor, width: 20, maxHeight: 4, Theme.Default);
        var rowText = SnapshotRow(buffer, 0);
        var alphaIndex = rowText.IndexOf("alpha", StringComparison.Ordinal);
        Assert.IsTrue(alphaIndex >= 0, $"Expected first line text to render. Row: `{rowText}`");

        var alphaCellStyle = GetCellStyle(buffer, alphaIndex, 0);
        Assert.IsTrue(alphaCellStyle.TryGetForeground(out var foreground), "Expected highlighted text to carry a foreground color.");
        Assert.AreEqual(Color.Basic16(1), foreground);
    }

    [TestMethod]
    public void CodeEditor_Advanced_SyntaxHighlighter_Does_Not_Rebuild_On_Scroll()
    {
        var highlighter = new CountingSyntaxHighlighter();
        var editor = new CodeEditor(string.Join("\n", Enumerable.Range(1, 100).Select(i => $"Line {i:000}")))
        {
            MinHeight = 6,
            MaxHeight = 6,
            SyntaxHighlighter = highlighter,
        };

        var root = new VStack { editor };
        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(30, 8));
        driver.Tick();

        Assert.AreEqual(1, highlighter.BuildCount, "Expected initial render to build syntax state once.");

        driver.App.Post(() => editor.Scroll.SetOffset(0, 20));
        driver.Tick();

        Assert.AreEqual(1, highlighter.BuildCount, "Pure scrolling should not rebuild syntax state.");
        Assert.AreEqual(0, highlighter.UpdateCount, "Pure scrolling should not trigger incremental syntax updates.");
        Assert.IsTrue(highlighter.LineRequestCount > 0, "Expected visible-line syntax requests during rendering.");
    }

    [TestMethod]
    public void CodeEditor_Advanced_SyntaxHighlighter_Updates_After_Edit()
    {
        var highlighter = new CountingSyntaxHighlighter();
        var editor = new CodeEditor("one\ntwo\nthree")
        {
            SyntaxHighlighter = highlighter,
        };

        var root = new VStack { editor };
        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(20, 6));
        driver.Tick();
        driver.App.Focus(editor);

        driver.Backend.PushEvent(new TerminalTextEvent { Text = "X" });
        driver.TickUntil(() => editor.Text == "Xone\ntwo\nthree");

        Assert.IsGreaterThanOrEqualTo(highlighter.BuildCount, 1, "Expected initial build to happen before incremental updates.");
        Assert.AreEqual(1, highlighter.UpdateCount, "Expected a document edit to trigger an incremental syntax update.");
    }

    [TestMethod]
    public void CodeEditor_CtrlF_Opens_Find_Popup()
    {
        var editor = new CodeEditor("foo bar foo")
        {
            MinHeight = 5,
            MaxHeight = 5,
        };

        var root = new VStack { editor };
        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(40, 12));
        driver.Tick();
        driver.App.Focus(editor);

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Unknown, Char = TerminalChar.CtrlF, Modifiers = TerminalModifiers.Ctrl });
        driver.Tick();

        var popup = editor.EnumerateVisualsDepthFirst().OfType<SearchReplacePopup>().Single();
        Assert.IsTrue(popup.IsOpen, "Expected Ctrl+F to open the code editor find popup.");
    }

    [TestMethod]
    public void CodeEditor_CtrlG_Opens_GoToLine_Popup_Centered_On_Editor()
    {
        var editor = new CodeEditor("one\ntwo\nthree\nfour\nfive")
        {
            MinHeight = 5,
            MaxHeight = 5,
        };

        using var driver = new TerminalAppTestDriver(new VStack { editor }, TerminalHostKind.Fullscreen, new TerminalSize(40, 12));
        driver.Tick();
        driver.App.Focus(editor);

        SendCtrlGesture(driver, TerminalChar.CtrlG);
        driver.Tick();

        var popup = driver.App.Root.EnumerateVisualsDepthFirst().OfType<Popup>().Single();
        var editorRect = GetEditorRect(editor);
        var expectedLeft = editorRect.X + Math.Max(0, (editorRect.Width - popup.PopupRect.Width) / 2);
        var expectedTop = editorRect.Y + Math.Max(0, (editorRect.Height - popup.PopupRect.Height) / 2);

        Assert.AreEqual(expectedLeft, popup.PopupRect.X, "Expected Ctrl+G to center the Go To Line popup horizontally inside the code editor surface.");
        Assert.AreEqual(expectedTop, popup.PopupRect.Y, "Expected Ctrl+G to center the Go To Line popup vertically inside the code editor surface.");
        Assert.IsInstanceOfType<NumberBox<int>>(driver.App.FocusedElement, "Expected the Go To Line number box to receive focus when the popup opens.");
    }

    [TestMethod]
    public void CodeEditor_GoToLine_Popup_Enter_Navigates_To_Requested_Line()
    {
        var editor = new CodeEditor(string.Join('\n', Enumerable.Range(1, 12).Select(i => $"Line {i:00}")))
        {
            MinHeight = 5,
            MaxHeight = 5,
        };

        using var driver = new TerminalAppTestDriver(new VStack { editor }, TerminalHostKind.Fullscreen, new TerminalSize(40, 12));
        driver.Tick();
        driver.App.Focus(editor);

        SendCtrlGesture(driver, TerminalChar.CtrlG);
        driver.Tick();

        var numberBox = driver.App.Root.EnumerateVisualsDepthFirst().OfType<NumberBox<int>>().Single();
        numberBox.Value = 9;
        driver.Tick();

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Enter });
        driver.TickUntil(() => driver.App.Root.EnumerateVisualsDepthFirst().OfType<Popup>().Count() == 0);

        Assert.AreEqual(9, editor.Line, "Expected Enter in the Go To Line popup to move the caret to the requested line.");
        Assert.AreEqual(0, driver.App.Root.EnumerateVisualsDepthFirst().OfType<Popup>().Count(), "Expected the Go To Line popup to close after a successful navigation.");
        Assert.AreSame(editor, driver.App.FocusedElement, "Expected focus to return to the editor after closing the Go To Line popup.");
    }

    [TestMethod]
    public void CodeEditor_GoToLine_Popup_Typed_Enter_Closes_And_Input_Returns_To_Editor()
    {
        var editor = new CodeEditor(string.Join('\n', Enumerable.Range(1, 12).Select(i => $"Line {i:00}")))
        {
            MinHeight = 5,
            MaxHeight = 5,
        };

        using var driver = new TerminalAppTestDriver(new VStack { editor }, TerminalHostKind.Fullscreen, new TerminalSize(40, 12));
        driver.Tick();
        driver.App.Focus(editor);

        SendCtrlGesture(driver, TerminalChar.CtrlG);
        driver.Tick();

        driver.Backend.PushEvent(new TerminalTextEvent { Text = "9" });
        driver.Tick();

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Enter });
        driver.TickUntil(() => driver.App.Root.EnumerateVisualsDepthFirst().OfType<Popup>().Count() == 0);

        Assert.AreEqual(9, editor.Line, "Expected typing a line number then pressing Enter to move the caret to that line.");
        Assert.AreSame(editor, driver.App.FocusedElement, "Expected focus to return to the editor after the typed Go To Line workflow completes.");

        driver.Backend.PushEvent(new TerminalTextEvent { Text = "X" });
        driver.TickUntil(() => editor.Text!.Contains("X", StringComparison.Ordinal));
        Assert.AreSame(editor, driver.App.FocusedElement, "Expected subsequent input to go back to the editor instead of remaining trapped in the popup.");
    }

    [TestMethod]
    public void CodeEditor_GoToLine_Popup_Can_Reopen_After_A_Successful_Navigation()
    {
        var editor = new CodeEditor(string.Join('\n', Enumerable.Range(1, 12).Select(i => $"Line {i:00}")))
        {
            MinHeight = 5,
            MaxHeight = 5,
        };

        using var driver = new TerminalAppTestDriver(new VStack { editor }, TerminalHostKind.Fullscreen, new TerminalSize(40, 12));
        driver.Tick();
        driver.App.Focus(editor);

        SendCtrlGesture(driver, TerminalChar.CtrlG);
        driver.Tick();
        driver.Backend.PushEvent(new TerminalTextEvent { Text = "9" });
        driver.Tick();
        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Enter });
        driver.TickUntil(() => driver.App.Root.EnumerateVisualsDepthFirst().OfType<Popup>().Count() == 0);

        Assert.AreEqual(9, editor.Line, "Expected the first Go To Line interaction to navigate successfully.");

        SendCtrlGesture(driver, TerminalChar.CtrlG);
        driver.Tick();
        Assert.AreEqual(1, driver.App.Root.EnumerateVisualsDepthFirst().OfType<Popup>().Count(), "Expected the Go To Line popup to reopen after a previous successful close.");

        driver.Backend.PushEvent(new TerminalTextEvent { Text = "3" });
        driver.Tick();
        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Enter });
        driver.TickUntil(() => driver.App.Root.EnumerateVisualsDepthFirst().OfType<Popup>().Count() == 0);

        Assert.AreEqual(3, editor.Line, "Expected the reopened Go To Line popup to close cleanly and navigate again.");
        Assert.AreSame(editor, driver.App.FocusedElement, "Expected focus to return to the editor after reopening and closing Go To Line again.");
    }

    [TestMethod]
    public void CodeEditor_GoToLine_Popup_Escape_Restores_Previous_Caret()
    {
        var editor = new CodeEditor(string.Join('\n', Enumerable.Range(1, 10).Select(i => $"Line {i:00}")))
        {
            MinHeight = 5,
            MaxHeight = 5,
        };

        using var driver = new TerminalAppTestDriver(new VStack { editor }, TerminalHostKind.Fullscreen, new TerminalSize(40, 12));
        driver.Tick();

        editor.GoToLine(6, 3);
        driver.Tick();
        var originalCaret = editor.CaretIndex;

        driver.App.Focus(editor);

        SendCtrlGesture(driver, TerminalChar.CtrlG);
        driver.Tick();

        editor.GoToLine(2, 1);
        driver.Tick();

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Escape });
        driver.TickUntil(() => driver.App.Root.EnumerateVisualsDepthFirst().OfType<Popup>().Count() == 0);

        Assert.AreEqual(originalCaret, editor.CaretIndex, "Expected Escape in the Go To Line popup to restore the caret position captured when the popup opened.");
        Assert.AreEqual(0, driver.App.Root.EnumerateVisualsDepthFirst().OfType<Popup>().Count(), "Expected Escape to close the Go To Line popup.");
        Assert.AreSame(editor, driver.App.FocusedElement, "Expected focus to return to the editor after cancelling Go To Line.");
    }

    [TestMethod]
    public void CodeEditor_GoToLine_Config_Can_Customize_Gesture_Text_And_Alignment()
    {
        var config = new CodeEditorConfig
        {
            GoToLine = new CodeEditorGoToLineConfig
            {
                Command = new CodeEditorCommandConfig(
                    "Jump",
                    "Jump to a line.",
                    new KeyGesture(TerminalChar.CtrlL, TerminalModifiers.Ctrl)),
                PromptText = "Line #:",
                PopupHorizontalAlignment = Align.End,
                PopupVerticalAlignment = Align.End,
                PopupOffsetX = -2,
                PopupOffsetY = -1,
            },
        };

        var editor = new CodeEditor("one\ntwo\nthree\nfour\nfive", config)
        {
            MinHeight = 5,
            MaxHeight = 5,
        };

        using var driver = new TerminalAppTestDriver(new VStack { editor }, TerminalHostKind.Fullscreen, new TerminalSize(40, 12));
        driver.Tick();
        driver.App.Focus(editor);

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Unknown, Char = TerminalChar.CtrlG, Modifiers = TerminalModifiers.Ctrl });
        driver.Tick();
        Assert.AreEqual(0, driver.App.Root.EnumerateVisualsDepthFirst().OfType<Popup>().Count(), "Expected the default Ctrl+G gesture to stop working when a custom Go To Line gesture is configured.");

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Unknown, Char = TerminalChar.CtrlL, Modifiers = TerminalModifiers.Ctrl });
        driver.Tick();

        var popup = driver.App.Root.EnumerateVisualsDepthFirst().OfType<Popup>().Single();
        var prompt = driver.App.Root.EnumerateVisualsDepthFirst().OfType<TextBlock>().Single(tb => tb.Text == "Line #:");
        var editorRect = GetEditorRect(editor);
        var expectedLeft = Math.Max(editorRect.X, editorRect.Right - popup.PopupRect.Width - 2);
        var expectedTop = Math.Max(editorRect.Y, editorRect.Bottom - popup.PopupRect.Height - 1);

        Assert.AreEqual(expectedLeft, popup.PopupRect.X, "Expected the configured horizontal alignment and offset to move the Go To Line popup.");
        Assert.AreEqual(expectedTop, popup.PopupRect.Y, "Expected the configured vertical alignment and offset to move the Go To Line popup.");
        Assert.AreEqual("Line #:", prompt.Text, "Expected the configured Go To Line prompt text to be shown inside the popup.");
    }

    [TestMethod]
    public void CodeEditor_GoToLine_Can_Be_Disabled_At_Init_Time()
    {
        var editor = new CodeEditor(
            "one\ntwo\nthree",
            new CodeEditorConfig
            {
                GoToLine = CodeEditorGoToLineConfig.Disabled,
            })
        {
            MinHeight = 5,
            MaxHeight = 5,
        };

        using var driver = new TerminalAppTestDriver(new VStack { editor }, TerminalHostKind.Fullscreen, new TerminalSize(40, 12));
        driver.Tick();
        driver.App.Focus(editor);

        SendCtrlGesture(driver, TerminalChar.CtrlG);
        driver.Tick();

        Assert.AreEqual(0, driver.App.Root.EnumerateVisualsDepthFirst().OfType<Popup>().Count(), "Expected Ctrl+G to do nothing when Go To Line is disabled in the immutable CodeEditor configuration.");
        Assert.IsFalse(editor.OpenGoToLine(), "Expected OpenGoToLine to report that the feature is unavailable when disabled at initialization.");
    }

    [TestMethod]
    public void CodeEditor_Search_Highlights_Compose_With_Syntax_Highlighting()
    {
        var editorStyle = CodeEditorStyle.Default with
        {
            SearchMatchBackground = Color.Basic16(3),
            ActiveSearchMatchBackground = Color.Basic16(4),
        };

        var editor = new CodeEditor("foo bar foo")
            .Style(editorStyle)
            .Highlighter(static (in CodeEditorLineHighlightRequest request, List<StyledRun> runs) =>
            {
                if (request.LineLength > 0)
                {
                    runs.Add(new StyledRun(0, request.LineLength, Style.None.WithForeground(Color.Basic16(1))));
                }
            });

        using var driver = new TerminalAppTestDriver(new VStack { editor }, TerminalHostKind.Fullscreen, new TerminalSize(24, 6));
        driver.Tick();

        editor.CreateSearchReplaceTarget().SetQuery(new SearchQuery("foo", CaseSensitive: false, WholeWord: false, UseRegex: false));
        driver.Tick();

        var runs = editor.GetHighlightRunsForTests(0);
        Assert.IsNotNull(runs, "Expected visible line highlight runs to be cached.");
        Assert.AreEqual(0, editor.GetSearchStateForTests().ActiveMatchIndex, "Expected the first match to be active when the caret is at the start of the document.");
        Assert.AreEqual(2, editor.GetSearchStateForTests().Matches.Count, "Expected both search matches to be tracked.");
        Assert.IsTrue(runs.Any(run => run.Start == 0), "Expected an active search highlight run at the first match.");
        Assert.IsTrue(runs.Any(run => run.Start == 8), "Expected an inactive search highlight run at the second match.");

        var activeRun = runs.OrderByDescending(run => run.Length).First(run => run.Start == 0);
        var inactiveRun = runs.OrderByDescending(run => run.Length).First(run => run.Start == 8);

        Assert.IsTrue(activeRun.Style.TryGetForeground(out var activeForeground), "Expected syntax highlight foreground on active match.");
        Assert.AreEqual(Color.Basic16(1), activeForeground);
        Assert.IsTrue(activeRun.Style.TryGetBackground(out var activeBackground), "Expected active search match background.");
        Assert.AreEqual(Color.Basic16(4), activeBackground);

        Assert.IsTrue(inactiveRun.Style.TryGetForeground(out var inactiveForeground), "Expected syntax highlight foreground on inactive match.");
        Assert.AreEqual(Color.Basic16(1), inactiveForeground);
        Assert.IsTrue(inactiveRun.Style.TryGetBackground(out var inactiveBackground), "Expected inactive search match background.");
        Assert.AreEqual(Color.Basic16(3), inactiveBackground);
    }

    [TestMethod]
    public void CodeEditor_Scrolling_Recomputes_Only_Newly_Visible_Line_Highlights()
    {
        var requestedLines = new List<int>();
        var editor = new CodeEditor(string.Join("\n", Enumerable.Range(0, 10).Select(i => $"Line {i}")))
        {
            MinHeight = 4,
            MaxHeight = 4,
        };
        editor.Highlighter((in CodeEditorLineHighlightRequest request, List<StyledRun> runs) =>
        {
            requestedLines.Add(request.LineIndex);
            if (request.LineLength > 0)
            {
                runs.Add(new StyledRun(0, Math.Min(4, request.LineLength), Style.None.WithForeground(Color.Basic16(2))));
            }
        });

        var root = new VStack { editor };
        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(24, 6));
        driver.Tick();

        requestedLines.Clear();
        driver.App.Post(() => editor.Scroll.SetOffset(0, 1));
        driver.Tick();

        CollectionAssert.AreEqual(new[] { 4 }, requestedLines.Distinct().ToArray(), "Expected scrolling by one row to request highlighting only for the newly visible logical line.");
        Assert.AreEqual(1, editor.GetLastVisibleLineRequestCountForTests(), "Expected highlight recomputation to be limited to newly visible lines.");
        Assert.AreEqual(4, editor.GetCachedHighlightLineCountForTests(), "Expected the highlight cache to remain bounded to the current viewport lines.");
    }

    [TestMethod]
    public void CodeEditor_Viewport_Width_Change_Rewraps_Without_Rebuilding_Syntax_State()
    {
        var highlighter = new CountingSyntaxHighlighter();
        var editor = new CodeEditor("abcdefghijklmnopqrstuvwxyz\nsecond line")
        {
            MinHeight = 4,
            MaxHeight = 4,
            SyntaxHighlighter = highlighter,
        };

        var root = new VStack { editor };
        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(30, 6));
        driver.Tick();

        Assert.AreEqual(1, highlighter.BuildCount, "Expected initial render to build syntax state once.");

        driver.Backend.SetSize(new TerminalSize(18, 6));
        driver.Tick();

        Assert.AreEqual(1, highlighter.BuildCount, "Changing viewport width should not rebuild syntax state for the same snapshot.");
        Assert.AreEqual(0, highlighter.UpdateCount, "Changing viewport width should only refresh wrapping, not syntax state.");
    }

    [TestMethod]
    public void CodeEditor_Caret_Move_Does_Not_Recompute_Stable_Syntax_Highlights()
    {
        var highlighter = new StableCountingSyntaxHighlighter();
        var editor = new CodeEditor("alpha\nbeta\ngamma\ndelta")
        {
            MinHeight = 4,
            MaxHeight = 4,
            SyntaxHighlighter = highlighter,
        };

        using var driver = new TerminalAppTestDriver(new VStack { editor }, TerminalHostKind.Fullscreen, new TerminalSize(20, 6));
        driver.Tick();

        var initialLineRequestCount = highlighter.LineRequestCount;
        var initialCacheCount = editor.GetCachedHighlightLineCountForTests();
        Assert.IsTrue(initialLineRequestCount > 0, "Expected the initial render to populate visible syntax-highlight runs.");

        driver.App.Focus(editor);
        driver.App.Post(() => editor.CaretIndex = 1);
        driver.TickUntil(() => editor.CaretIndex == 1);
        driver.Tick();

        Assert.AreEqual(
            initialLineRequestCount,
            highlighter.LineRequestCount,
            "Expected caret movement with a caret-invariant syntax highlighter to reuse cached visible-line runs instead of recomputing them on the UI thread.");
        Assert.AreEqual(
            initialCacheCount,
            editor.GetCachedHighlightLineCountForTests(),
            "Expected caret movement with a caret-invariant syntax highlighter to preserve the visible highlight cache.");
    }

    [TestMethod]
    public void CodeEditor_Async_SyntaxHighlighter_Discards_Stale_Build_Result()
    {
        var highlighter = new AsyncSyntaxHighlighter();
        var editor = new CodeEditor("one\ntwo")
        {
            MinHeight = 4,
            MaxHeight = 4,
            SyntaxHighlighter = highlighter,
        };

        using var driver = new TerminalAppTestDriver(new VStack { editor }, TerminalHostKind.Fullscreen, new TerminalSize(20, 6));
        driver.Tick();
        Assert.AreEqual(1, highlighter.BuildRequests.Count, "Expected the initial async syntax build to be scheduled.");

        driver.App.Focus(editor);
        driver.Backend.PushEvent(new TerminalTextEvent { Text = "X" });
        driver.TickUntil(() => editor.Text == "Xone\ntwo");
        driver.Tick();

        Assert.AreEqual(2, highlighter.BuildRequests.Count, "Expected editing while an async build is pending to schedule a replacement build.");

        highlighter.BuildRequests[0].Complete(new TestSyntaxState(0));
        driver.Tick();

        Assert.AreEqual(-1, editor.GetSyntaxStateSnapshotVersionForTests(), "Expected stale async syntax results to be discarded instead of being applied.");
        highlighter.BuildRequests[1].Complete(new TestSyntaxState(editor.TextDocument.CurrentSnapshot.Version));
        driver.TickUntil(() => editor.GetSyntaxStateSnapshotVersionForTests() == editor.TextDocument.CurrentSnapshot.Version);
        driver.Tick();
        Assert.IsTrue(highlighter.BuildRequests[0].IsCompleted, "Expected the stale request to have completed without being applied.");
        Assert.IsTrue(highlighter.BuildRequests[1].IsCompleted, "Expected the latest request to complete successfully.");

        Assert.AreEqual(editor.TextDocument.CurrentSnapshot.Version, editor.GetSyntaxStateSnapshotVersionForTests(), "Expected only the latest async syntax state to be retained.");
    }

    [TestMethod]
    public void CodeEditor_Async_SyntaxHighlighter_Uses_Update_After_Edit_And_Remains_Responsive()
    {
        var highlighter = new AsyncSyntaxHighlighter();
        var editor = new CodeEditor("alpha\nbeta\ngamma\ndelta")
        {
            MinHeight = 2,
            MaxHeight = 2,
            SyntaxHighlighter = highlighter,
        };

        var root = new VStack { editor };
        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(20, 6));
        driver.Tick();

        highlighter.BuildRequests[0].Complete(new TestSyntaxState(editor.TextDocument.CurrentSnapshot.Version));
        driver.TickUntil(() => editor.GetSyntaxStateSnapshotVersionForTests() == editor.TextDocument.CurrentSnapshot.Version);

        driver.App.Focus(editor);
        driver.Backend.PushEvent(new TerminalTextEvent { Text = "!" });
        driver.TickUntil(() => editor.Text == "!alpha\nbeta\ngamma\ndelta");
        driver.Tick();

        Assert.AreEqual(1, highlighter.UpdateRequests.Count, "Expected edits after an applied async state to use UpdateAsync.");

        driver.App.Post(() => editor.Scroll.SetOffset(0, 1));
        driver.Tick();
        Assert.AreEqual(1, editor.Scroll.OffsetY, "Expected scrolling to remain responsive while async syntax work is pending.");

        highlighter.UpdateRequests[0].Complete(new TestSyntaxState(editor.TextDocument.CurrentSnapshot.Version));
        driver.TickUntil(() => editor.GetSyntaxStateSnapshotVersionForTests() == editor.TextDocument.CurrentSnapshot.Version);

        Assert.AreEqual(1, highlighter.UpdateAsyncCount, "Expected exactly one async incremental update for the edit.");
    }

    [TestMethod]
    public void CodeEditor_Async_SyntaxHighlighter_Does_Not_Reuse_Stale_State_While_Update_Is_Pending()
    {
        var highlighter = new AsyncSyntaxHighlighter();
        var editor = new CodeEditor("alpha\nbeta")
        {
            MinHeight = 4,
            MaxHeight = 4,
            SyntaxHighlighter = highlighter,
        };

        using var driver = new TerminalAppTestDriver(new VStack { editor }, TerminalHostKind.Fullscreen, new TerminalSize(20, 6));
        driver.Tick();

        highlighter.BuildRequests[0].Complete(new TestSyntaxState(editor.TextDocument.CurrentSnapshot.Version));
        driver.TickUntil(() => editor.GetSyntaxStateSnapshotVersionForTests() == editor.TextDocument.CurrentSnapshot.Version);

        driver.App.Focus(editor);
        driver.App.Post(() => editor.CaretIndex = 0);
        driver.TickUntil(() => editor.CaretIndex == 0);

        var previousSnapshotVersion = editor.TextDocument.CurrentSnapshot.Version;
        var initialAppliedCount = highlighter.AppliedStateVersions.Count;

        driver.Backend.PushEvent(new TerminalTextEvent { Text = "!" });
        driver.TickUntil(() => editor.Text == "!alpha\nbeta");
        driver.Tick();

        Assert.AreEqual(1, highlighter.UpdateRequests.Count, "Expected the edit to queue an async syntax update.");
        if (highlighter.AppliedStateVersions.Count > initialAppliedCount)
        {
            Assert.AreNotEqual(
                previousSnapshotVersion,
                highlighter.AppliedStateVersions[^1],
                "Expected the editor not to keep painting with a stale syntax state from the previous snapshot while the async update is still pending.");
        }

        highlighter.UpdateRequests[0].Complete(new TestSyntaxState(editor.TextDocument.CurrentSnapshot.Version));
        driver.TickUntil(() => editor.GetSyntaxStateSnapshotVersionForTests() == editor.TextDocument.CurrentSnapshot.Version);
    }

    [TestMethod]
    public async Task TextMate_LargeDocument_Update_Preserves_Prepared_Visible_Line_Highlighting()
    {
        var text = string.Join('\n', Enumerable.Range(0, 220).Select(i => $"var value{i:0000} = \"text{i:0000}\";"));
        var highlighter = new TextMateCodeEditorSyntaxHighlighter(
            new TextMateCodeEditorOptions
            {
                LanguageId = "csharp",
                LargeDocumentLineThreshold = 1,
                LargeDocumentCharacterThreshold = 1,
                BackgroundTokenizationLineBudget = 1,
                CheckpointLineInterval = 64,
                SpeculativeLookBehindLineCount = 8,
                SpeculativeWindowLineCount = 24,
                SpeculativeCheckpointSearchLineCount = 32,
            });

        var theme = Theme.Default;
        var document = new TextDocument(text);
        var snapshot = document.CurrentSnapshot;
        var visibleLine = snapshot.LineCount - 2;
        var firstVisibleLine = Math.Max(0, visibleLine - 3);
        var lastVisibleLine = snapshot.LineCount - 1;

        var state = await highlighter.BuildAsync(new CodeEditorSyntaxBuildContext(snapshot, theme, 0, 0, 0));
        state = await highlighter.PrepareVisibleRangeAsync(
            state,
            new CodeEditorSyntaxVisibleRangeContext(snapshot, theme, firstVisibleLine, lastVisibleLine, 0, 0, 0));

        var initialRuns = new List<StyledRun>();
        var initialLine = snapshot.GetLine(visibleLine);
        highlighter.GetLineRuns(
            state,
            new CodeEditorLineSyntaxRequest(snapshot, theme, visibleLine, initialLine.Start, initialLine.Length, 0, 0, 0),
            initialRuns);
        Assert.IsTrue(initialRuns.Count > 0, "Expected the prepared visible range to provide non-empty TextMate runs before editing.");

        TextDocumentChangedEventArgs? change = null;
        document.Changed += (_, args) => change = args;
        document.Insert(document.CurrentSnapshot.Length, "\n");

        Assert.IsNotNull(change, "Expected the document edit to raise change metadata for the incremental TextMate update.");

        var updatedSnapshot = document.CurrentSnapshot;
        var startLine = updatedSnapshot.GetLineIndexFromPosition(Math.Clamp(change.Position, 0, updatedSnapshot.Length));
        var endPosition = Math.Min(updatedSnapshot.Length, change.Position + change.InsertedLength);
        var endLine = updatedSnapshot.GetLineIndexFromPosition(endPosition);

        var updatedState = await highlighter.UpdateAsync(
            state,
            new CodeEditorSyntaxUpdateContext(updatedSnapshot, theme, change, startLine, endLine, updatedSnapshot.Length, updatedSnapshot.Length, 0));

        var updatedRuns = new List<StyledRun>();
        var updatedLine = updatedSnapshot.GetLine(visibleLine);
        highlighter.GetLineRuns(
            updatedState,
            new CodeEditorLineSyntaxRequest(updatedSnapshot, theme, visibleLine, updatedLine.Start, updatedLine.Length, updatedSnapshot.Length, updatedSnapshot.Length, 0),
            updatedRuns);
        Assert.IsTrue(updatedRuns.Count > 0, "Expected the incremental TextMate update to preserve the already prepared visible-line highlighting instead of dropping back to white.");
    }

    [TestMethod]
    public void CodeEditor_Async_SyntaxHighlighter_Can_Apply_Synchronously_Completed_Update_During_Edit_Render()
    {
        var highlighter = new InlineCompletingAsyncSyntaxHighlighter();
        var editor = new CodeEditor("alphabeta")
        {
            MinHeight = 4,
            MaxHeight = 4,
            SyntaxHighlighter = highlighter,
        };

        using var driver = new TerminalAppTestDriver(new VStack { editor }, TerminalHostKind.Fullscreen, new TerminalSize(20, 6));
        driver.Tick();
        driver.App.Focus(editor);
        driver.App.Post(() => editor.CaretIndex = 5);
        driver.TickUntil(() => editor.CaretIndex == 5);

        Assert.AreEqual(editor.TextDocument.CurrentSnapshot.Version, editor.GetSyntaxStateSnapshotVersionForTests(), "Expected the initial completed async build to apply immediately.");

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Enter });
        driver.TickUntil(() => editor.Text == "alpha\nbeta");

        Assert.AreEqual(editor.TextDocument.CurrentSnapshot.Version, editor.GetSyntaxStateSnapshotVersionForTests(), "Expected a synchronously completed async update to apply during the edit render.");

        var runs = editor.GetHighlightRunsForTests(1);
        Assert.IsNotNull(runs, "Expected the newly inserted logical line to be highlighted without requiring extra navigation.");
        Assert.IsTrue(runs.Any(run => run.Start == 0 && run.Length > 0), "Expected the new logical line to receive highlight runs immediately after pressing Enter.");
    }

    [TestMethod]
    public void CodeEditor_Async_SyntaxHighlighter_Refreshes_Visible_Lines_When_Background_Update_Advances()
    {
        var highlighter = new ProgressiveAsyncSyntaxHighlighter();
        var editor = new CodeEditor("alpha\nbeta")
        {
            MinHeight = 4,
            MaxHeight = 4,
            SyntaxHighlighter = highlighter,
        };

        using var driver = new TerminalAppTestDriver(new VStack { editor }, TerminalHostKind.Fullscreen, new TerminalSize(20, 6));
        driver.Tick();

        highlighter.BuildRequests[0].Complete(new ProgressiveSyntaxState(editor.TextDocument.CurrentSnapshot.Version, phase: 1, isComplete: true));
        driver.TickUntil(() => editor.GetSyntaxStateSnapshotVersionForTests() == editor.TextDocument.CurrentSnapshot.Version);
        driver.TickUntil(() => editor.GetCachedHighlightLineCountForTests() > 0);
        var visibleLines = editor.GetVisibleLogicalLineIndicesForTests();
        Assert.IsTrue(visibleLines.Length > 0, "Expected the editor to report visible logical lines after the initial async build.");
        var observedLine = visibleLines[^1];
        var initialRuns = editor.GetHighlightRunsForTests(observedLine);
        Assert.IsNotNull(initialRuns, "Expected the visible line to be highlighted once the initial async build completes.");

        driver.App.Focus(editor);
        driver.App.Post(() => editor.CaretIndex = 0);
        driver.TickUntil(() => editor.CaretIndex == 0);
        driver.Backend.PushEvent(new TerminalTextEvent { Text = "!" });
        driver.TickUntil(() => editor.Text == "!alpha\nbeta");
        driver.Tick();

        Assert.AreEqual(1, highlighter.UpdateRequests.Count, "Expected the edit to queue the first async update.");
        highlighter.UpdateRequests[0].Complete(new ProgressiveSyntaxState(editor.TextDocument.CurrentSnapshot.Version, phase: 0, isComplete: false));
        driver.Tick();
        driver.TickUntil(() => highlighter.UpdateRequests.Count == 2);

        highlighter.UpdateRequests[1].Complete(new ProgressiveSyntaxState(editor.TextDocument.CurrentSnapshot.Version, phase: 2, isComplete: true));
        driver.TickUntil(() => editor.GetHighlightRunsForTests(observedLine) is { Length: > 0 } runs && !RunsEqual(initialRuns, runs));

        var updatedRuns = editor.GetHighlightRunsForTests(observedLine);
        Assert.IsNotNull(updatedRuns, "Expected the visible line cache to refresh after the follow-up async update completes.");
        Assert.IsFalse(RunsEqual(initialRuns, updatedRuns), "Expected the visible line highlighting to update without requiring extra navigation.");
    }

    [TestMethod]
    public void CodeEditor_Async_Progress_For_NonVisible_Lines_Does_Not_Rehighlight_Viewport()
    {
        var highlighter = new NonVisibleProgressAsyncSyntaxHighlighter();
        var text = string.Join("\n", Enumerable.Range(0, 220).Select(i => $"Line {i:000}"));
        var editor = new CodeEditor(text)
        {
            MinHeight = 4,
            MaxHeight = 4,
            SyntaxHighlighter = highlighter,
        };

        using var driver = new TerminalAppTestDriver(new VStack { editor }, TerminalHostKind.Fullscreen, new TerminalSize(24, 6));
        driver.Tick();

        var initialState = new ProgressiveCoverageSyntaxState(editor.TextDocument.CurrentSnapshot.Version, completedLineCount: 0, isComplete: false);
        highlighter.BuildRequests[0].Complete(initialState);
        driver.TickUntil(() => editor.GetSyntaxStateSnapshotVersionForTests() == editor.TextDocument.CurrentSnapshot.Version);

        driver.App.Post(() => editor.Scroll.SetOffset(0, 190));
        driver.Tick();
        driver.TickUntil(() => editor.GetCachedHighlightLineCountForTests() > 0);

        var initialLineRequestCount = highlighter.LineRequestCount;
        var initialCacheCount = editor.GetCachedHighlightLineCountForTests();

        driver.TickUntil(() => highlighter.UpdateRequests.Count == 1);
        initialState.CompletedLineCount = 64;
        highlighter.UpdateRequests[0].Complete(initialState);
        driver.Tick();

        Assert.AreEqual(
            initialLineRequestCount,
            highlighter.LineRequestCount,
            "Expected background syntax progress outside the viewport not to recompute visible-line highlights on the UI thread.");
        Assert.AreEqual(
            initialCacheCount,
            editor.GetCachedHighlightLineCountForTests(),
            "Expected the visible highlight cache to stay intact when async progress does not intersect the current viewport.");
    }

    [TestMethod]
    public void CodeEditor_Visible_Syntax_Preparation_Cancels_Stale_Viewport_Request()
    {
        var highlighter = new ViewportAsyncSyntaxHighlighter();
        var text = string.Join("\n", Enumerable.Range(0, 240).Select(i => $"Line {i:000}"));
        var editor = new CodeEditor(text)
        {
            MinHeight = 4,
            MaxHeight = 4,
            SyntaxHighlighter = highlighter,
        };

        using var driver = new TerminalAppTestDriver(new VStack { editor }, TerminalHostKind.Fullscreen, new TerminalSize(24, 6));
        driver.Tick();

        highlighter.BuildRequests[0].Complete(new ViewportSyntaxState(editor.TextDocument.CurrentSnapshot.Version));
        driver.TickUntil(() => editor.GetSyntaxStateSnapshotVersionForTests() == editor.TextDocument.CurrentSnapshot.Version);
        highlighter.VisibleRangeRequests.Clear();

        driver.App.Post(() => editor.Scroll.SetOffset(0, 100));
        driver.TickUntil(() => highlighter.VisibleRangeRequests.Count == 1);

        driver.App.Post(() => editor.Scroll.SetOffset(0, 150));
        driver.TickUntil(() => highlighter.VisibleRangeRequests.Count == 2);

        highlighter.VisibleRangeRequests[0].Complete(new ViewportSyntaxState(editor.TextDocument.CurrentSnapshot.Version, preparedFirstLine: 100, preparedLastLine: 103));
        driver.Tick();
        var staleRuns = editor.GetHighlightRunsForTests(150);
        Assert.IsTrue(staleRuns is null || staleRuns.Length == 0, "Expected a completed stale viewport-preparation result to be ignored after the user scrolls to a newer page.");

        highlighter.VisibleRangeRequests[1].Complete(new ViewportSyntaxState(editor.TextDocument.CurrentSnapshot.Version, preparedFirstLine: 150, preparedLastLine: 153));
        driver.TickUntil(() => editor.GetHighlightRunsForTests(150) is { Length: > 0 });

        var currentRuns = editor.GetHighlightRunsForTests(150);
        Assert.IsTrue(currentRuns is not null && currentRuns.Length > 0, "Expected the latest viewport-preparation request to populate highlighting for the current page.");
    }

    [TestMethod]
    public void CodeEditor_Visible_Syntax_Preparation_Refreshes_Viewport_When_Same_State_Gains_Visible_Coverage()
    {
        var highlighter = new ProgressiveViewportPreparationSyntaxHighlighter();
        var text = string.Join("\n", Enumerable.Range(0, 240).Select(i => $"Line {i:000}"));
        var editor = new CodeEditor(text)
        {
            MinHeight = 4,
            MaxHeight = 4,
            SyntaxHighlighter = highlighter,
        };

        using var driver = new TerminalAppTestDriver(new VStack { editor }, TerminalHostKind.Fullscreen, new TerminalSize(24, 6));
        driver.Tick();

        var initialState = new ProgressiveViewportSyntaxState(editor.TextDocument.CurrentSnapshot.Version, editor.TextDocument.CurrentSnapshot.LineCount, completedLineCount: 0);
        highlighter.BuildRequests[0].Complete(initialState);
        driver.TickUntil(() => editor.GetSyntaxStateSnapshotVersionForTests() == editor.TextDocument.CurrentSnapshot.Version);
        driver.TickUntil(() => highlighter.VisibleRangeRequests.Count == 1);
        highlighter.VisibleRangeRequests.Clear();

        driver.App.Post(() => editor.Scroll.SetOffset(0, 190));
        driver.TickUntil(() => highlighter.VisibleRangeRequests.Count == 1);

        var runsBeforePreparation = editor.GetHighlightRunsForTests(190);
        Assert.IsTrue(runsBeforePreparation is null || runsBeforePreparation.Length == 0, "Expected the far viewport to remain unhighlighted until visible-range preparation completes.");

        highlighter.VisibleRangeRequests[0].CompletePreparedRange();
        driver.TickUntil(() => editor.GetHighlightRunsForTests(190) is { Length: > 0 });

        var runsAfterPreparation = editor.GetHighlightRunsForTests(190);
        Assert.IsTrue(runsAfterPreparation is not null && runsAfterPreparation.Length > 0, "Expected visible-range preparation to populate instant highlighting even when it reuses and mutates the same syntax state instance.");
    }

    [TestMethod]
    public void CodeEditor_Clipboard_Cut_Copy_And_Paste_Work()
    {
        var editor = new CodeEditor();
        using var driver = new TerminalAppTestDriver(new VStack { editor }, TerminalHostKind.Fullscreen, new TerminalSize(24, 6));
        driver.Tick();
        driver.App.Focus(editor);

        driver.Backend.PushEvent(new TerminalTextEvent { Text = "a" });
        driver.Backend.PushEvent(new TerminalTextEvent { Text = "b" });
        driver.Backend.PushEvent(new TerminalTextEvent { Text = "c" });
        driver.TickUntil(() => editor.Text == "abc");

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Left, Modifiers = TerminalModifiers.Shift });
        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Unknown, Char = TerminalChar.CtrlC, Modifiers = TerminalModifiers.Ctrl });
        driver.Tick();
        Assert.AreEqual("c", driver.Terminal.Clipboard.Text);

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Unknown, Char = TerminalChar.CtrlX, Modifiers = TerminalModifiers.Ctrl });
        driver.TickUntil(() => editor.Text == "ab");

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Unknown, Char = TerminalChar.CtrlV, Modifiers = TerminalModifiers.Ctrl });
        driver.TickUntil(() => editor.Text == "abc");
    }

    [TestMethod]
    public void CodeEditor_Undo_And_Redo_Work()
    {
        var editor = new CodeEditor();
        editor.UndoManager.SetClockForTests(new ConstantClock());

        using var driver = new TerminalAppTestDriver(new VStack { editor }, TerminalHostKind.Fullscreen, new TerminalSize(24, 6));
        driver.Tick();
        driver.App.Focus(editor);

        driver.Backend.PushEvent(new TerminalTextEvent { Text = "a" });
        driver.Backend.PushEvent(new TerminalTextEvent { Text = "b" });
        driver.Backend.PushEvent(new TerminalTextEvent { Text = "c" });
        driver.TickUntil(() => editor.Text == "abc");

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Unknown, Char = TerminalChar.CtrlZ, Modifiers = TerminalModifiers.Ctrl });
        driver.TickUntil(() => editor.Text == string.Empty);

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Unknown, Char = TerminalChar.CtrlR, Modifiers = TerminalModifiers.Ctrl });
        driver.TickUntil(() => editor.Text == "abc");
    }

    [TestMethod]
    public void CodeEditor_GoToLine_Column_And_Position_MoveCaret_And_UpdateReadableLocation()
    {
        var text = string.Join("\n", Enumerable.Range(1, 120).Select(i => $"Line {i:000}"));
        var editor = new CodeEditor(text)
        {
            MinHeight = 5,
            MaxHeight = 5,
        };

        using var driver = new TerminalAppTestDriver(new VStack { editor }, TerminalHostKind.Fullscreen, new TerminalSize(24, 8));
        driver.Tick();
        driver.App.Focus(editor);

        driver.App.Post(() => editor.GoToLine(80));
        driver.TickUntil(() => editor.Line == 80);

        Assert.AreEqual(80, editor.Line, "Expected GoToLine to use one-based line numbers.");
        Assert.AreEqual(1, editor.Column, "Expected GoToLine(line) to move to the first column of the resolved line.");
        Assert.IsTrue(editor.Scroll.OffsetY > 0, "Expected GoToLine to scroll the caret into view.");

        driver.App.Post(() => editor.GoToColumn(6));
        driver.TickUntil(() => editor.Column == 6);

        Assert.AreEqual(80, editor.Line, "Expected GoToColumn to stay on the current caret line.");
        Assert.AreEqual(6, editor.Column, "Expected GoToColumn to use one-based columns.");

        var lineThree = editor.TextDocument.CurrentSnapshot.GetLine(2);
        driver.App.Post(() => editor.GoToLine(3, 6));
        driver.TickUntil(() => editor.Line == 3 && editor.Column == 6);

        Assert.AreEqual(lineThree.Start + 5, editor.CaretIndex, "Expected GoToLine(line, column) to resolve to the requested position.");

        var line120Position = text.IndexOf("Line 120", StringComparison.Ordinal) + 5;
        driver.App.Post(() => editor.GoToPosition(line120Position));
        driver.TickUntil(() => editor.Line == 120 && editor.Column == 6);

        Assert.AreEqual(120, editor.Line, "Expected GoToPosition(int) to move the caret to the matching line.");
        Assert.AreEqual(6, editor.Column, "Expected GoToPosition(int) to update the readable column.");

        driver.App.Post(() => editor.GoToPosition(new XenoAtom.Terminal.UI.Text.TextPosition(0)));
        driver.TickUntil(() => editor.Line == 1 && editor.Column == 1);

        Assert.AreEqual(1, editor.Line, "Expected GoToPosition(TextPosition) to support the typed overload.");
        Assert.AreEqual(1, editor.Column, "Expected GoToPosition(TextPosition) to move back to the document start.");
    }

    [TestMethod]
    public void CodeEditor_Line_And_Column_BindableProperties_Can_Drive_A_StatusBar()
    {
        var editor = new CodeEditor("alpha\nbeta")
        {
            MinHeight = 4,
            MaxHeight = 4,
        };

        var status = new TextBlock(() => $"Ln {editor.Line}, Col {editor.Column}");

        using var driver = new TerminalAppTestDriver(new VStack(editor, status), TerminalHostKind.Fullscreen, new TerminalSize(24, 8));
        driver.Tick();

        var initialScreen = new AnsiTestScreen(24, 8);
        initialScreen.Apply(driver.Backend.GetOutText());
        StringAssert.Contains(initialScreen.GetText(), "Ln 1, Col 1", "Expected the status bar binding to show the initial caret location.");

        driver.App.Post(() => editor.GoToLine(2, 3));
        driver.TickUntil(() => editor.Line == 2 && editor.Column == 3);

        var updatedScreen = new AnsiTestScreen(24, 8);
        updatedScreen.Apply(driver.Backend.GetOutText());
        StringAssert.Contains(updatedScreen.GetText(), "Ln 2, Col 3", "Expected the status bar binding to update when the caret moves.");
    }

    [TestMethod]
    public void CodeEditor_Line_And_Column_BindableProperties_Update_When_Navigating_With_Keyboard()
    {
        var editor = new CodeEditor("alpha\nbeta")
        {
            MinHeight = 4,
            MaxHeight = 4,
        };

        var status = new Footer()
            .Left(new TextBlock(() => $"Ln {editor.Line}, Col {editor.Column}"));

        using var driver = new TerminalAppTestDriver(new DockLayout().Content(editor).Bottom(status), TerminalHostKind.Fullscreen, new TerminalSize(24, 8));
        driver.Tick();
        driver.App.Focus(editor);

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Down });
        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Right });
        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Right });
        driver.TickUntil(() => editor.Line == 2 && editor.Column == 3);

        var screen = new AnsiTestScreen(24, 8);
        screen.Apply(driver.Backend.GetOutText());
        StringAssert.Contains(screen.GetText(), "Ln 2, Col 3", "Expected keyboard caret navigation to update the bound status footer.");
    }

    [TestMethod]
    public void CodeEditor_Line_And_Column_Computed_Status_Still_Updates_When_Built_Under_Suppressed_Tracking()
    {
        CodeEditor editor;
        Footer status;
        using (BindingManager.Current.SuppressReadTracking())
        using (BindingManager.Current.SuppressWriteTracking())
        {
            editor = new CodeEditor("alpha\nbeta")
            {
                MinHeight = 4,
                MaxHeight = 4,
            };

            status = new Footer()
                .Left(new TextBlock(() => $"Ln {editor.Line}, Col {editor.Column}"));
        }

        using var driver = new TerminalAppTestDriver(new DockLayout().Content(editor).Bottom(status), TerminalHostKind.Fullscreen, new TerminalSize(24, 8));
        driver.Tick();
        driver.App.Focus(editor);

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Down });
        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Right });
        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Right });
        driver.TickUntil(() => editor.Line == 2 && editor.Column == 3);

        var screen = new AnsiTestScreen(24, 8);
        screen.Apply(driver.Backend.GetOutText());
        StringAssert.Contains(screen.GetText(), "Ln 2, Col 3", "Expected computed status text to keep tracking editor.Line/editor.Column even when created under suppressed tracking.");
    }

    [TestMethod]
    public void CodeEditor_Line_And_Column_Can_Drive_A_StateBacked_Demo_Status_Footer_When_Navigating_With_Keyboard()
    {
        var editor = new CodeEditor("alpha\nbeta")
        {
            MinHeight = 4,
            MaxHeight = 4,
        };

        var caretLocationText = new State<string?>("Ln 1, Col 1");
        editor.Update(_ =>
        {
            caretLocationText.Value = $"Ln {editor.Line}, Col {editor.Column}";
        });

        var status = new Footer()
            .Left(new TextBlock(caretLocationText));

        using var driver = new TerminalAppTestDriver(new DockLayout().Content(editor).Bottom(status), TerminalHostKind.Fullscreen, new TerminalSize(24, 8));
        driver.Tick();
        driver.App.Focus(editor);

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Down });
        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Right });
        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Right });
        driver.TickUntil(() => editor.Line == 2 && editor.Column == 3);

        var screen = new AnsiTestScreen(24, 8);
        screen.Apply(driver.Backend.GetOutText());
        StringAssert.Contains(screen.GetText(), "Ln 2, Col 3", "Expected a demo-style state-backed footer to update from editor.Line/editor.Column during keyboard navigation.");
    }

    [TestMethod]
    public void CodeEditor_Cursor_Placement_Accounts_For_Margins()
    {
        var editor = new CodeEditor("hello") { CaretIndex = 2 };
        using var driver = new TerminalAppTestDriver(new VStack { editor }, TerminalHostKind.Fullscreen, new TerminalSize(20, 4));
        driver.Tick();

        Assert.IsTrue(editor.TryGetCursorCell(out var caretX, out var caretY), "Expected the caret to be visible.");

        var screen = new AnsiTestScreen(20, 4);
        screen.Apply(driver.Backend.GetOutText());
        var row = screen.GetText().Split('\n')[caretY];
        var textStart = row.IndexOf("hello", StringComparison.Ordinal);
        Assert.IsTrue(textStart >= 0, $"Expected the editor text to render on the caret row. Row: `{row}`");
        Assert.AreEqual(textStart + 2, caretX, "Expected the caret x position to include the gutter width.");
    }

    [TestMethod]
    public void CodeEditor_Horizontal_Scrolling_Works_When_WordWrap_Is_Disabled()
    {
        var text = "abcdefghijklmnopqrstuvwxyz";
        var editor = new CodeEditor(text)
        {
            MinHeight = 4,
            MaxHeight = 4,
            CaretIndex = text.Length,
        };
        editor.WordWrap = false;

        using var driver = new TerminalAppTestDriver(new VStack { editor }, TerminalHostKind.Fullscreen, new TerminalSize(14, 6));
        driver.TickUntil(() => editor.Scroll.OffsetX > 0);

        var screen = new AnsiTestScreen(14, 6);
        screen.Apply(driver.Backend.GetOutText());
        var row = screen.GetText().Split('\n')[0];

        Assert.IsTrue(row.StartsWith("  1", StringComparison.Ordinal), $"Expected the line number gutter to stay fixed while horizontally scrolling. Row: `{row}`");
        StringAssert.Contains(row, "xyz", "Expected the scrolled viewport to show the tail of the long line.");
    }

    [TestMethod]
    public void CodeEditor_ScrollViewer_ScrollBar_Click_Jumps_To_Clicked_Position()
    {
        var editor = new CodeEditor(string.Join('\n', Enumerable.Range(0, 60).Select(i => $"Line {i:00}")))
        {
            MinHeight = 8,
            MaxHeight = 8,
        };

        var scrollViewer = new ScrollViewer(editor)
        {
            MinHeight = 8,
            MaxHeight = 8,
        };

        using var driver = new TerminalAppTestDriver(scrollViewer, TerminalHostKind.Fullscreen, new TerminalSize(40, 10));
        driver.Tick();

        var verticalBar = scrollViewer.EnumerateVisualsDepthFirst().OfType<VScrollBar>().Single();
        Assert.IsTrue(verticalBar.IsVisible, "Expected the wrapped CodeEditor to expose a vertical scrollbar.");

        var barX = verticalBar.Bounds.X;
        var barBottom = verticalBar.Bounds.Bottom - 1;

        driver.Backend.PushEvent(new TerminalMouseEvent
        {
            Kind = TerminalMouseKind.Down,
            Button = TerminalMouseButton.Left,
            X = barX,
            Y = barBottom,
        });
        driver.Backend.PushEvent(new TerminalMouseEvent
        {
            Kind = TerminalMouseKind.Up,
            Button = TerminalMouseButton.Left,
            X = barX,
            Y = barBottom,
        });
        driver.TickUntil(() => editor.Scroll.OffsetY == editor.Scroll.ExtentHeight - editor.Scroll.ViewportHeight);

        Assert.AreEqual(editor.Scroll.ExtentHeight - editor.Scroll.ViewportHeight, editor.Scroll.OffsetY, "Clicking the lower end of the scrollbar track should move the editor to the bottom.");

        driver.Backend.PushEvent(new TerminalMouseEvent
        {
            Kind = TerminalMouseKind.Down,
            Button = TerminalMouseButton.Left,
            X = barX,
            Y = verticalBar.Bounds.Y,
        });
        driver.Backend.PushEvent(new TerminalMouseEvent
        {
            Kind = TerminalMouseKind.Up,
            Button = TerminalMouseButton.Left,
            X = barX,
            Y = verticalBar.Bounds.Y,
        });
        driver.TickUntil(() => editor.Scroll.OffsetY == 0);

        Assert.AreEqual(0, editor.Scroll.OffsetY, "Clicking the upper end of the scrollbar track should move the editor back to the top.");
    }

    [TestMethod]
    public void CodeEditor_ScrollViewer_ScrollBar_Drag_Reaches_Full_Range_With_Adaptive_Gutter()
    {
        var longLine = "public static void RenderCurrentLineBackground(CellBuffer buffer, Theme theme, CodeEditorStyle style, bool focused)";
        var text = string.Join('\n', Enumerable.Range(0, 1800).Select(i => $"{longLine} // {i:0000}"));
        var editor = new CodeEditor(text)
        {
            MinHeight = 8,
            MaxHeight = 8,
        };

        var scrollViewer = new ScrollViewer(editor)
        {
            MinHeight = 8,
            MaxHeight = 8,
            MaxWidth = 30,
        };

        using var driver = new TerminalAppTestDriver(scrollViewer, TerminalHostKind.Fullscreen, new TerminalSize(30, 10));
        driver.Tick();

        var verticalBar = scrollViewer.EnumerateVisualsDepthFirst().OfType<VScrollBar>().Single();
        Assert.IsTrue(verticalBar.IsVisible, "Expected the wrapped CodeEditor to expose a vertical scrollbar.");

        var barX = verticalBar.Bounds.X;
        var barTop = verticalBar.Bounds.Y;
        var barBottom = verticalBar.Bounds.Bottom - 1;
        Assert.AreEqual(nameof(VScrollBar), scrollViewer.HitTest(barX, barTop)?.GetType().Name, "Expected the drag to start on the scrollbar thumb.");

        driver.Backend.PushEvent(new TerminalMouseEvent
        {
            Kind = TerminalMouseKind.Down,
            Button = TerminalMouseButton.Left,
            X = barX,
            Y = barTop,
        });
        driver.Tick();

        driver.Backend.PushEvent(new TerminalMouseEvent
        {
            Kind = TerminalMouseKind.Drag,
            Button = TerminalMouseButton.Left,
            X = barX,
            Y = barBottom,
        });
        driver.Backend.PushEvent(new TerminalMouseEvent
        {
            Kind = TerminalMouseKind.Up,
            Button = TerminalMouseButton.Left,
            X = barX,
            Y = barBottom,
        });
        var expectedBottomOffset = -1;
        for (var i = 0; i < 50; i++)
        {
            driver.Tick();
            expectedBottomOffset = editor.Scroll.ExtentHeight - editor.Scroll.ViewportHeight;
            if (editor.Scroll.OffsetY == expectedBottomOffset)
            {
                break;
            }
        }

        Assert.AreEqual(
            expectedBottomOffset,
            editor.Scroll.OffsetY,
            $"Dragging the CodeEditor scrollbar to the bottom should reach the full scroll range even when the gutter width changes. actual={editor.Scroll.OffsetY}, expected={expectedBottomOffset}, extent={editor.Scroll.ExtentHeight}, viewport={editor.Scroll.ViewportHeight}, barValue={verticalBar.Value}, barMax={verticalBar.Maximum}, barViewport={verticalBar.ViewportSize}");

        driver.Backend.PushEvent(new TerminalMouseEvent
        {
            Kind = TerminalMouseKind.Down,
            Button = TerminalMouseButton.Left,
            X = barX,
            Y = barBottom,
        });
        driver.Tick();

        driver.Backend.PushEvent(new TerminalMouseEvent
        {
            Kind = TerminalMouseKind.Drag,
            Button = TerminalMouseButton.Left,
            X = barX,
            Y = barTop,
        });
        driver.Backend.PushEvent(new TerminalMouseEvent
        {
            Kind = TerminalMouseKind.Up,
            Button = TerminalMouseButton.Left,
            X = barX,
            Y = barTop,
        });
        driver.TickUntil(() => editor.Scroll.OffsetY == 0);

        Assert.AreEqual(0, editor.Scroll.OffsetY, "Dragging the CodeEditor scrollbar back to the top should restore the initial position.");
    }

    private static Style GetCellStyle(CellBuffer buffer, int x, int y)
    {
        var cells = buffer.UnsafeCells;
        return cells[(y * buffer.Width) + x];
    }

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

    private sealed class TestRightMargin : CodeEditorMargin
    {
        public List<int> RenderedLineIndices { get; } = new();

        public override CodeEditorMarginSide Side => CodeEditorMarginSide.Right;

        public override int MeasureWidth(in CodeEditorMarginMeasureContext context)
        {
            _ = context;
            return 1;
        }

        public override void Render(in CodeEditorMarginRenderContext context)
        {
            RenderedLineIndices.Clear();
            for (var i = 0; i < context.VisibleLines.Count; i++)
            {
                RenderedLineIndices.Add(context.VisibleLines[i].LineIndex);
            }
        }
    }

    private class CountingSyntaxHighlighter : CodeEditorSyntaxHighlighter
    {
        public int BuildCount { get; private set; }

        public int UpdateCount { get; private set; }

        public int LineRequestCount { get; private set; }

        public override CodeEditorSyntaxState Build(in CodeEditorSyntaxBuildContext context)
        {
            BuildCount++;
            return new TestSyntaxState(context.Snapshot.Version);
        }

        public override CodeEditorSyntaxState Update(CodeEditorSyntaxState previousState, in CodeEditorSyntaxUpdateContext context)
        {
            _ = previousState;
            UpdateCount++;
            return new TestSyntaxState(context.Snapshot.Version);
        }

        public override void GetLineRuns(CodeEditorSyntaxState state, in CodeEditorLineSyntaxRequest request, List<StyledRun> runs)
        {
            _ = state;
            LineRequestCount++;
            if (request.LineLength > 0)
            {
                runs.Add(new StyledRun(0, Math.Min(4, request.LineLength), Style.None.WithForeground(Color.Basic16(2))));
            }
        }
    }

    private static Rectangle GetEditorRect(CodeEditor editor)
        => (Rectangle)typeof(CodeEditor).GetField("_editorRect", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(editor)!;

    private static void SendCtrlGesture(TerminalAppTestDriver driver, char gesture)
        => driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Unknown, Char = gesture, Modifiers = TerminalModifiers.Ctrl });

    private sealed class StableCountingSyntaxHighlighter : CountingSyntaxHighlighter
    {
        public override bool DependsOnCaretOrSelection => false;
    }

    private sealed class TestSyntaxState : CodeEditorSyntaxState
    {
        public TestSyntaxState(int snapshotVersion)
        {
            SnapshotVersion = snapshotVersion;
        }

        public override int SnapshotVersion { get; }
    }

    private sealed class AsyncSyntaxHighlighter : CodeEditorSyntaxHighlighter, IAsyncCodeEditorSyntaxHighlighter
    {
        public List<PendingRequest> BuildRequests { get; } = new();

        public List<PendingRequest> UpdateRequests { get; } = new();

        public List<int> AppliedStateVersions { get; } = new();

        public int BuildAsyncCount { get; private set; }

        public int UpdateAsyncCount { get; private set; }

        public int LineRequestCount { get; private set; }

        public int LastAppliedStateVersion { get; private set; } = -1;

        public override bool DependsOnCaretOrSelection => false;

        public override CodeEditorSyntaxState Build(in CodeEditorSyntaxBuildContext context) => new TestSyntaxState(context.Snapshot.Version);

        public override CodeEditorSyntaxState Update(CodeEditorSyntaxState previousState, in CodeEditorSyntaxUpdateContext context)
        {
            _ = previousState;
            return new TestSyntaxState(context.Snapshot.Version);
        }

        public override void GetLineRuns(CodeEditorSyntaxState state, in CodeEditorLineSyntaxRequest request, List<StyledRun> runs)
        {
            _ = request;
            LineRequestCount++;
            LastAppliedStateVersion = state.SnapshotVersion;
            AppliedStateVersions.Add(state.SnapshotVersion);
            if (request.LineLength > 0)
            {
                runs.Add(new StyledRun(0, Math.Min(3, request.LineLength), Style.None.WithForeground(Color.Basic16(5))));
            }
        }

        public ValueTask<CodeEditorSyntaxState> BuildAsync(in CodeEditorSyntaxBuildContext context, CancellationToken cancellationToken = default)
        {
            BuildAsyncCount++;
            var request = new PendingRequest(context.Snapshot.Version);
            BuildRequests.Add(request);
            return new ValueTask<CodeEditorSyntaxState>(request.Task);
        }

        public ValueTask<CodeEditorSyntaxState> UpdateAsync(CodeEditorSyntaxState previousState, in CodeEditorSyntaxUpdateContext context, CancellationToken cancellationToken = default)
        {
            _ = previousState;
            UpdateAsyncCount++;
            var request = new PendingRequest(context.Snapshot.Version);
            UpdateRequests.Add(request);
            return new ValueTask<CodeEditorSyntaxState>(request.Task);
        }
    }

    private sealed class InlineCompletingAsyncSyntaxHighlighter : CodeEditorSyntaxHighlighter, IAsyncCodeEditorSyntaxHighlighter
    {
        public override CodeEditorSyntaxState Build(in CodeEditorSyntaxBuildContext context)
            => new TestSyntaxState(context.Snapshot.Version);

        public override CodeEditorSyntaxState Update(CodeEditorSyntaxState previousState, in CodeEditorSyntaxUpdateContext context)
        {
            _ = previousState;
            return new TestSyntaxState(context.Snapshot.Version);
        }

        public override void GetLineRuns(CodeEditorSyntaxState state, in CodeEditorLineSyntaxRequest request, List<StyledRun> runs)
        {
            if (state is not InlineSyntaxState inlineState || request.LineLength <= 0 || request.LineIndex >= inlineState.LineCount)
            {
                return;
            }

            runs.Add(new StyledRun(0, Math.Min(4, request.LineLength), Style.None.WithForeground(Color.Basic16(6))));
        }

        public ValueTask<CodeEditorSyntaxState> BuildAsync(in CodeEditorSyntaxBuildContext context, CancellationToken cancellationToken = default)
            => new(new InlineSyntaxState(context.Snapshot.Version, context.Snapshot.LineCount));

        public ValueTask<CodeEditorSyntaxState> UpdateAsync(CodeEditorSyntaxState previousState, in CodeEditorSyntaxUpdateContext context, CancellationToken cancellationToken = default)
        {
            _ = previousState;
            return new(new InlineSyntaxState(context.Snapshot.Version, context.Snapshot.LineCount));
        }
    }

    private sealed class InlineSyntaxState : CodeEditorSyntaxState
    {
        public InlineSyntaxState(int snapshotVersion, int lineCount)
        {
            SnapshotVersion = snapshotVersion;
            LineCount = lineCount;
        }

        public override int SnapshotVersion { get; }

        public int LineCount { get; }
    }

    private sealed class ProgressiveAsyncSyntaxHighlighter : CodeEditorSyntaxHighlighter, IAsyncCodeEditorSyntaxHighlighter
    {
        public List<PendingRequest> BuildRequests { get; } = new();

        public List<PendingRequest> UpdateRequests { get; } = new();

        public override CodeEditorSyntaxState Build(in CodeEditorSyntaxBuildContext context)
            => new ProgressiveSyntaxState(context.Snapshot.Version, phase: 1, isComplete: true);

        public override CodeEditorSyntaxState Update(CodeEditorSyntaxState previousState, in CodeEditorSyntaxUpdateContext context)
        {
            _ = previousState;
            return new ProgressiveSyntaxState(context.Snapshot.Version, phase: 2, isComplete: true);
        }

        public override void GetLineRuns(CodeEditorSyntaxState state, in CodeEditorLineSyntaxRequest request, List<StyledRun> runs)
        {
            if (state is not ProgressiveSyntaxState progressiveState || request.LineLength <= 0)
            {
                return;
            }

            if (progressiveState.Phase <= 0)
            {
                return;
            }

            var color = progressiveState.Phase == 1 ? Color.Basic16(2) : Color.Basic16(4);
            runs.Add(new StyledRun(0, Math.Min(4, request.LineLength), Style.None.WithForeground(color)));
        }

        public ValueTask<CodeEditorSyntaxState> BuildAsync(in CodeEditorSyntaxBuildContext context, CancellationToken cancellationToken = default)
        {
            var request = new PendingRequest(context.Snapshot.Version);
            BuildRequests.Add(request);
            return new ValueTask<CodeEditorSyntaxState>(request.Task);
        }

        public ValueTask<CodeEditorSyntaxState> UpdateAsync(CodeEditorSyntaxState previousState, in CodeEditorSyntaxUpdateContext context, CancellationToken cancellationToken = default)
        {
            _ = previousState;
            var request = new PendingRequest(context.Snapshot.Version);
            UpdateRequests.Add(request);
            return new ValueTask<CodeEditorSyntaxState>(request.Task);
        }
    }

    private sealed class ProgressiveSyntaxState : CodeEditorSyntaxState
    {
        public ProgressiveSyntaxState(int snapshotVersion, int phase, bool isComplete)
        {
            SnapshotVersion = snapshotVersion;
            Phase = phase;
            IsComplete = isComplete;
        }

        public override int SnapshotVersion { get; }

        public int Phase { get; }

        public override bool IsComplete { get; }
    }

    private sealed class NonVisibleProgressAsyncSyntaxHighlighter : CodeEditorSyntaxHighlighter, IAsyncCodeEditorSyntaxHighlighter
    {
        public List<PendingRequest> BuildRequests { get; } = new();

        public List<PendingRequest> UpdateRequests { get; } = new();

        public int LineRequestCount { get; private set; }

        public override bool DependsOnCaretOrSelection => false;

        public override CodeEditorSyntaxState Build(in CodeEditorSyntaxBuildContext context)
            => new ProgressiveCoverageSyntaxState(context.Snapshot.Version, completedLineCount: context.Snapshot.LineCount, isComplete: true);

        public override CodeEditorSyntaxState Update(CodeEditorSyntaxState previousState, in CodeEditorSyntaxUpdateContext context)
        {
            _ = previousState;
            return new ProgressiveCoverageSyntaxState(context.Snapshot.Version, completedLineCount: context.Snapshot.LineCount, isComplete: true);
        }

        public override void GetLineRuns(CodeEditorSyntaxState state, in CodeEditorLineSyntaxRequest request, List<StyledRun> runs)
        {
            _ = state;
            LineRequestCount++;
            if (request.LineLength > 0)
            {
                runs.Add(new StyledRun(0, Math.Min(4, request.LineLength), Style.None.WithForeground(Color.Basic16(6))));
            }
        }

        public ValueTask<CodeEditorSyntaxState> BuildAsync(in CodeEditorSyntaxBuildContext context, CancellationToken cancellationToken = default)
        {
            var request = new PendingRequest(context.Snapshot.Version);
            BuildRequests.Add(request);
            return new ValueTask<CodeEditorSyntaxState>(request.Task);
        }

        public ValueTask<CodeEditorSyntaxState> UpdateAsync(CodeEditorSyntaxState previousState, in CodeEditorSyntaxUpdateContext context, CancellationToken cancellationToken = default)
        {
            _ = previousState;
            var request = new PendingRequest(context.Snapshot.Version);
            UpdateRequests.Add(request);
            return new ValueTask<CodeEditorSyntaxState>(request.Task);
        }
    }

    private sealed class ProgressiveCoverageSyntaxState : CodeEditorSyntaxState, IProgressiveCodeEditorSyntaxState
    {
        private readonly bool _isComplete;

        public ProgressiveCoverageSyntaxState(int snapshotVersion, int completedLineCount, bool isComplete)
        {
            SnapshotVersion = snapshotVersion;
            CompletedLineCount = completedLineCount;
            _isComplete = isComplete;
        }

        public override int SnapshotVersion { get; }

        public int CompletedLineCount { get; set; }

        public override bool IsComplete => _isComplete;
    }

    private sealed class ViewportAsyncSyntaxHighlighter : CodeEditorSyntaxHighlighter, IAsyncCodeEditorSyntaxHighlighter
    {
        public List<PendingRequest> BuildRequests { get; } = new();

        public List<PendingVisibleRangeRequest> VisibleRangeRequests { get; } = new();

        public override bool DependsOnCaretOrSelection => false;

        public override CodeEditorSyntaxState Build(in CodeEditorSyntaxBuildContext context)
            => new ViewportSyntaxState(context.Snapshot.Version);

        public override CodeEditorSyntaxState Update(CodeEditorSyntaxState previousState, in CodeEditorSyntaxUpdateContext context)
        {
            _ = previousState;
            return new ViewportSyntaxState(context.Snapshot.Version);
        }

        public override void GetLineRuns(CodeEditorSyntaxState state, in CodeEditorLineSyntaxRequest request, List<StyledRun> runs)
        {
            if (state is not ViewportSyntaxState viewportState || request.LineLength <= 0)
            {
                return;
            }

            if (request.LineIndex < viewportState.PreparedFirstLine || request.LineIndex > viewportState.PreparedLastLine)
            {
                return;
            }

            runs.Add(new StyledRun(0, Math.Min(4, request.LineLength), Style.None.WithForeground(Color.Basic16(3))));
        }

        public ValueTask<CodeEditorSyntaxState> BuildAsync(in CodeEditorSyntaxBuildContext context, CancellationToken cancellationToken = default)
        {
            var request = new PendingRequest(context.Snapshot.Version);
            BuildRequests.Add(request);
            return new ValueTask<CodeEditorSyntaxState>(request.Task);
        }

        public ValueTask<CodeEditorSyntaxState> UpdateAsync(CodeEditorSyntaxState previousState, in CodeEditorSyntaxUpdateContext context, CancellationToken cancellationToken = default)
        {
            _ = previousState;
            return new ValueTask<CodeEditorSyntaxState>(new ViewportSyntaxState(context.Snapshot.Version));
        }

        public ValueTask<CodeEditorSyntaxState> PrepareVisibleRangeAsync(CodeEditorSyntaxState state, in CodeEditorSyntaxVisibleRangeContext context, CancellationToken cancellationToken = default)
        {
            _ = state;
            var request = new PendingVisibleRangeRequest(context.Snapshot.Version, context.FirstVisibleLineIndex, context.LastVisibleLineIndex);
            VisibleRangeRequests.Add(request);
            return new ValueTask<CodeEditorSyntaxState>(request.Task);
        }
    }

    private sealed class ViewportSyntaxState : CodeEditorSyntaxState, ICodeEditorSyntaxCoverageState
    {
        public ViewportSyntaxState(int snapshotVersion, int preparedFirstLine = -1, int preparedLastLine = -1)
        {
            SnapshotVersion = snapshotVersion;
            PreparedFirstLine = preparedFirstLine;
            PreparedLastLine = preparedLastLine;
        }

        public override int SnapshotVersion { get; }

        public int PreparedFirstLine { get; }

        public int PreparedLastLine { get; }

        public bool HasLineCoverage(int lineIndex)
            => lineIndex >= PreparedFirstLine && lineIndex <= PreparedLastLine;
    }

    private sealed class ProgressiveViewportPreparationSyntaxHighlighter : CodeEditorSyntaxHighlighter, IAsyncCodeEditorSyntaxHighlighter
    {
        public List<PendingRequest> BuildRequests { get; } = new();

        public List<PendingPreparedViewportRequest> VisibleRangeRequests { get; } = new();

        public override bool DependsOnCaretOrSelection => false;

        public override CodeEditorSyntaxState Build(in CodeEditorSyntaxBuildContext context)
            => new ProgressiveViewportSyntaxState(context.Snapshot.Version, context.Snapshot.LineCount, context.Snapshot.LineCount);

        public override CodeEditorSyntaxState Update(CodeEditorSyntaxState previousState, in CodeEditorSyntaxUpdateContext context)
        {
            _ = previousState;
            return new ProgressiveViewportSyntaxState(context.Snapshot.Version, context.Snapshot.LineCount, context.Snapshot.LineCount);
        }

        public override void GetLineRuns(CodeEditorSyntaxState state, in CodeEditorLineSyntaxRequest request, List<StyledRun> runs)
        {
            if (state is not ProgressiveViewportSyntaxState viewportState || request.LineLength <= 0)
            {
                return;
            }

            if (!viewportState.HasLineCoverage(request.LineIndex))
            {
                return;
            }

            runs.Add(new StyledRun(0, Math.Min(4, request.LineLength), Style.None.WithForeground(Color.Basic16(5))));
        }

        public ValueTask<CodeEditorSyntaxState> BuildAsync(in CodeEditorSyntaxBuildContext context, CancellationToken cancellationToken = default)
        {
            var request = new PendingRequest(context.Snapshot.Version);
            BuildRequests.Add(request);
            return new ValueTask<CodeEditorSyntaxState>(request.Task);
        }

        public ValueTask<CodeEditorSyntaxState> UpdateAsync(CodeEditorSyntaxState previousState, in CodeEditorSyntaxUpdateContext context, CancellationToken cancellationToken = default)
        {
            _ = previousState;
            return new ValueTask<CodeEditorSyntaxState>(new ProgressiveViewportSyntaxState(context.Snapshot.Version, context.Snapshot.LineCount, context.Snapshot.LineCount));
        }

        public ValueTask<CodeEditorSyntaxState> PrepareVisibleRangeAsync(CodeEditorSyntaxState state, in CodeEditorSyntaxVisibleRangeContext context, CancellationToken cancellationToken = default)
        {
            var viewportState = AssertState(state, context.Snapshot.Version);
            var request = new PendingPreparedViewportRequest(viewportState, context.FirstVisibleLineIndex, context.LastVisibleLineIndex);
            VisibleRangeRequests.Add(request);
            return new ValueTask<CodeEditorSyntaxState>(request.Task);
        }

        private static ProgressiveViewportSyntaxState AssertState(CodeEditorSyntaxState state, int snapshotVersion)
        {
            Assert.IsInstanceOfType<ProgressiveViewportSyntaxState>(state);
            var viewportState = (ProgressiveViewportSyntaxState)state;
            Assert.AreEqual(snapshotVersion, viewportState.SnapshotVersion, "Expected visible-range preparation to keep operating on the current snapshot state.");
            return viewportState;
        }
    }

    private sealed class ProgressiveViewportSyntaxState : CodeEditorSyntaxState, IProgressiveCodeEditorSyntaxState, ICodeEditorSyntaxCoverageState
    {
        private readonly HashSet<int> _preparedLines;

        public ProgressiveViewportSyntaxState(int snapshotVersion, int lineCount, int completedLineCount)
        {
            SnapshotVersion = snapshotVersion;
            LineCount = lineCount;
            CompletedLineCount = completedLineCount;
            _preparedLines = new HashSet<int>();
        }

        public override int SnapshotVersion { get; }

        public int LineCount { get; }

        public int CompletedLineCount { get; set; }

        public void PrepareRange(int firstLineIndex, int lastLineIndex)
        {
            var start = Math.Clamp(firstLineIndex, 0, Math.Max(0, LineCount - 1));
            var end = Math.Clamp(lastLineIndex, start, Math.Max(0, LineCount - 1));
            for (var lineIndex = start; lineIndex <= end; lineIndex++)
            {
                _preparedLines.Add(lineIndex);
            }
        }

        public bool HasLineCoverage(int lineIndex)
            => lineIndex >= 0
                && lineIndex < LineCount
                && (lineIndex < CompletedLineCount || _preparedLines.Contains(lineIndex));
    }

    private sealed class PendingRequest
    {
        private readonly TaskCompletionSource<CodeEditorSyntaxState> _tcs = new();

        public PendingRequest(int snapshotVersion)
        {
            SnapshotVersion = snapshotVersion;
        }

        public int SnapshotVersion { get; }

        public Task<CodeEditorSyntaxState> Task => _tcs.Task;

        public bool IsCompleted => _tcs.Task.IsCompleted;

        public void Complete(CodeEditorSyntaxState state) => _tcs.TrySetResult(state);
    }

    private sealed class PendingVisibleRangeRequest
    {
        private readonly TaskCompletionSource<CodeEditorSyntaxState> _tcs = new();

        public PendingVisibleRangeRequest(int snapshotVersion, int firstVisibleLineIndex, int lastVisibleLineIndex)
        {
            SnapshotVersion = snapshotVersion;
            FirstVisibleLineIndex = firstVisibleLineIndex;
            LastVisibleLineIndex = lastVisibleLineIndex;
        }

        public int SnapshotVersion { get; }

        public int FirstVisibleLineIndex { get; }

        public int LastVisibleLineIndex { get; }

        public Task<CodeEditorSyntaxState> Task => _tcs.Task;

        public void Complete(CodeEditorSyntaxState state) => _tcs.TrySetResult(state);
    }

    private sealed class PendingPreparedViewportRequest
    {
        private readonly TaskCompletionSource<CodeEditorSyntaxState> _tcs = new();
        private readonly ProgressiveViewportSyntaxState _state;

        public PendingPreparedViewportRequest(ProgressiveViewportSyntaxState state, int firstVisibleLineIndex, int lastVisibleLineIndex)
        {
            _state = state;
            FirstVisibleLineIndex = firstVisibleLineIndex;
            LastVisibleLineIndex = lastVisibleLineIndex;
        }

        public int FirstVisibleLineIndex { get; }

        public int LastVisibleLineIndex { get; }

        public Task<CodeEditorSyntaxState> Task => _tcs.Task;

        public void CompletePreparedRange()
        {
            _state.PrepareRange(FirstVisibleLineIndex, LastVisibleLineIndex);
            _tcs.TrySetResult(_state);
        }
    }

    private static bool RunsEqual(StyledRun[]? left, StyledRun[]? right)
    {
        if (ReferenceEquals(left, right))
        {
            return true;
        }

        if (left is null || right is null || left.Length != right.Length)
        {
            return false;
        }

        for (var i = 0; i < left.Length; i++)
        {
            if (left[i] != right[i])
            {
                return false;
            }
        }

        return true;
    }

    private sealed class ConstantClock : TextUndoRedoManager.IUndoClock
    {
        public int NowMilliseconds => 0;
    }
}
