// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using XenoAtom.Terminal.UI.Text;

namespace XenoAtom.Terminal.UI.Controls;

internal readonly record struct TextEditorVisibleRowInfo(
    int VisualRow,
    int LineIndex,
    int LineStart,
    int LineLength,
    int RowInLine,
    int SegmentStart,
    int SegmentLength)
{
    public bool IsFirstRowOfLine => RowInLine == 0;
}

internal readonly record struct TextEditorSearchMatchInfo(int Start, int Length, bool IsActive);

internal sealed partial class TextEditorCore
{
    private const int WrapRowCheckpointStride = 64;
    private const int WrapRowBlockSize = 256;
    private const int WrapRowBlockCacheCapacity = 4;

    private TextDocumentChangedEventArgs? _pendingLayoutChange;
    private bool _hasMultiplePendingLayoutChanges;
    private readonly DocumentLayoutCache _layoutCache = new();

    private void ResetLayoutCache()
    {
        _pendingLayoutChange = null;
        _hasMultiplePendingLayoutChanges = false;
        _layoutCache.Reset();
    }

    private void NoteLayoutChange(TextDocumentChangedEventArgs e)
    {
        if (_pendingLayoutChange is null)
        {
            _pendingLayoutChange = e;
            return;
        }

        _hasMultiplePendingLayoutChanges = true;
    }

    private void EnsureMultiLineLayoutCache(in TextEditorOptions options)
        => EnsureMultiLineLayoutCache(options, _contentWidth);

    private void EnsureMultiLineLayoutCache(in TextEditorOptions options, int contentWidth)
    {
        if (options.SingleLine || contentWidth <= 0)
        {
            return;
        }

        var snapshot = _document.CurrentSnapshot;
        var text = GetText();

        if (_layoutCache.SnapshotVersion != snapshot.Version)
        {
            if (!_hasMultiplePendingLayoutChanges
                && _pendingLayoutChange is { } pending
                && pending.OldVersion == _layoutCache.SnapshotVersion
                && pending.NewVersion == snapshot.Version)
            {
                _layoutCache.ApplyChange(snapshot, text, pending, options.WordWrap, contentWidth, options.TabSize);
            }
            else
            {
                _layoutCache.Rebuild(snapshot, text, options.WordWrap, contentWidth, options.TabSize);
            }

            _pendingLayoutChange = null;
            _hasMultiplePendingLayoutChanges = false;
            return;
        }

        if (_layoutCache.RequiresLayoutRefresh(options.WordWrap, contentWidth, options.TabSize))
        {
            _layoutCache.RefreshLayout(snapshot, text, options.WordWrap, contentWidth, options.TabSize);
        }
    }

    private readonly record struct LineRowInfo(int LineIndex, int RowInLine);

    private readonly record struct WrapSegmentInfo(int RowInLine, int Start, int Length);

    internal readonly record struct TextEditorLineLayoutDiagnostics(
        int RowCount,
        int WrapRowCheckpointCount,
        int ActiveWrapRowBlockCount,
        int MaxCachedWrapRowStartCount,
        int MaxWrapRowBlockCacheEntries);

    private struct CachedWrapRowBlock
    {
        public int BlockIndex;
        public int StartRow;
        public int RowCount;
        public int[]? Starts;
        public int AccessStamp;
    }

    private struct CachedLineLayout
    {
        public int Start;
        public int Length;
        public int Width;
        public int RowOffset;
        public int RowCount;
        public int[]? WrapRowCheckpointStarts;
        public CachedWrapRowBlock[]? WrapRowBlocks;
        public int WrapRowBlockAccessStamp;
        public int CachedWrapWidth;
        public int CachedTabSize;
    }

    private sealed class DocumentLayoutCache
    {
        private CachedLineLayout[] _lines = [];
        private readonly SortedDictionary<int, int> _widthHistogram = new();
        private int _documentLength;
        private bool _wordWrap;
        private int _wrapWidth;
        private int _tabSize;

        public int SnapshotVersion { get; private set; } = -1;

        public int TotalRows { get; private set; } = 1;

        public int MaxWidth { get; private set; }

        public int LineCount => _lines.Length;

