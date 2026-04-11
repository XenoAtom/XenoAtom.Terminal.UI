// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Collections.Generic;

namespace XenoAtom.Terminal.UI.Controls;

public sealed partial class LogControl
{
    private sealed partial class LogContentVisual
    {
        private const int WrapRowCheckpointStride = 64;
        private const int WrapRowBlockSize = 256;
        private const int WrapRowBlockCacheCapacity = 4;

        private CachedEntryLayout[] _entryLayouts = [];
        private int _entryLayoutCount;
        private readonly SortedDictionary<int, int> _widthHistogram = new();
        private bool _layoutCacheInitialized;
        private bool _cachedWrap;
        private int _cachedWrapWidth;

        private readonly record struct WrapSegmentInfo(int RowInEntry, int Start, int Length);

        private struct CachedWrapRowBlock
        {
            public int BlockIndex;
            public int StartRow;
            public int RowCount;
            public int[]? Starts;
            public int[]? Ends;
            public int AccessStamp;
        }

        private struct CachedEntryLayout
        {
            public int Width;
            public int RowOffset;
            public int RowCount;
            public int[]? WrapRowCheckpointStarts;
            public CachedWrapRowBlock[]? WrapRowBlocks;
            public int WrapRowBlockAccessStamp;
            public int CachedWrapWidth;
        }

        public void ResetLayoutCache()
        {
            _entryLayouts = [];
            _entryLayoutCount = 0;
            _widthHistogram.Clear();
            _layoutCacheInitialized = false;
            _cachedWrap = false;
            _cachedWrapWidth = 0;
            _extentHeight = 0;
            _extentWidth = 0;
            _lastWrapWidth = 0;
        }

        public void OnEntryAdded(LogEntry entry)
        {
            if (!_layoutCacheInitialized)
            {
                return;
            }

            var layout = CreateEntryLayout(entry, _cachedWrap, _cachedWrapWidth);
            layout.RowOffset = _entryLayoutCount == 0
                ? 0
                : _entryLayouts[_entryLayoutCount - 1].RowOffset + _entryLayouts[_entryLayoutCount - 1].RowCount;

            EnsureEntryLayoutCapacity(_entryLayoutCount + 1);
            _entryLayouts[_entryLayoutCount++] = layout;
            AddWidth(layout.Width);

            _extentHeight = layout.RowOffset + layout.RowCount;
            UpdateMaxWidth();
        }

        public void OnEntriesRemoved(int index, int count)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(index);
            ArgumentOutOfRangeException.ThrowIfNegative(count);

            if (count == 0 || !_layoutCacheInitialized)
            {
                return;
            }

            if (index > _entryLayoutCount - count)
            {
                ResetLayoutCache();
                return;
            }

            for (var i = index; i < index + count; i++)
            {
                RemoveWidth(_entryLayouts[i].Width);
            }

            var tailStart = index + count;
            var tailCount = _entryLayoutCount - tailStart;
            if (tailCount > 0)
            {
                Array.Copy(_entryLayouts, tailStart, _entryLayouts, index, tailCount);
            }

            _entryLayoutCount -= count;

            if (_entryLayoutCount == 0)
            {
                _extentHeight = 0;
                UpdateMaxWidth();
                return;
            }

            var rowOffset = index == 0
                ? 0
                : _entryLayouts[index - 1].RowOffset + _entryLayouts[index - 1].RowCount;

            for (var i = index; i < _entryLayoutCount; i++)
            {
                _entryLayouts[i].RowOffset = rowOffset;
                rowOffset += _entryLayouts[i].RowCount;
            }

            _extentHeight = rowOffset;
            UpdateMaxWidth();
        }

        public int GetPrefixRowCount(int entryCount, bool wrap, int wrapWidth)
        {
            EnsureLayoutCache(wrap, wrapWidth);

            if (entryCount <= 0)
            {
                return 0;
            }

            if (entryCount >= _entryLayoutCount)
            {
                return _extentHeight;
            }

            return _entryLayouts[entryCount].RowOffset;
        }

