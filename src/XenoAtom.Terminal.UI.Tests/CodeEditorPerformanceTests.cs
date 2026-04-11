// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Linq;
using XenoAtom.Terminal;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Hosting;
using XenoAtom.Terminal.UI.Text;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class CodeEditorPerformanceTests
{
    [TestMethod]
    public void CodeEditor_DeepScroll_InHugeDocument_OnlyReads_Visible_Lines()
    {
        var text = string.Join('\n', Enumerable.Range(0, 100_000).Select(i => $"Line {i:000000}"));
        var document = new CountingTextDocument(text);
        var editor = new CodeEditor
        {
            TextDocument = document,
            MinHeight = 8,
            MaxHeight = 8,
        };

        using var driver = new TerminalAppTestDriver(new VStack { editor }, TerminalHostKind.Fullscreen, new TerminalSize(36, 10));
        driver.Tick();

        document.ResetCounters();
        editor.Scroll.SetOffset(0, 95_000);
        driver.Tick();

        Assert.IsLessThan(96, document.GetLineCallCount, $"Expected deep-scroll rendering to touch only visible lines. Actual line reads: {document.GetLineCallCount}.");
    }

    [TestMethod]
    public void CodeEditor_DeepScroll_Only_Rehighlights_Visible_Lines()
    {
        var requestedLines = new List<int>();
        var editor = new CodeEditor(string.Join('\n', Enumerable.Range(0, 20_000).Select(i => $"Line {i:000000}")))
        {
            MinHeight = 8,
            MaxHeight = 8,
        };
        editor.Highlighter((in CodeEditorLineHighlightRequest request, List<StyledRun> runs) =>
        {
            requestedLines.Add(request.LineIndex);
            if (request.LineLength > 0)
            {
                runs.Add(new StyledRun(0, Math.Min(4, request.LineLength), Style.None.WithForeground(Color.Basic16(2))));
            }
        });

        using var driver = new TerminalAppTestDriver(new VStack { editor }, TerminalHostKind.Fullscreen, new TerminalSize(36, 10));
        driver.Tick();

        requestedLines.Clear();
        editor.Scroll.SetOffset(0, 12_345);
        driver.Tick();

        var uniqueLines = requestedLines.Distinct().ToArray();
        Assert.IsLessThanOrEqualTo(editor.Scroll.ViewportHeight + 1, uniqueLines.Length, $"Expected deep scrolling to re-highlight only the visible logical lines. Actual lines: {string.Join(", ", uniqueLines)}.");
        Assert.IsLessThanOrEqualTo(editor.Scroll.ViewportHeight + 1, editor.GetCachedHighlightLineCountForTests(), "Expected the highlight cache to stay bounded to the viewport.");
    }

    [TestMethod]
    public void CodeEditor_AdvancedSyntaxHighlighting_DeepScroll_Only_Requests_Visible_Lines()
    {
        var highlighter = new CountingVisibleSyntaxHighlighter();
        var editor = new CodeEditor(string.Join('\n', Enumerable.Range(0, 50_000).Select(i => $"Line {i:000000}")))
        {
            MinHeight = 8,
            MaxHeight = 8,
            SyntaxHighlighter = highlighter,
        };

        using var driver = new TerminalAppTestDriver(new VStack { editor }, TerminalHostKind.Fullscreen, new TerminalSize(36, 10));
        driver.Tick();

        highlighter.ResetLineRequests();
        editor.Scroll.SetOffset(0, 40_000);
        driver.Tick();

        Assert.AreEqual(1, highlighter.BuildCount, "Expected deep scrolling to reuse the existing syntax state.");
        Assert.AreEqual(0, highlighter.UpdateCount, "Expected deep scrolling not to trigger incremental syntax updates.");
        Assert.IsLessThanOrEqualTo(editor.Scroll.ViewportHeight + 1, highlighter.LineRequestCount, $"Expected deep scrolling to request syntax runs only for visible lines. Actual requests: {highlighter.LineRequestCount}.");
    }

    [TestMethod]
    public void CodeEditor_LongWrappedLine_Uses_Sparse_Checkpoints()
    {
        var text = new string('a', 400_000);
        var editor = new CodeEditor(text);

        using var driver = new TerminalAppTestDriver(new VStack { editor }, TerminalHostKind.Fullscreen, new TerminalSize(20, 8));
        driver.Tick();

        var initialDiagnostics = editor.GetLineLayoutDiagnostics(0);
        var rowCount = initialDiagnostics.RowCount;

        editor.Scroll.SetOffset(0, rowCount / 2);
        driver.Tick();

        var scrolledDiagnostics = editor.GetLineLayoutDiagnostics(0);

        Assert.IsGreaterThan(1_000, rowCount, "Expected the test input to produce a heavily wrapped line.");
        Assert.IsLessThan(rowCount / 8, initialDiagnostics.WrapRowCheckpointCount, $"Expected sparse checkpoint storage for wrapped rows. Row count: {rowCount}, checkpoints: {initialDiagnostics.WrapRowCheckpointCount}.");
        Assert.AreEqual(initialDiagnostics.WrapRowCheckpointCount, scrolledDiagnostics.WrapRowCheckpointCount, "Deep scrolling should not materialize per-row wrap offsets for the whole line.");
        Assert.IsLessThanOrEqualTo(initialDiagnostics.MaxWrapRowBlockCacheEntries, initialDiagnostics.ActiveWrapRowBlockCount, "Expected the wrapped-row block cache to stay bounded.");
        Assert.IsLessThanOrEqualTo(scrolledDiagnostics.MaxWrapRowBlockCacheEntries, scrolledDiagnostics.ActiveWrapRowBlockCount, "Expected deep scrolling to keep only a bounded number of cached wrapped-row blocks.");
        Assert.IsLessThanOrEqualTo(257, initialDiagnostics.MaxCachedWrapRowStartCount, "Expected each cached wrapped-row block to stay bounded.");
        Assert.IsLessThanOrEqualTo(257, scrolledDiagnostics.MaxCachedWrapRowStartCount, "Expected deep scrolling to keep each cached wrapped-row block bounded.");
    }

    [TestMethod]
    public void CodeEditor_HugeFile_Keeps_LineNumber_Gutter_Compact_Near_The_Start()
    {
        var text = string.Join('\n', Enumerable.Range(0, 100_000).Select(i => $"Line {i:000000}"));
        var editor = new CodeEditor(text)
        {
            MinHeight = 6,
            MaxHeight = 6,
        };

        using var driver = new TerminalAppTestDriver(new VStack { editor }, TerminalHostKind.Fullscreen, new TerminalSize(24, 8));
        driver.Tick();

        var screen = new AnsiTestScreen(24, 8);
        screen.Apply(driver.Backend.GetOutText());
        var row = screen.GetText().Split('\n')[0];
        var textStart = row.IndexOf("Line 000000", StringComparison.Ordinal);

        Assert.IsTrue(textStart >= 0, $"Expected the first line text to render. Row: `{row}`");
        Assert.IsLessThan(6, textStart, $"Expected the gutter to stay compact near the start of a huge file. Row: `{row}`");
    }

    [TestMethod]
    public void CodeEditor_LineNumber_Gutter_Expands_When_Crossing_A_Digit_Bucket()
    {
        var editor = new CodeEditor(string.Join('\n', Enumerable.Range(1, 150).Select(i => $"Line {i:000}")))
        {
            MinHeight = 6,
            MaxHeight = 6,
        };

        using var driver = new TerminalAppTestDriver(new VStack { editor }, TerminalHostKind.Fullscreen, new TerminalSize(24, 8));
        driver.Tick();

        var startScreen = new AnsiTestScreen(24, 8);
        startScreen.Apply(driver.Backend.GetOutText());
        var startRow = startScreen.GetText().Split('\n')[0];
        var startSeparatorIndex = startRow.IndexOf('│');

        driver.App.Focus(editor);
        for (var i = 0; i < 99; i++)
        {
            driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Down });
        }

        driver.TickUntil(() => editor.Scroll.OffsetY > 0);

        var laterScreen = new AnsiTestScreen(24, 8);
        laterScreen.Apply(driver.Backend.GetOutText());
        var laterRow = laterScreen.GetText().Split('\n')[0];
        var laterSeparatorIndex = laterRow.IndexOf('│');

        Assert.IsTrue(startSeparatorIndex >= 0 && laterSeparatorIndex >= 0, $"Expected the margin separator to render before and after crossing the digit bucket. Start=`{startRow}` Later=`{laterRow}`");
        Assert.IsTrue(laterSeparatorIndex > startSeparatorIndex, $"Expected the gutter to expand when the visible line numbers cross from two digits to three digits. Start=`{startRow}` Later=`{laterRow}`");
    }

    [TestMethod]
    public void CodeEditor_LineNumber_Bucket_Change_Reuses_Syntax_State()
    {
        var highlighter = new CountingVisibleSyntaxHighlighter();
        var editor = new CodeEditor(string.Join('\n', Enumerable.Range(1, 150).Select(i => $"Line {i:000}")))
        {
            MinHeight = 6,
            MaxHeight = 6,
            SyntaxHighlighter = highlighter,
        };

        using var driver = new TerminalAppTestDriver(new VStack { editor }, TerminalHostKind.Fullscreen, new TerminalSize(24, 8));
        driver.Tick();

        driver.App.Focus(editor);
        for (var i = 0; i < 99; i++)
        {
            driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Down });
        }

        driver.TickUntil(() => editor.Scroll.OffsetY > 0);

        Assert.AreEqual(1, highlighter.BuildCount, "Expected gutter width changes to reuse the existing syntax state.");
        Assert.AreEqual(0, highlighter.UpdateCount, "Expected gutter width changes not to trigger incremental syntax updates.");
    }

    [TestMethod]
    public void CodeEditor_Custom_Left_And_Right_Margins_Receive_Wrapped_Row_Mapping()
    {
        var leftMargin = new RecordingMargin(CodeEditorMarginSide.Left);
        var rightMargin = new RecordingMargin(CodeEditorMarginSide.Right);
        var editor = new CodeEditor("Wrapped margin sample line\nNext line")
        {
            MinHeight = 8,
            MaxHeight = 8,
        };

        editor.LeftMargins.Insert(0, leftMargin);
        editor.RightMargins.Add(rightMargin);

        using var driver = new TerminalAppTestDriver(new VStack { editor }, TerminalHostKind.Fullscreen, new TerminalSize(18, 7));
        driver.Tick();

        CollectionAssert.AreEqual(leftMargin.Rows.ToArray(), rightMargin.Rows.ToArray(), "Expected left and right margins to receive identical wrapped-row mappings.");
        Assert.IsTrue(leftMargin.Rows.Any(row => row.LineIndex == 0 && row.RowInLine > 0), "Expected custom margins to receive continuation wrapped rows for the same logical line.");
        Assert.IsTrue(leftMargin.Rows.Any(row => row.LineIndex == 1), $"Expected custom margins to receive later logical lines after wrapped rows. Actual rows: {string.Join(", ", leftMargin.Rows.Select(row => $"({row.LineIndex},{row.RowInLine})"))}");
    }

    private sealed class CountingVisibleSyntaxHighlighter : CodeEditorSyntaxHighlighter
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
                runs.Add(new StyledRun(0, Math.Min(4, request.LineLength), Style.None.WithForeground(Color.Basic16(5))));
            }
        }

        public void ResetLineRequests() => LineRequestCount = 0;
    }

    private sealed class TestSyntaxState : CodeEditorSyntaxState
    {
        public TestSyntaxState(int snapshotVersion)
        {
            SnapshotVersion = snapshotVersion;
        }

        public override int SnapshotVersion { get; }
    }

    private sealed class RecordingMargin : CodeEditorMargin
    {
        public RecordingMargin(CodeEditorMarginSide side)
        {
            Side = side;
        }

        public override CodeEditorMarginSide Side { get; }

        public List<(int LineIndex, int RowInLine)> Rows { get; } = new();

        public override int MeasureWidth(in CodeEditorMarginMeasureContext context)
        {
            _ = context;
            return 1;
        }

        public override void Render(in CodeEditorMarginRenderContext context)
        {
            Rows.Clear();
            for (var i = 0; i < context.VisibleLines.Count; i++)
            {
                var visible = context.VisibleLines[i];
                Rows.Add((visible.LineIndex, visible.RowInLine));
            }
        }
    }

    private sealed class CountingTextDocument : ITextDocument
    {
        private readonly TextDocument _inner;

        public CountingTextDocument(string text)
        {
            _inner = new TextDocument(text);
        }

        public int GetLineCallCount { get; private set; }

        public ITextSnapshot CurrentSnapshot => new CountingTextSnapshot(_inner.CurrentSnapshot, this);

        public int Version => _inner.Version;

        public event EventHandler<TextDocumentChangedEventArgs>? Changed
        {
            add => _inner.Changed += value;
            remove => _inner.Changed -= value;
        }

        public IDisposable BeginUpdate() => _inner.BeginUpdate();

        public void Insert(int position, ReadOnlySpan<char> text) => _inner.Insert(position, text);

        public void Remove(int position, int length) => _inner.Remove(position, length);

        public void Replace(int position, int length, ReadOnlySpan<char> text) => _inner.Replace(position, length, text);

        public void ResetCounters() => GetLineCallCount = 0;

        private sealed class CountingTextSnapshot : ITextSnapshot
        {
            private readonly ITextSnapshot _inner;
            private readonly CountingTextDocument _owner;

            public CountingTextSnapshot(ITextSnapshot inner, CountingTextDocument owner)
            {
                _inner = inner;
                _owner = owner;
            }

            public int Version => _inner.Version;

            public int Length => _inner.Length;

            public int LineCount => _inner.LineCount;

            public char this[int index] => _inner[index];

            public TextLine GetLine(int lineIndex)
            {
                _owner.GetLineCallCount++;
                return _inner.GetLine(lineIndex);
            }

            public int GetLineIndexFromPosition(int position) => _inner.GetLineIndexFromPosition(position);

            public void CopyTo(int start, Span<char> destination) => _inner.CopyTo(start, destination);
        }
    }
}