        public TextEditorLineLayoutDiagnostics GetLineDiagnostics(int lineIndex)
        {
            var line = _lines[lineIndex];
            var activeWrapRowBlockCount = 0;
            var maxCachedWrapRowStartCount = 0;

            if (line.WrapRowBlocks is { } blocks)
            {
                foreach (var block in blocks)
                {
                    if (block.Starts is null || block.RowCount <= 0)
                    {
                        continue;
                    }

                    activeWrapRowBlockCount++;
                    maxCachedWrapRowStartCount = Math.Max(maxCachedWrapRowStartCount, block.RowCount + 1);
                }
            }

            return new TextEditorLineLayoutDiagnostics(
                line.RowCount,
                line.WrapRowCheckpointStarts?.Length ?? 0,
                activeWrapRowBlockCount,
                maxCachedWrapRowStartCount,
                WrapRowBlockCacheCapacity);
        }

        public void Reset()
        {
            _lines = [];
            _widthHistogram.Clear();
            _documentLength = 0;
            _wordWrap = false;
            _wrapWidth = 0;
            _tabSize = 0;
            SnapshotVersion = -1;
            TotalRows = 1;
            MaxWidth = 0;
        }

        public bool RequiresLayoutRefresh(bool wordWrap, int wrapWidth, int tabSize)
            => SnapshotVersion >= 0 && (_wordWrap != wordWrap || _wrapWidth != wrapWidth || _tabSize != tabSize);

        public CachedLineLayout GetLine(int lineIndex) => _lines[lineIndex];

        public int GetLineIndexFromPosition(int position)
        {
            if (_lines.Length == 0)
            {
                return 0;
            }

            position = Math.Clamp(position, 0, _documentLength);

            var low = 0;
            var high = _lines.Length - 1;
            while (low <= high)
            {
                var mid = low + ((high - low) >> 1);
                var start = _lines[mid].Start;
                if (start == position)
                {
                    return mid;
                }

                if (start < position)
                {
                    low = mid + 1;
                }
                else
                {
                    high = mid - 1;
                }
            }

            return Math.Max(0, low - 1);
        }

        public LineRowInfo GetLineFromRow(int row)
        {
            if (_lines.Length == 0)
            {
                return new LineRowInfo(0, 0);
            }

            row = Math.Clamp(row, 0, Math.Max(0, TotalRows - 1));

            var low = 0;
            var high = _lines.Length - 1;
            while (low <= high)
            {
                var mid = low + ((high - low) >> 1);
                var line = _lines[mid];
                if (row < line.RowOffset)
                {
                    high = mid - 1;
                    continue;
                }

                var endRow = line.RowOffset + Math.Max(1, line.RowCount);
                if (row >= endRow)
                {
                    low = mid + 1;
                    continue;
                }

                return new LineRowInfo(mid, row - line.RowOffset);
            }

            var lastIndex = _lines.Length - 1;
            return new LineRowInfo(lastIndex, Math.Max(0, row - _lines[lastIndex].RowOffset));
        }

        public WrapSegmentInfo GetWrapSegmentAtRow(int lineIndex, string text, int wrapWidth, int tabSize, int rowInLine)
        {
            ref var line = ref _lines[lineIndex];
            if (line.Length == 0)
            {
                return new WrapSegmentInfo(0, 0, 0);
            }

            EnsureWrapCheckpointStarts(ref line, text, wrapWidth, tabSize);
            rowInLine = Math.Clamp(rowInLine, 0, Math.Max(0, line.RowCount - 1));
            var blockStarts = GetWrapRowBlock(lineIndex, text, wrapWidth, tabSize, rowInLine, out var blockStartRow, out _);
            var localRow = rowInLine - blockStartRow;
            var segmentStart = blockStarts[localRow];
            var segmentLength = blockStarts[localRow + 1] - segmentStart;
            return new WrapSegmentInfo(rowInLine, segmentStart, segmentLength);
        }

        public ReadOnlySpan<int> GetWrapRowBlock(int lineIndex, string text, int wrapWidth, int tabSize, int targetRowInLine, out int blockStartRow, out int blockRowCount)
        {
            ref var line = ref _lines[lineIndex];
            EnsureWrapCheckpointStarts(ref line, text, wrapWidth, tabSize);
            targetRowInLine = Math.Clamp(targetRowInLine, 0, Math.Max(0, line.RowCount - 1));
            var blockSlot = EnsureWrapRowBlock(ref line, text, wrapWidth, tabSize, targetRowInLine);
            ref var block = ref line.WrapRowBlocks![blockSlot];
            blockStartRow = block.StartRow;
            blockRowCount = block.RowCount;
            return block.Starts!.AsSpan(0, block.RowCount + 1);
        }

