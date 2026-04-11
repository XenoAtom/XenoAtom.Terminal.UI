// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using XenoAtom.Terminal;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Hosting;
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

    private sealed class CountingSyntaxHighlighter : CodeEditorSyntaxHighlighter
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

        public int BuildAsyncCount { get; private set; }

        public int UpdateAsyncCount { get; private set; }

        public int LineRequestCount { get; private set; }

        public int LastAppliedStateVersion { get; private set; } = -1;

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

    private sealed class ConstantClock : TextUndoRedoManager.IUndoClock
    {
        public int NowMilliseconds => 0;
    }
}
