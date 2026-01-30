// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Diagnostics;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Rendering;

namespace XenoAtom.Terminal.UI;

internal sealed class DebugOverlayMetrics : ICellBufferDiffMetricsSink
{
    internal readonly struct StageStats
    {
        public readonly int Calls;
        public readonly long Ticks;

        public StageStats(int calls, long ticks)
        {
            Calls = calls;
            Ticks = ticks;
        }
    }

    internal long TickUserUpdateTicks;
    internal long TickTotalTicks;

    internal long RenderMeasureTicks;
    internal long RenderArrangeTicks;
    internal long RenderTreeTicks;
    internal long RenderHostTicks;
    internal long RenderTotalTicks;

    internal StageStats DynamicUpdate => new(_dynamicUpdateCalls, _dynamicUpdateTicks);
    internal StageStats PrepareChildren => new(_prepareChildrenCalls, _prepareChildrenTicks);
    internal StageStats Measure => new(_measureCalls, _measureTicks);
    internal StageStats Arrange => new(_arrangeCalls, _arrangeTicks);
    internal StageStats RenderOverride => new(_renderOverrideCalls, _renderOverrideTicks);

    internal int MeasureCacheHits => _measureCacheHits;
    internal int ArrangeCacheHits => _arrangeCacheHits;
    internal int RenderClipSkips => _renderClipSkips;

    internal bool HasDirtyRect => _dirtyRectValid;
    internal Rectangle DirtyRect => _dirtyRect;
    internal bool FullRepaint { get; private set; }

    internal bool HasRepaintRect => _repaintRectValid;
    internal Rectangle RepaintRect => _repaintRect;

    internal int DiffOutputChars { get; private set; }
    internal int DiffCellsTouched { get; private set; }
    internal bool DiffForceFull { get; private set; }

    internal double Fps { get; private set; }
    internal long FrameIndex { get; private set; }

    private int _dynamicUpdateCalls;
    private long _dynamicUpdateTicks;

    private int _prepareChildrenCalls;
    private long _prepareChildrenTicks;

    private int _measureCalls;
    private int _measureCacheHits;
    private long _measureTicks;

    private int _arrangeCalls;
    private int _arrangeCacheHits;
    private long _arrangeTicks;

    private int _renderOverrideCalls;
    private int _renderClipSkips;
    private long _renderOverrideTicks;

    private bool _dirtyRectValid;
    private Rectangle _dirtyRect;

    private bool _repaintRectValid;
    private Rectangle _repaintRect;

    private long _lastFrameTimestamp;

    public void BeginTick(long timestamp)
    {
        _tickStartTimestamp = timestamp != 0 ? timestamp : Stopwatch.GetTimestamp();
    }

    public void EndTick(long timestamp, long userUpdateTicks)
    {
        TickUserUpdateTicks = userUpdateTicks;

        if (_tickStartTimestamp == 0)
        {
            TickTotalTicks = 0;
            return;
        }

        TickTotalTicks = Math.Max(0, timestamp - _tickStartTimestamp);
        _tickStartTimestamp = 0;
    }

    public void BeginRenderFrame(long frameIndex)
    {
        FrameIndex = frameIndex;
        FullRepaint = false;

        RenderMeasureTicks = 0;
        RenderArrangeTicks = 0;
        RenderTreeTicks = 0;
        RenderHostTicks = 0;
        RenderTotalTicks = 0;

        _dynamicUpdateCalls = 0;
        _dynamicUpdateTicks = 0;
        _prepareChildrenCalls = 0;
        _prepareChildrenTicks = 0;
        _measureCalls = 0;
        _measureCacheHits = 0;
        _measureTicks = 0;
        _arrangeCalls = 0;
        _arrangeCacheHits = 0;
        _arrangeTicks = 0;
        _renderOverrideCalls = 0;
        _renderClipSkips = 0;
        _renderOverrideTicks = 0;

        _dirtyRectValid = false;
        _dirtyRect = default;

        _repaintRectValid = false;
        _repaintRect = default;
    }

    public void SetFullRepaint(bool fullRepaint) => FullRepaint = fullRepaint;

    public void SetRepaintRect(in Rectangle rect)
    {
        _repaintRect = rect;
        _repaintRectValid = rect.Width > 0 && rect.Height > 0;
    }

    public void EndRenderFrame(long startTimestamp, long endTimestamp)
    {
        RenderTotalTicks = Math.Max(0, endTimestamp - startTimestamp);

        if (_lastFrameTimestamp != 0)
        {
            var dt = Math.Max(1, endTimestamp - _lastFrameTimestamp);
            Fps = Stopwatch.Frequency / (double)dt;
        }

        _lastFrameTimestamp = endTimestamp;
    }

    public void AddDirtyRect(in Rectangle rect)
    {
        if (rect.Width <= 0 || rect.Height <= 0)
        {
            return;
        }

        if (!_dirtyRectValid)
        {
            _dirtyRect = rect;
            _dirtyRectValid = true;
            return;
        }

        _dirtyRect = Rectangle.Union(_dirtyRect, rect);
    }

    public void RecordDynamicUpdate(long elapsedTicks)
    {
        _dynamicUpdateCalls++;
        _dynamicUpdateTicks += elapsedTicks;
    }

    public void RecordPrepareChildren(long elapsedTicks)
    {
        _prepareChildrenCalls++;
        _prepareChildrenTicks += elapsedTicks;
    }

    public void RecordMeasure(long elapsedTicks)
    {
        _measureCalls++;
        _measureTicks += elapsedTicks;
    }

    public void RecordMeasureCacheHit() => _measureCacheHits++;

    public void RecordArrange(long elapsedTicks)
    {
        _arrangeCalls++;
        _arrangeTicks += elapsedTicks;
    }

    public void RecordArrangeCacheHit() => _arrangeCacheHits++;

    public void RecordRenderOverride(long elapsedTicks)
    {
        _renderOverrideCalls++;
        _renderOverrideTicks += elapsedTicks;
    }

    public void RecordRenderClipSkip() => _renderClipSkips++;

    void ICellBufferDiffMetricsSink.OnRendered(CellBufferDiffMetrics metrics)
    {
        DiffOutputChars = metrics.OutputChars;
        DiffCellsTouched = metrics.CellsTouched;
        DiffForceFull = metrics.ForceFull;
    }

    private long _tickStartTimestamp;
}