        public WrapSegmentInfo FindWrapSegmentForIndex(int lineIndex, string text, int wrapWidth, int tabSize, int indexInLine)
        {
            ref var line = ref _lines[lineIndex];
            if (line.Length == 0)
            {
                return new WrapSegmentInfo(0, 0, 0);
            }

            EnsureWrapCheckpointStarts(ref line, text, wrapWidth, tabSize);
            indexInLine = Math.Clamp(indexInLine, 0, line.Length);

            if (TryFindWrapSegmentInCachedBlocks(ref line, indexInLine, out var windowSegment))
            {
                return windowSegment;
            }

            var checkpointStarts = line.WrapRowCheckpointStarts!;
            var checkpointIndex = FindCheckpointIndex(checkpointStarts, indexInLine);
            var approximateRow = Math.Min(Math.Max(0, line.RowCount - 1), checkpointIndex * WrapRowCheckpointStride);
            _ = EnsureWrapRowBlock(ref line, text, wrapWidth, tabSize, approximateRow);

            if (TryFindWrapSegmentInCachedBlocks(ref line, indexInLine, out windowSegment))
            {
                return windowSegment;
            }

            var nextApproximateRow = Math.Min(Math.Max(0, line.RowCount - 1), approximateRow + WrapRowBlockSize);
            if (nextApproximateRow != approximateRow)
            {
                _ = EnsureWrapRowBlock(ref line, text, wrapWidth, tabSize, nextApproximateRow);
                if (TryFindWrapSegmentInCachedBlocks(ref line, indexInLine, out windowSegment))
                {
                    return windowSegment;
                }
            }

            var fallbackRow = approximateRow;
            return GetWrapSegmentAtRow(lineIndex, text, wrapWidth, tabSize, fallbackRow);
        }

        public void Rebuild(ITextSnapshot snapshot, string text, bool wordWrap, int wrapWidth, int tabSize)
        {
            _widthHistogram.Clear();
            _lines = new CachedLineLayout[snapshot.LineCount];
            _documentLength = snapshot.Length;
            SnapshotVersion = snapshot.Version;
            _wordWrap = wordWrap;
            _wrapWidth = wrapWidth;
            _tabSize = tabSize;

            var rowOffset = 0;
            for (var i = 0; i < _lines.Length; i++)
            {
                var layout = CreateLineLayout(snapshot.GetLine(i), text, wordWrap, wrapWidth, tabSize);
                layout.RowOffset = rowOffset;
                rowOffset += layout.RowCount;
                _lines[i] = layout;
                AddWidth(layout.Width);
            }

            TotalRows = Math.Max(1, rowOffset);
            UpdateMaxWidth();
        }

        public void RefreshLayout(ITextSnapshot snapshot, string text, bool wordWrap, int wrapWidth, int tabSize)
        {
            if (tabSize != _tabSize || SnapshotVersion != snapshot.Version)
            {
                Rebuild(snapshot, text, wordWrap, wrapWidth, tabSize);
                return;
            }

            _wordWrap = wordWrap;
            _wrapWidth = wrapWidth;

            var rowOffset = 0;
            for (var i = 0; i < _lines.Length; i++)
            {
                ref var line = ref _lines[i];
                var refreshed = CreateLineLayout(new TextLine(i, line.Start, line.Length, 0), text, wordWrap, wrapWidth, tabSize);
                refreshed.RowOffset = rowOffset;
                _lines[i] = refreshed;
                rowOffset += refreshed.RowCount;
            }

            TotalRows = Math.Max(1, rowOffset);
        }

