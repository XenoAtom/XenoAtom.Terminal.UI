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
public sealed class TextEditorPerformanceTests
{
    [TestMethod]
    public void TextArea_Rendering_DeepScroll_Only_Reads_Visible_Lines()
    {
        var text = string.Join('\n', Enumerable.Range(0, 100_000).Select(i => $"Line {i:000000}"));
        var document = new CountingTextDocument(text);
        var textArea = new TextArea { TextDocument = document };
        var root = new VStack { textArea };

        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(40, 8));
        driver.Tick();

        document.ResetCounters();
        textArea.Scroll.SetOffset(0, 95_000);
        driver.Tick();

        Assert.IsLessThan(64, document.GetLineCallCount, $"Expected deep-scroll rendering to touch only visible lines. Actual line reads: {document.GetLineCallCount}.");
    }

    [TestMethod]
    public void TextArea_Incremental_Edit_Does_Not_Recompute_All_LineLayouts()
    {
        var text = string.Join('\n', Enumerable.Range(0, 100_000).Select(i => $"Line {i:000000}"));
        var document = new CountingTextDocument(text);
        var textArea = new TextArea { TextDocument = document };
        var root = new VStack { textArea };

        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(40, 8));
        driver.Tick();

        document.ResetCounters();
        document.Insert(document.CurrentSnapshot.Length, "\nTail".AsSpan());
        driver.Tick();

        Assert.IsLessThan(64, document.GetLineCallCount, $"Expected an incremental edit to update only changed lines plus the viewport. Actual line reads: {document.GetLineCallCount}.");
    }

    [TestMethod]
    public void TextArea_LongWrappedLine_Uses_Sparse_Checkpoints()
    {
        var text = new string('a', 400_000);
        var textArea = new TextArea(text);
        var root = new VStack { textArea };

        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(20, 8));
        driver.Tick();

        var initialDiagnostics = textArea.GetLineLayoutDiagnostics(0);
        var rowCount = initialDiagnostics.RowCount;

        textArea.Scroll.SetOffset(0, rowCount / 2);
        driver.Tick();

        var scrolledDiagnostics = textArea.GetLineLayoutDiagnostics(0);

        Assert.IsGreaterThan(1_000, rowCount, "Expected the test input to produce a heavily wrapped line.");
        Assert.IsLessThan(rowCount / 8, initialDiagnostics.WrapRowCheckpointCount, $"Expected sparse checkpoint storage for wrapped rows. Row count: {rowCount}, checkpoints: {initialDiagnostics.WrapRowCheckpointCount}.");
        Assert.AreEqual(initialDiagnostics.WrapRowCheckpointCount, scrolledDiagnostics.WrapRowCheckpointCount, "Deep scrolling should not materialize per-row wrap offsets for the whole line.");
        Assert.IsLessThanOrEqualTo(initialDiagnostics.MaxWrapRowBlockCacheEntries, initialDiagnostics.ActiveWrapRowBlockCount, "Expected the wrapped-row block cache to stay bounded.");
        Assert.IsLessThanOrEqualTo(scrolledDiagnostics.MaxWrapRowBlockCacheEntries, scrolledDiagnostics.ActiveWrapRowBlockCount, "Expected deep scrolling to keep only a bounded number of cached wrapped-row blocks.");
        Assert.IsLessThanOrEqualTo(257, initialDiagnostics.MaxCachedWrapRowStartCount, "Expected each cached wrapped-row block to stay bounded.");
        Assert.IsLessThanOrEqualTo(257, scrolledDiagnostics.MaxCachedWrapRowStartCount, "Expected deep scrolling to keep each cached wrapped-row block bounded.");
    }

    [TestMethod]
    public void TextArea_DeepScroll_OnLongWrappedLine_Renders_Expected_Row()
    {
        var text = string.Concat(Enumerable.Range(0, 200_000).Select(i => (char)('A' + (i % 26))));
        var textArea = new TextArea(text);
        var root = new VStack { textArea };

        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(24, 8));
        driver.Tick();

        var viewportWidth = Math.Max(1, textArea.Scroll.ViewportWidth);
        var targetRow = 1_000;
        textArea.Scroll.SetOffset(0, targetRow);
        driver.Tick();

        var expectedStart = targetRow * viewportWidth;
        var expected = text.Substring(expectedStart, Math.Min(viewportWidth, text.Length - expectedStart));

        var screen = new AnsiTestScreen(24, 8);
        screen.Apply(driver.Backend.GetOutText());
        var firstVisibleRow = screen.GetText().Split('\n')[0];

        StringAssert.Contains(firstVisibleRow, expected[..Math.Min(8, expected.Length)], "Expected deep scrolling to render the correct wrapped row from a very long line.");
    }

    [TestMethod]
    public void TextArea_PageNavigation_OnLongWrappedLine_Keeps_BlockCache_Bounded()
    {
        var text = new string('c', 1_000_000);
        var textArea = new TextArea(text);
        var root = new VStack { textArea };

        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(40, 12));
        driver.App.Focus(textArea);
        driver.Tick();

        for (var i = 0; i < 24; i++)
        {
            driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.PageDown });
            driver.Tick();
        }

        for (var i = 0; i < 12; i++)
        {
            driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.PageUp });
            driver.Tick();
        }

        var diagnostics = textArea.GetLineLayoutDiagnostics(0);
        Assert.IsGreaterThan(2_000, diagnostics.RowCount, "Expected the test input to produce a very large wrapped line.");
        Assert.IsLessThanOrEqualTo(diagnostics.MaxWrapRowBlockCacheEntries, diagnostics.ActiveWrapRowBlockCount, "Expected PageUp/PageDown navigation to reuse a bounded wrapped-row block cache.");
        Assert.IsLessThanOrEqualTo(257, diagnostics.MaxCachedWrapRowStartCount, "Expected PageUp/PageDown navigation to keep cached wrapped-row blocks fixed-size.");
    }

    [TestMethod]
    public void TextArea_LongWrappedLine_Reuses_Bounded_BlockCache_Across_Many_DeepScrolls()
    {
        var text = new string('b', 1_000_000);
        var textArea = new TextArea(text);
        var root = new VStack { textArea };

        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(32, 10));
        driver.Tick();

        var diagnostics = textArea.GetLineLayoutDiagnostics(0);
        var rowCount = diagnostics.RowCount;
        Assert.IsGreaterThan(2_000, rowCount, "Expected the test input to produce a very large wrapped line.");

        foreach (var row in new[] { 0, rowCount / 8, rowCount / 4, rowCount / 2, (rowCount * 3) / 4, rowCount - 1, rowCount / 3, rowCount / 5 })
        {
            textArea.Scroll.SetOffset(0, row);
            driver.Tick();
        }

        var finalDiagnostics = textArea.GetLineLayoutDiagnostics(0);
        Assert.IsLessThanOrEqualTo(finalDiagnostics.MaxWrapRowBlockCacheEntries, finalDiagnostics.ActiveWrapRowBlockCount, "Expected repeated deep scrolling to reuse a bounded wrapped-row block cache.");
        Assert.IsLessThanOrEqualTo(257, finalDiagnostics.MaxCachedWrapRowStartCount, "Expected cached wrapped-row blocks to remain fixed-size after repeated deep scrolling.");
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
