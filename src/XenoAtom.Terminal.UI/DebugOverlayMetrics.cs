// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Diagnostics;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Rendering;
using XenoAtom.Terminal.UI.Threading;

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

    internal bool SceneHasDirtyRect => _sceneDirtyRectValid;
    internal Rectangle SceneDirtyRect => _sceneDirtyRect;
    internal bool SceneFullRepaint { get; private set; }

    internal bool SceneHasRepaintRect => _sceneRepaintRectValid;
    internal Rectangle SceneRepaintRect => _sceneRepaintRect;

    internal bool OverlayVisible { get; private set; }
    internal bool OverlayComposited { get; private set; }
    internal bool OverlayOnlyFrame { get; private set; }
    internal bool HasOverlayRect => _overlayRectValid;
    internal Rectangle OverlayRect => _overlayRect;
    internal long OverlayRenderTicks { get; private set; }

    internal int DiffOutputChars { get; private set; }
    internal int DiffCellsTouched { get; private set; }
    internal bool DiffForceFull { get; private set; }

    internal double Fps { get; private set; }
    internal long FrameIndex { get; private set; }
    internal int LateDeadlineCount { get; private set; }
    internal int WakeDeadlineCount { get; private set; }
    internal int WakePostCount { get; private set; }
    internal int WakeRenderCount { get; private set; }
    internal int WakeAnimationCount { get; private set; }
    internal int WakeInputCount { get; private set; }
    internal int WakeAsyncUpdateCount { get; private set; }
    internal int WakeShutdownCount { get; private set; }

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

    private bool _sceneDirtyRectValid;
    private Rectangle _sceneDirtyRect;

    private bool _sceneRepaintRectValid;
    private Rectangle _sceneRepaintRect;

    private bool _overlayRectValid;
    private Rectangle _overlayRect;

    private long _lastFrameTimestamp;
    private readonly TerminalLoopWakeReason[] _wakeSamples = new TerminalLoopWakeReason[64];
    private int _wakeSampleCount;
    private int _wakeSampleIndex;

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
        SceneFullRepaint = false;

        RenderMeasureTicks = 0;
        RenderArrangeTicks = 0;
        RenderTreeTicks = 0;
        RenderHostTicks = 0;
        RenderTotalTicks = 0;
        OverlayVisible = false;
        OverlayComposited = false;
        OverlayOnlyFrame = false;

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

        _sceneDirtyRectValid = false;
        _sceneDirtyRect = default;

        _sceneRepaintRectValid = false;
        _sceneRepaintRect = default;

        _overlayRectValid = false;
        _overlayRect = default;
    }

    public void SetSceneFullRepaint(bool fullRepaint) => SceneFullRepaint = fullRepaint;

    public void SetSceneRepaintRect(in Rectangle rect)
    {
        _sceneRepaintRect = rect;
        _sceneRepaintRectValid = rect.Width > 0 && rect.Height > 0;
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

    public void AddSceneDirtyRect(in Rectangle rect)
    {
        if (rect.Width <= 0 || rect.Height <= 0)
        {
            return;
        }

        if (!_sceneDirtyRectValid)
        {
            _sceneDirtyRect = rect;
            _sceneDirtyRectValid = true;
            return;
        }

        _sceneDirtyRect = Rectangle.Union(_sceneDirtyRect, rect);
    }

    public void SetOverlayFrame(bool visible, bool overlayOnlyFrame)
    {
        OverlayVisible = visible;
        OverlayOnlyFrame = visible && overlayOnlyFrame;
        OverlayComposited = false;
        _overlayRectValid = false;
        _overlayRect = default;
    }

    public void RecordOverlayComposition(in Rectangle rect, long renderTicks)
    {
        OverlayComposited = rect.Width > 0 && rect.Height > 0;
        OverlayRenderTicks = renderTicks;
        _overlayRect = rect;
        _overlayRectValid = OverlayComposited;
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

    public void RecordWake(TerminalLoopWakeReason wakeReason)
    {
        _wakeSamples[_wakeSampleIndex] = wakeReason;
        _wakeSampleIndex = (_wakeSampleIndex + 1) % _wakeSamples.Length;
        if (_wakeSampleCount < _wakeSamples.Length)
        {
            _wakeSampleCount++;
        }

        RecomputeWakeCounters();
    }

    public void RecordLateDeadline(long latenessTicks)
    {
        if (latenessTicks <= 0)
        {
            return;
        }

        LateDeadlineCount++;
    }

    void ICellBufferDiffMetricsSink.OnRendered(CellBufferDiffMetrics metrics)
    {
        DiffOutputChars = metrics.OutputChars;
        DiffCellsTouched = metrics.CellsTouched;
        DiffForceFull = metrics.ForceFull;
    }

    private long _tickStartTimestamp;

    private void RecomputeWakeCounters()
    {
        WakeDeadlineCount = 0;
        WakePostCount = 0;
        WakeRenderCount = 0;
        WakeAnimationCount = 0;
        WakeInputCount = 0;
        WakeAsyncUpdateCount = 0;
        WakeShutdownCount = 0;

        for (var i = 0; i < _wakeSampleCount; i++)
        {
            var wakeReason = _wakeSamples[i];
            if ((wakeReason & TerminalLoopWakeReason.Deadline) != 0) WakeDeadlineCount++;
            if ((wakeReason & TerminalLoopWakeReason.Post) != 0) WakePostCount++;
            if ((wakeReason & TerminalLoopWakeReason.Render) != 0) WakeRenderCount++;
            if ((wakeReason & TerminalLoopWakeReason.Animation) != 0) WakeAnimationCount++;
            if ((wakeReason & TerminalLoopWakeReason.Input) != 0) WakeInputCount++;
            if ((wakeReason & TerminalLoopWakeReason.AsyncUpdate) != 0) WakeAsyncUpdateCount++;
            if ((wakeReason & TerminalLoopWakeReason.Shutdown) != 0) WakeShutdownCount++;
        }
    }
}