        public void ApplyChange(ITextSnapshot snapshot, string text, TextDocumentChangedEventArgs change, bool wordWrap, int wrapWidth, int tabSize)
        {
            if (SnapshotVersion < 0 || change.OldVersion != SnapshotVersion || tabSize != _tabSize)
            {
                Rebuild(snapshot, text, wordWrap, wrapWidth, tabSize);
                return;
            }

            var startLine = GetLineIndexFromPosition(change.Position);
            var oldEndPosition = Math.Min(_documentLength, change.Position + change.RemovedLength);
            var oldEndLine = GetLineIndexFromPosition(oldEndPosition);
            var oldAffectedCount = Math.Max(1, oldEndLine - startLine + 1);

            var newEndPosition = Math.Min(snapshot.Length, change.Position + change.InsertedLength);
            var newEndLine = snapshot.GetLineIndexFromPosition(newEndPosition);
            var newAffectedCount = Math.Max(1, newEndLine - startLine + 1);
            var lineDelta = newAffectedCount - oldAffectedCount;
            var charDelta = change.InsertedLength - change.RemovedLength;

            for (var i = startLine; i < startLine + oldAffectedCount; i++)
            {
                RemoveWidth(_lines[i].Width);
            }

            var nextLines = lineDelta == 0
                ? _lines
                : new CachedLineLayout[_lines.Length + lineDelta];

            if (lineDelta != 0)
            {
                if (startLine > 0)
                {
                    Array.Copy(_lines, 0, nextLines, 0, startLine);
                }

                var oldTailStart = startLine + oldAffectedCount;
                var newTailStart = startLine + newAffectedCount;
                var tailCount = _lines.Length - oldTailStart;
                if (tailCount > 0)
                {
                    Array.Copy(_lines, oldTailStart, nextLines, newTailStart, tailCount);
                }
            }

            for (var i = 0; i < newAffectedCount; i++)
            {
                var line = snapshot.GetLine(startLine + i);
                var layout = CreateLineLayout(line, text, wordWrap, wrapWidth, tabSize);
                nextLines[startLine + i] = layout;
                AddWidth(layout.Width);
            }

            var tailStartIndex = startLine + newAffectedCount;
            for (var i = tailStartIndex; i < nextLines.Length; i++)
            {
                nextLines[i].Start += charDelta;
            }

            _lines = nextLines;
            _documentLength = snapshot.Length;
            SnapshotVersion = snapshot.Version;
            _wordWrap = wordWrap;
            _wrapWidth = wrapWidth;
            _tabSize = tabSize;

            var rowOffset = startLine == 0
                ? 0
                : _lines[startLine - 1].RowOffset + _lines[startLine - 1].RowCount;

            for (var i = startLine; i < _lines.Length; i++)
            {
                _lines[i].RowOffset = rowOffset;
                rowOffset += _lines[i].RowCount;
            }

            TotalRows = Math.Max(1, rowOffset);
            UpdateMaxWidth();
        }

        private static CachedLineLayout CreateLineLayout(TextLine line, string text, bool wordWrap, int wrapWidth, int tabSize)
        {
            var lineSpan = text.AsSpan(line.Start, line.Length);
            if (!wordWrap || wrapWidth <= 0)
            {
                return new CachedLineLayout
                {
                    Start = line.Start,
                    Length = line.Length,
                    Width = GetTextCells(lineSpan, tabSize),
                    RowCount = 1,
                    RowOffset = 0,
                    WrapRowCheckpointStarts = null,
                    WrapRowBlocks = null,
                    WrapRowBlockAccessStamp = 0,
                    CachedWrapWidth = 0,
                    CachedTabSize = 0,
                };
            }

            AnalyzeWrappedLine(lineSpan, wrapWidth, tabSize, out var width, out var rowCount, out var checkpoints);
            return new CachedLineLayout
            {
                Start = line.Start,
                Length = line.Length,
                Width = width,
                RowCount = rowCount,
                RowOffset = 0,
                WrapRowCheckpointStarts = checkpoints,
                WrapRowBlocks = null,
                WrapRowBlockAccessStamp = 0,
                CachedWrapWidth = wrapWidth,
                CachedTabSize = tabSize,
            };
        }