        public LogEntryLayoutDiagnostics GetEntryLayoutDiagnostics(int entryIndex)
        {
            var wrapWidth = _lastWrapWidth > 0 ? _lastWrapWidth : Math.Max(1, _owner._scrollViewer.ViewportWidth);
            EnsureLayoutCache(_owner.WrapText, Math.Max(1, wrapWidth));

            if ((uint)entryIndex >= (uint)_entryLayoutCount)
            {
                return default;
            }

            var entry = _entryLayouts[entryIndex];
            var activeWrapRowBlockCount = 0;
            var maxCachedWrapRowStartCount = 0;

            if (entry.WrapRowBlocks is { } blocks)
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

            return new LogEntryLayoutDiagnostics(
                entry.RowCount,
                entry.WrapRowCheckpointStarts?.Length ?? 0,
                activeWrapRowBlockCount,
                maxCachedWrapRowStartCount,
                WrapRowBlockCacheCapacity);
        }

        public LogLayoutCacheDiagnostics GetLayoutCacheDiagnostics()
            => new(
                IsInitialized: _layoutCacheInitialized,
                IsWrapCached: _cachedWrap,
                CachedWrapWidth: _cachedWrapWidth,
                EntryCount: _entryLayoutCount,
                ExtentWidth: _extentWidth,
                ExtentHeight: _extentHeight);

        private int GetPreferredWrapWidth()
        {
            var viewportWidth = _owner._scrollViewer.ViewportWidth;
            if (viewportWidth > 0)
            {
                return viewportWidth;
            }

            var boundsWidth = Bounds.Width;
            if (boundsWidth > 0)
            {
                return boundsWidth;
            }

            return _cachedWrapWidth > 0 ? _cachedWrapWidth : 80;
        }

        private void EnsureLayoutCache(bool wrap, int wrapWidth)
        {
            if (!wrap)
            {
                wrapWidth = 0;
            }
            else
            {
                wrapWidth = Math.Max(1, wrapWidth);
            }

            var entryCount = _owner._entries.Count;

            if (!_layoutCacheInitialized || _entryLayoutCount != entryCount)
            {
                RebuildLayoutCache(wrap, wrapWidth);
                return;
            }

            if (_cachedWrap != wrap || (wrap && _cachedWrapWidth != wrapWidth))
            {
                RefreshLayoutCache(wrap, wrapWidth);
            }
        }

        private void RebuildLayoutCache(bool wrap, int wrapWidth)
        {
            if (!wrap)
            {
                wrapWidth = 0;
            }
            else
            {
                wrapWidth = Math.Max(1, wrapWidth);
            }

            _widthHistogram.Clear();

            var entries = _owner._entries;
            var count = entries.Count;
            EnsureEntryLayoutCapacity(count);

            var rowOffset = 0;
            for (var i = 0; i < count; i++)
            {
                var layout = CreateEntryLayout(entries[i], wrap, wrapWidth);
                layout.RowOffset = rowOffset;
                rowOffset += layout.RowCount;
                _entryLayouts[i] = layout;
                AddWidth(layout.Width);
            }

            _entryLayoutCount = count;
            _layoutCacheInitialized = true;
            _cachedWrap = wrap;
            _cachedWrapWidth = wrapWidth;
            _extentHeight = rowOffset;
            UpdateMaxWidth();
        }

        private void RefreshLayoutCache(bool wrap, int wrapWidth)
        {
            if (!wrap)
            {
                wrapWidth = 0;
            }
            else
            {
                wrapWidth = Math.Max(1, wrapWidth);
            }

            _widthHistogram.Clear();

            var entries = _owner._entries;
            var rowOffset = 0;
            for (var i = 0; i < _entryLayoutCount; i++)
            {
                var layout = CreateEntryLayout(entries[i], wrap, wrapWidth);
                layout.RowOffset = rowOffset;
                rowOffset += layout.RowCount;
                _entryLayouts[i] = layout;
                AddWidth(layout.Width);
            }

            _cachedWrap = wrap;
            _cachedWrapWidth = wrapWidth;
            _extentHeight = rowOffset;
            UpdateMaxWidth();
        }

        private void EnsureEntryLayoutCapacity(int count)
        {
            if (_entryLayouts.Length >= count)
            {
                return;
            }

            var nextSize = Math.Max(4, _entryLayouts.Length == 0 ? count : _entryLayouts.Length);
            while (nextSize < count)
            {
                nextSize *= 2;
            }

            Array.Resize(ref _entryLayouts, nextSize);
        }

