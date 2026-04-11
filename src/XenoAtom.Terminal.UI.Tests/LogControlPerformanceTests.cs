// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Layout;
using XenoAtom.Terminal.UI.Hosting;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class LogControlPerformanceTests
{
    [TestMethod]
    public void LogControl_LongWrappedEntry_Uses_Sparse_Checkpoints()
    {
        var log = new LogControl();
        log.AppendLine(new string('a', 400_000));

        using var driver = new TerminalAppTestDriver(log, TerminalHostKind.Fullscreen, new TerminalSize(20, 8));
        driver.Tick();

        var initialDiagnostics = log.GetEntryLayoutDiagnostics(0);
        var rowCount = initialDiagnostics.RowCount;
        var scrollViewer = log.EnumerateVisualsDepthFirst().OfType<ScrollViewer>().Single();

        scrollViewer.VerticalOffset = rowCount / 2;
        driver.Tick();

        var scrolledDiagnostics = log.GetEntryLayoutDiagnostics(0);

        Assert.IsGreaterThan(1_000, rowCount, "Expected the test input to produce a heavily wrapped log entry.");
        Assert.IsLessThan(rowCount / 8, initialDiagnostics.WrapRowCheckpointCount, $"Expected sparse checkpoint storage for wrapped rows. Row count: {rowCount}, checkpoints: {initialDiagnostics.WrapRowCheckpointCount}.");
        Assert.AreEqual(initialDiagnostics.WrapRowCheckpointCount, scrolledDiagnostics.WrapRowCheckpointCount, "Deep scrolling should not materialize per-row wrap offsets for the whole entry.");
        Assert.IsLessThanOrEqualTo(initialDiagnostics.MaxWrapRowBlockCacheEntries, initialDiagnostics.ActiveWrapRowBlockCount, "Expected the wrapped-row block cache to stay bounded.");
        Assert.IsLessThanOrEqualTo(scrolledDiagnostics.MaxWrapRowBlockCacheEntries, scrolledDiagnostics.ActiveWrapRowBlockCount, "Expected deep scrolling to keep only a bounded number of cached wrapped-row blocks.");
        Assert.IsLessThanOrEqualTo(257, initialDiagnostics.MaxCachedWrapRowStartCount, "Expected each cached wrapped-row block to stay bounded.");
        Assert.IsLessThanOrEqualTo(257, scrolledDiagnostics.MaxCachedWrapRowStartCount, "Expected deep scrolling to keep each cached wrapped-row block bounded.");
    }

    [TestMethod]
    public void LogControl_DeepScroll_OnLongWrappedEntry_Renders_Expected_Row()
    {
        var text = string.Concat(Enumerable.Range(0, 200_000).Select(i => (char)('A' + (i % 26))));
        var log = new LogControl();
        log.AppendLine(text);

        using var driver = new TerminalAppTestDriver(log, TerminalHostKind.Fullscreen, new TerminalSize(24, 8));
        driver.Tick();

        var scrollViewer = log.EnumerateVisualsDepthFirst().OfType<ScrollViewer>().Single();
        var viewportWidth = Math.Max(1, scrollViewer.ViewportWidth);
        var targetRow = 1_000;
        scrollViewer.VerticalOffset = targetRow;
        driver.Tick();

        var expectedStart = targetRow * viewportWidth;
        var expected = text.Substring(expectedStart, Math.Min(viewportWidth, text.Length - expectedStart));

        var screen = new AnsiTestScreen(24, 8);
        screen.Apply(driver.Backend.GetOutText());
        var firstVisibleRow = screen.GetText().Split('\n')[0];

        StringAssert.Contains(firstVisibleRow, expected[..Math.Min(8, expected.Length)], "Expected deep scrolling to render the correct wrapped row from a very long log entry.");
    }

    [TestMethod]
    public void LogControl_LongWrappedEntry_Reuses_Bounded_BlockCache_Across_Many_DeepScrolls()
    {
        var log = new LogControl();
        log.AppendLine(new string('b', 1_000_000));

        using var driver = new TerminalAppTestDriver(log, TerminalHostKind.Fullscreen, new TerminalSize(32, 10));
        driver.Tick();

        var diagnostics = log.GetEntryLayoutDiagnostics(0);
        var rowCount = diagnostics.RowCount;
        Assert.IsGreaterThan(2_000, rowCount, "Expected the test input to produce a very large wrapped log entry.");

        var scrollViewer = log.EnumerateVisualsDepthFirst().OfType<ScrollViewer>().Single();
        foreach (var row in new[] { 0, rowCount / 8, rowCount / 4, rowCount / 2, (rowCount * 3) / 4, rowCount - 1, rowCount / 3, rowCount / 5 })
        {
            scrollViewer.VerticalOffset = row;
            driver.Tick();
        }

        var finalDiagnostics = log.GetEntryLayoutDiagnostics(0);
        Assert.IsLessThanOrEqualTo(finalDiagnostics.MaxWrapRowBlockCacheEntries, finalDiagnostics.ActiveWrapRowBlockCount, "Expected repeated deep scrolling to reuse a bounded wrapped-row block cache.");
        Assert.IsLessThanOrEqualTo(257, finalDiagnostics.MaxCachedWrapRowStartCount, "Expected cached wrapped-row blocks to remain fixed-size after repeated deep scrolling.");
    }

    [TestMethod]
    public void LogControl_UnboundedMeasure_Does_Not_Populate_Wrapped_Layout_Cache()
    {
        var log = new LogControl();
        log.AppendLine(new string('x', 200_000));

        var hints = log.Measure(LayoutConstraints.Unbounded);
        var diagnostics = log.GetLayoutCacheDiagnostics();

        Assert.IsFalse(diagnostics.IsWrapCached, "Measuring with an unbounded width should not build wrapped-entry layout data.");
        Assert.AreEqual(0, diagnostics.CachedWrapWidth, "Unbounded measurement should leave the wrapped-width cache empty.");
        Assert.IsTrue(hints.Natural.Width > 0);
    }

    [TestMethod]
    public void LogControl_BoundedMeasure_Populates_Wrapped_Layout_Cache()
    {
        var log = new LogControl();
        log.AppendLine(new string('x', 200_000));

        _ = log.Measure(new LayoutConstraints(0, 24, 0, LayoutConstants.Infinite));
        var diagnostics = log.GetLayoutCacheDiagnostics();

        Assert.IsTrue(diagnostics.IsWrapCached, "Measuring with a bounded width should build wrapped-entry layout data.");
        Assert.IsTrue(diagnostics.CachedWrapWidth > 0 && diagnostics.CachedWrapWidth <= 24);
    }
}