        private static void AnalyzeWrappedLine(ReadOnlySpan<char> lineSpan, int wrapWidth, int tabSize, out int width, out int rowCount, out int[] checkpoints)
        {
            width = 0;
            rowCount = 1;
            checkpoints = [0];

            if (lineSpan.IsEmpty)
            {
                return;
            }

            var checkpointList = new List<int>(Math.Max(2, (lineSpan.Length / Math.Max(1, wrapWidth * WrapRowCheckpointStride)) + 1)) { 0 };
            var wrapColumn = 0;
            var index = 0;

            while (index < lineSpan.Length)
            {
                var next = GetNextTextElementIndexFast(lineSpan, index);
                if (next <= index)
                {
                    break;
                }

                var element = lineSpan.Slice(index, next - index);
                var totalWidth = GetTextElementCellWidth(element, width, tabSize);
                var wrappedWidth = GetTextElementCellWidth(element, wrapColumn, tabSize);

                if (wrapColumn + wrappedWidth > wrapWidth && wrapColumn > 0)
                {
                    AddCheckpointForRowStart(checkpointList, rowCount, index);
                    rowCount++;
                    wrapColumn = 0;
                    wrappedWidth = GetTextElementCellWidth(element, wrapColumn, tabSize);
                }

                width += totalWidth;
                wrapColumn += wrappedWidth;
                index = next;

                if (wrapColumn >= wrapWidth && index < lineSpan.Length)
                {
                    AddCheckpointForRowStart(checkpointList, rowCount, index);
                    rowCount++;
                    wrapColumn = 0;
                }
            }

            checkpoints = checkpointList.ToArray();
        }

        private static void AddCheckpointForRowStart(List<int> checkpoints, int rowIndex, int startIndex)
        {
            if (rowIndex % WrapRowCheckpointStride != 0)
            {
                return;
            }

            if (checkpoints.Count == 0 || checkpoints[^1] != startIndex)
            {
                checkpoints.Add(startIndex);
            }
        }

        private static int FindCheckpointIndex(ReadOnlySpan<int> checkpointStarts, int indexInLine)
        {
            var low = 0;
            var high = checkpointStarts.Length - 1;
            while (low <= high)
            {
                var mid = low + ((high - low) >> 1);
                var start = checkpointStarts[mid];
                if (start == indexInLine)
                {
                    return mid;
                }

                if (start < indexInLine)
                {
                    low = mid + 1;
                }
                else
                {
                    high = mid - 1;
                }
            }

            return Math.Max(0, low - 1);
        }

        private static void EnsureWrapCheckpointStarts(ref CachedLineLayout line, string text, int wrapWidth, int tabSize)
        {
            if (line.WrapRowCheckpointStarts is not null && line.CachedWrapWidth == wrapWidth && line.CachedTabSize == tabSize)
            {
                return;
            }

            var lineSpan = text.AsSpan(line.Start, line.Length);
            AnalyzeWrappedLine(lineSpan, wrapWidth, tabSize, out var width, out var rowCount, out var checkpoints);
            line.Width = width;
            line.RowCount = rowCount;
            line.WrapRowCheckpointStarts = checkpoints;
            line.WrapRowBlocks = null;
            line.WrapRowBlockAccessStamp = 0;
            line.CachedWrapWidth = wrapWidth;
            line.CachedTabSize = tabSize;
        }

        private static int EnsureWrapRowBlock(ref CachedLineLayout line, string text, int wrapWidth, int tabSize, int targetRowInLine)
        {
            var blockIndex = targetRowInLine / WrapRowBlockSize;
            var nextAccessStamp = unchecked(line.WrapRowBlockAccessStamp + 1);
            line.WrapRowBlockAccessStamp = nextAccessStamp == 0 ? 1 : nextAccessStamp;

            var blocks = line.WrapRowBlocks;
            if (blocks is null)
            {
                blocks = new CachedWrapRowBlock[WrapRowBlockCacheCapacity];
                line.WrapRowBlocks = blocks;
            }

            for (var i = 0; i < blocks.Length; i++)
            {
                if (blocks[i].Starts is null || blocks[i].BlockIndex != blockIndex)
                {
                    continue;
                }

                blocks[i].AccessStamp = line.WrapRowBlockAccessStamp;
                return i;
            }

            var blockSlot = FindWrapRowBlockSlot(blocks);
            ref var block = ref blocks[blockSlot];
            block.Starts ??= new int[WrapRowBlockSize + 1];

            PopulateWrapRowBlock(ref line, ref block, text, wrapWidth, tabSize, blockIndex);
            block.AccessStamp = line.WrapRowBlockAccessStamp;
            return blockSlot;
        }

