// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Linq;
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
}