        private CachedEntryLayout CreateEntryLayout(LogEntry entry, bool wrap, int wrapWidth)
        {
            var width = MeasureCellWidth(entry.Text.AsSpan());
            if (!wrap)
            {
                return new CachedEntryLayout
                {
                    Width = width,
                    RowOffset = 0,
                    RowCount = 1,
                    WrapRowCheckpointStarts = null,
                    WrapRowBlocks = null,
                    WrapRowBlockAccessStamp = 0,
                    CachedWrapWidth = 0,
                };
            }

            AnalyzeWrappedEntry(entry.Text.AsSpan(), wrapWidth, out var rowCount, out var checkpoints);
            return new CachedEntryLayout
            {
                Width = width,
                RowOffset = 0,
                RowCount = rowCount,
                WrapRowCheckpointStarts = checkpoints,
                WrapRowBlocks = null,
                WrapRowBlockAccessStamp = 0,
                CachedWrapWidth = wrapWidth,
            };
        }

        private static void AnalyzeWrappedEntry(ReadOnlySpan<char> text, int wrapWidth, out int rowCount, out int[] checkpoints)
        {
            rowCount = 1;
            checkpoints = [0];

            if (text.IsEmpty)
            {
                return;
            }

            wrapWidth = Math.Max(1, wrapWidth);
            var checkpointList = new List<int>(Math.Max(2, (text.Length / Math.Max(1, wrapWidth * WrapRowCheckpointStride)) + 1)) { 0 };

            var rowIndex = 0;
            var start = 0;
            while (start < text.Length)
            {
                if (!TryGetNextWrapSlice(text, start, wrapWidth, out _, out var nextStart))
                {
                    break;
                }

                rowIndex++;
                if (nextStart >= text.Length)
                {
                    break;
                }

                if (rowIndex % WrapRowCheckpointStride == 0 && checkpointList[^1] != nextStart)
                {
                    checkpointList.Add(nextStart);
                }

                if (nextStart <= start)
                {
                    break;
                }

                start = nextStart;
            }

            rowCount = Math.Max(1, rowIndex);
            checkpoints = checkpointList.ToArray();
        }

        private bool TryMapRowToEntry(int row, out int entryIndex, out int rowInEntry)
        {
            entryIndex = 0;
            rowInEntry = 0;
            if (_entryLayoutCount == 0 || row < 0)
            {
                return false;
            }

            row = Math.Clamp(row, 0, Math.Max(0, _extentHeight - 1));

            var low = 0;
            var high = _entryLayoutCount - 1;
            while (low <= high)
            {
                var mid = low + ((high - low) >> 1);
                var layout = _entryLayouts[mid];
                if (row < layout.RowOffset)
                {
                    high = mid - 1;
                    continue;
                }

                var endRow = layout.RowOffset + Math.Max(1, layout.RowCount);
                if (row >= endRow)
                {
                    low = mid + 1;
                    continue;
                }

                entryIndex = mid;
                rowInEntry = row - layout.RowOffset;
                return true;
            }

            entryIndex = _entryLayoutCount - 1;
            rowInEntry = Math.Max(0, row - _entryLayouts[entryIndex].RowOffset);
            return true;
        }

        private int GetEntryRowCount(int entryIndex)
            => (uint)entryIndex < (uint)_entryLayoutCount ? _entryLayouts[entryIndex].RowCount : 1;

        private int GetEntryRowOffset(int entryIndex)
            => (uint)entryIndex < (uint)_entryLayoutCount ? _entryLayouts[entryIndex].RowOffset : 0;

        private WrapSegmentInfo GetWrapSegmentAtRow(int entryIndex, ReadOnlySpan<char> text, int rowInEntry, int wrapWidth)
        {
            ref var entry = ref _entryLayouts[entryIndex];
            if (text.IsEmpty)
            {
                return new WrapSegmentInfo(0, 0, 0);
            }

            EnsureWrapCheckpointStarts(ref entry, text, wrapWidth);
            rowInEntry = Math.Clamp(rowInEntry, 0, Math.Max(0, entry.RowCount - 1));
            var block = GetWrapRowBlock(entryIndex, text, wrapWidth, rowInEntry, out var blockStartRow, out _);
            var localRow = rowInEntry - blockStartRow;
            var starts = block.Starts!;
            var ends = block.Ends!;
            var start = starts[localRow];
            var end = ends[localRow];
            return new WrapSegmentInfo(rowInEntry, start, end - start);
        }