        private static int FindWrapRowBlockSlot(CachedWrapRowBlock[] blocks)
        {
            var emptySlot = -1;
            var leastRecentlyUsedSlot = 0;
            var leastRecentlyUsedStamp = int.MaxValue;

            for (var i = 0; i < blocks.Length; i++)
            {
                if (blocks[i].Starts is null)
                {
                    emptySlot = i;
                    break;
                }

                if (blocks[i].AccessStamp >= leastRecentlyUsedStamp)
                {
                    continue;
                }

                leastRecentlyUsedStamp = blocks[i].AccessStamp;
                leastRecentlyUsedSlot = i;
            }

            return emptySlot >= 0 ? emptySlot : leastRecentlyUsedSlot;
        }

        private static void PopulateWrapRowBlock(ref CachedLineLayout line, ref CachedWrapRowBlock block, string text, int wrapWidth, int tabSize, int blockIndex)
        {
            var rowStart = Math.Max(0, blockIndex * WrapRowBlockSize);
            var checkpointIndex = rowStart / WrapRowCheckpointStride;
            var checkpointStarts = line.WrapRowCheckpointStarts!;
            var currentRow = checkpointIndex * WrapRowCheckpointStride;
            var segmentStart = checkpointStarts[Math.Min(checkpointIndex, checkpointStarts.Length - 1)];
            var lineSpan = text.AsSpan(line.Start, line.Length);
            var starts = block.Starts!;

            while (currentRow < rowStart && segmentStart < lineSpan.Length)
            {
                segmentStart += GetWrapSegmentLength(lineSpan, segmentStart, wrapWidth, tabSize);
                currentRow++;
            }

            var rowsInBlock = Math.Max(1, Math.Min(WrapRowBlockSize, line.RowCount - rowStart));
            starts[0] = segmentStart;

            for (var i = 0; i < rowsInBlock; i++)
            {
                if (segmentStart < lineSpan.Length)
                {
                    segmentStart += GetWrapSegmentLength(lineSpan, segmentStart, wrapWidth, tabSize);
                }

                starts[i + 1] = segmentStart;
            }

            block.BlockIndex = blockIndex;
            block.StartRow = rowStart;
            block.RowCount = rowsInBlock;
        }

        private static bool TryFindWrapSegmentInCachedBlocks(ref CachedLineLayout line, int indexInLine, out WrapSegmentInfo segment)
        {
            var blocks = line.WrapRowBlocks;
            if (blocks is null || blocks.Length == 0)
            {
                segment = default;
                return false;
            }

            for (var blockIndex = 0; blockIndex < blocks.Length; blockIndex++)
            {
                ref var block = ref blocks[blockIndex];
                var starts = block.Starts;
                if (starts is null || block.RowCount <= 0)
                {
                    continue;
                }

                var blockStartIndex = starts[0];
                var blockEndIndex = starts[block.RowCount];
                if (indexInLine < blockStartIndex || indexInLine > blockEndIndex)
                {
                    continue;
                }

                var low = 0;
                var high = block.RowCount - 1;
                while (low <= high)
                {
                    var mid = low + ((high - low) >> 1);
                    var start = starts[mid];
                    var end = starts[mid + 1];
                    if (indexInLine < start)
                    {
                        high = mid - 1;
                        continue;
                    }

                    if (indexInLine > end || (indexInLine == end && end < line.Length && mid < block.RowCount - 1))
                    {
                        low = mid + 1;
                        continue;
                    }

                    block.AccessStamp = line.WrapRowBlockAccessStamp;
                    segment = new WrapSegmentInfo(block.StartRow + mid, start, end - start);
                    return true;
                }
            }

            segment = default;
            return false;
        }

        private void AddWidth(int width)
        {
            if (_widthHistogram.TryGetValue(width, out var count))
            {
                _widthHistogram[width] = count + 1;
            }
            else
            {
                _widthHistogram[width] = 1;
            }
        }

        private void RemoveWidth(int width)
        {
            if (!_widthHistogram.TryGetValue(width, out var count))
            {
                return;
            }

            if (count <= 1)
            {
                _widthHistogram.Remove(width);
            }
            else
            {
                _widthHistogram[width] = count - 1;
            }
        }

        private void UpdateMaxWidth()
            => MaxWidth = _widthHistogram.Count == 0 ? 0 : _widthHistogram.Last().Key;
    }
}