        private WrapSegmentInfo FindWrapSegmentForIndex(int entryIndex, ReadOnlySpan<char> text, int wrapWidth, int textIndex)
        {
            ref var entry = ref _entryLayouts[entryIndex];
            if (text.IsEmpty)
            {
                return new WrapSegmentInfo(0, 0, 0);
            }

            EnsureWrapCheckpointStarts(ref entry, text, wrapWidth);
            textIndex = Math.Clamp(textIndex, 0, text.Length);

            if (TryFindWrapSegmentInCachedBlocks(ref entry, textIndex, out var segment))
            {
                return segment;
            }

            var checkpointStarts = entry.WrapRowCheckpointStarts!;
            var checkpointIndex = FindCheckpointIndex(checkpointStarts, textIndex);
            var approximateRow = Math.Min(Math.Max(0, entry.RowCount - 1), checkpointIndex * WrapRowCheckpointStride);
            _ = GetWrapRowBlock(entryIndex, text, wrapWidth, approximateRow, out _, out _);

            if (TryFindWrapSegmentInCachedBlocks(ref entry, textIndex, out segment))
            {
                return segment;
            }

            var nextApproximateRow = Math.Min(Math.Max(0, entry.RowCount - 1), approximateRow + WrapRowBlockSize);
            if (nextApproximateRow != approximateRow)
            {
                _ = GetWrapRowBlock(entryIndex, text, wrapWidth, nextApproximateRow, out _, out _);
                if (TryFindWrapSegmentInCachedBlocks(ref entry, textIndex, out segment))
                {
                    return segment;
                }
            }

            var rowIndex = approximateRow;
            var start = checkpointStarts[Math.Min(checkpointIndex, checkpointStarts.Length - 1)];
            while (start < text.Length)
            {
                if (!TryGetNextWrapSlice(text, start, wrapWidth, out var end, out var nextStart))
                {
                    break;
                }

                if (textIndex <= end || textIndex < nextStart)
                {
                    return new WrapSegmentInfo(rowIndex, start, end - start);
                }

                if (nextStart <= start)
                {
                    break;
                }

                rowIndex++;
                start = nextStart;
            }

            return GetWrapSegmentAtRow(entryIndex, text, Math.Max(0, entry.RowCount - 1), wrapWidth);
        }

        private void EnsureWrapCheckpointStarts(ref CachedEntryLayout entry, ReadOnlySpan<char> text, int wrapWidth)
        {
            if (entry.WrapRowCheckpointStarts is not null && entry.CachedWrapWidth == wrapWidth)
            {
                return;
            }

            AnalyzeWrappedEntry(text, wrapWidth, out var rowCount, out var checkpoints);
            entry.RowCount = rowCount;
            entry.WrapRowCheckpointStarts = checkpoints;
            entry.WrapRowBlocks = null;
            entry.WrapRowBlockAccessStamp = 0;
            entry.CachedWrapWidth = wrapWidth;
        }

        private CachedWrapRowBlock GetWrapRowBlock(int entryIndex, ReadOnlySpan<char> text, int wrapWidth, int targetRowInEntry, out int blockStartRow, out int blockRowCount)
        {
            ref var entry = ref _entryLayouts[entryIndex];
            var blockSlot = EnsureWrapRowBlock(ref entry, text, wrapWidth, targetRowInEntry);
            var block = entry.WrapRowBlocks![blockSlot];
            blockStartRow = block.StartRow;
            blockRowCount = block.RowCount;
            return block;
        }

        private int EnsureWrapRowBlock(ref CachedEntryLayout entry, ReadOnlySpan<char> text, int wrapWidth, int targetRowInEntry)
        {
            var blockIndex = targetRowInEntry / WrapRowBlockSize;
            var nextAccessStamp = unchecked(entry.WrapRowBlockAccessStamp + 1);
            entry.WrapRowBlockAccessStamp = nextAccessStamp == 0 ? 1 : nextAccessStamp;

            var blocks = entry.WrapRowBlocks;
            if (blocks is null)
            {
                blocks = new CachedWrapRowBlock[WrapRowBlockCacheCapacity];
                entry.WrapRowBlocks = blocks;
            }

            for (var i = 0; i < blocks.Length; i++)
            {
                if (blocks[i].Starts is null || blocks[i].BlockIndex != blockIndex)
                {
                    continue;
                }

                blocks[i].AccessStamp = entry.WrapRowBlockAccessStamp;
                return i;
            }

            var blockSlot = FindWrapRowBlockSlot(blocks);
            ref var block = ref blocks[blockSlot];
            block.Starts ??= new int[WrapRowBlockSize];
            block.Ends ??= new int[WrapRowBlockSize];

            PopulateWrapRowBlock(ref entry, ref block, text, wrapWidth, blockIndex);
            block.AccessStamp = entry.WrapRowBlockAccessStamp;
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

        private static void PopulateWrapRowBlock(ref CachedEntryLayout entry, ref CachedWrapRowBlock block, ReadOnlySpan<char> text, int wrapWidth, int blockIndex)
        {
            var rowStart = Math.Max(0, blockIndex * WrapRowBlockSize);
            var checkpointIndex = rowStart / WrapRowCheckpointStride;
            var checkpointStarts = entry.WrapRowCheckpointStarts!;
            var currentRow = checkpointIndex * WrapRowCheckpointStride;
            var segmentStart = checkpointStarts[Math.Min(checkpointIndex, checkpointStarts.Length - 1)];
            var starts = block.Starts!;
            var ends = block.Ends!;

            while (currentRow < rowStart && segmentStart < text.Length)
            {
                if (!TryGetNextWrapSlice(text, segmentStart, wrapWidth, out _, out var nextStart))
                {
                    break;
                }

                currentRow++;
                segmentStart = nextStart;
            }

            var rowsInBlock = Math.Max(1, Math.Min(WrapRowBlockSize, entry.RowCount - rowStart));

            for (var i = 0; i < rowsInBlock; i++)
            {
                starts[i] = segmentStart;

                if (segmentStart < text.Length && TryGetNextWrapSlice(text, segmentStart, wrapWidth, out var end, out var nextStart))
                {
                    ends[i] = end;
                    segmentStart = nextStart;
                }
                else
                {
                    ends[i] = segmentStart;
                }
            }

            block.BlockIndex = blockIndex;
            block.StartRow = rowStart;
            block.RowCount = rowsInBlock;
        }

        private static bool TryFindWrapSegmentInCachedBlocks(ref CachedEntryLayout entry, int textIndex, out WrapSegmentInfo segment)
        {
            var blocks = entry.WrapRowBlocks;
            if (blocks is null || blocks.Length == 0)
            {
                segment = default;
                return false;
            }

            for (var blockIndex = 0; blockIndex < blocks.Length; blockIndex++)
            {
                ref var block = ref blocks[blockIndex];
                var starts = block.Starts;
                var ends = block.Ends;
                if (starts is null || ends is null || block.RowCount <= 0)
                {
                    continue;
                }

                var blockStartIndex = starts[0];
                var blockEndIndex = ends[block.RowCount - 1];
                if (textIndex < blockStartIndex || textIndex > blockEndIndex)
                {
                    continue;
                }

                var low = 0;
                var high = block.RowCount - 1;
                while (low <= high)
                {
                    var mid = low + ((high - low) >> 1);
                    var start = starts[mid];
                    var end = ends[mid];
                    if (textIndex < start)
                    {
                        high = mid - 1;
                        continue;
                    }

                    if (textIndex > end || (textIndex == end && blockEndIndex > end && mid < block.RowCount - 1))
                    {
                        low = mid + 1;
                        continue;
                    }

                    block.AccessStamp = entry.WrapRowBlockAccessStamp;
                    segment = new WrapSegmentInfo(block.StartRow + mid, start, end - start);
                    return true;
                }
            }

            segment = default;
            return false;
        }

        private static int FindCheckpointIndex(ReadOnlySpan<int> checkpointStarts, int indexInEntry)
        {
            var low = 0;
            var high = checkpointStarts.Length - 1;
            while (low <= high)
            {
                var mid = low + ((high - low) >> 1);
                var start = checkpointStarts[mid];
                if (start == indexInEntry)
                {
                    return mid;
                }

                if (start < indexInEntry)
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
        {
            var max = 0;
            foreach (var width in _widthHistogram.Keys)
            {
                max = width;
            }

            _extentWidth = max;
        }
    }
}
