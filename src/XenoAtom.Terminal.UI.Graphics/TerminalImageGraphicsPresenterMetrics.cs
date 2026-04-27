// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Diagnostics;
using XenoAtom.Terminal;
using XenoAtom.Terminal.Graphics;

namespace XenoAtom.Terminal.UI.Graphics;

/// <summary>
/// Exposes runtime diagnostics collected by <see cref="TerminalImageGraphicsPresenter"/>.
/// </summary>
public sealed class TerminalImageGraphicsPresenterMetrics
{
    private readonly Stopwatch _uptime = Stopwatch.StartNew();
    private TimeSpan _totalEncodeDuration;
    private TimeSpan _lastPresentationDuration;
    private TimeSpan _lastEncodeDuration;

    /// <summary>
    /// Gets the number of presentation passes attempted.
    /// </summary>
    public long PresentationCount { get; private set; }

    /// <summary>
    /// Gets the number of image commands encoded and presented.
    /// </summary>
    public long EncodedFrameCount { get; private set; }

    /// <summary>
    /// Gets the number of real-time source frame versions skipped between presented frames.
    /// </summary>
    public long DroppedFrameCount { get; private set; }

    /// <summary>
    /// Gets the cumulative encoded terminal payload bytes produced by presented images.
    /// </summary>
    public long PayloadByteCount { get; private set; }

    /// <summary>
    /// Gets the total time spent waiting for image encoding during presentation passes.
    /// </summary>
    public TimeSpan TotalEncodeDuration => _totalEncodeDuration;

    /// <summary>
    /// Gets the number of graphics commands observed in the latest presentation pass.
    /// </summary>
    public int LastCommandCount { get; private set; }

    /// <summary>
    /// Gets the duration of the latest presentation pass.
    /// </summary>
    public TimeSpan LastPresentationDuration => _lastPresentationDuration;

    /// <summary>
    /// Gets the number of image commands encoded and presented in the latest presentation pass.
    /// </summary>
    public int LastEncodedFrameCount { get; private set; }

    /// <summary>
    /// Gets the number of real-time source frame versions skipped during the latest presentation pass.
    /// </summary>
    public long LastDroppedFrameCount { get; private set; }

    /// <summary>
    /// Gets the encoded terminal payload bytes produced during the latest presentation pass.
    /// </summary>
    public long LastPayloadByteCount { get; private set; }

    /// <summary>
    /// Gets the time spent waiting for image encoding during the latest presentation pass.
    /// </summary>
    public TimeSpan LastEncodeDuration => _lastEncodeDuration;

    /// <summary>
    /// Gets the cumulative number of encoded-image cache hits.
    /// </summary>
    public long CacheHitCount { get; private set; }

    /// <summary>
    /// Gets the cumulative number of encoded-image cache misses.
    /// </summary>
    public long CacheMissCount { get; private set; }

    /// <summary>
    /// Gets the cumulative number of encoded-image cache stores.
    /// </summary>
    public long CacheStoreCount { get; private set; }

    /// <summary>
    /// Gets the number of encoded-image cache hits during the latest presentation pass.
    /// </summary>
    public long LastCacheHitCount { get; private set; }

    /// <summary>
    /// Gets the number of encoded-image cache misses during the latest presentation pass.
    /// </summary>
    public long LastCacheMissCount { get; private set; }

    /// <summary>
    /// Gets the number of encoded-image cache stores during the latest presentation pass.
    /// </summary>
    public long LastCacheStoreCount { get; private set; }

    /// <summary>
    /// Gets the average encode duration per encoded frame.
    /// </summary>
    public TimeSpan AverageEncodeDuration => EncodedFrameCount == 0
        ? TimeSpan.Zero
        : TimeSpan.FromTicks(_totalEncodeDuration.Ticks / EncodedFrameCount);

    /// <summary>
    /// Gets the effective presented image frame rate since the last reset.
    /// </summary>
    public double EffectiveFramesPerSecond
        => _uptime.Elapsed.TotalSeconds <= 0.0 ? 0.0 : EncodedFrameCount / _uptime.Elapsed.TotalSeconds;

    /// <summary>
    /// Resets all collected counters.
    /// </summary>
    public void Reset()
    {
        PresentationCount = 0;
        EncodedFrameCount = 0;
        DroppedFrameCount = 0;
        PayloadByteCount = 0;
        _totalEncodeDuration = TimeSpan.Zero;
        LastCommandCount = 0;
        LastEncodedFrameCount = 0;
        LastDroppedFrameCount = 0;
        LastPayloadByteCount = 0;
        _lastPresentationDuration = TimeSpan.Zero;
        _lastEncodeDuration = TimeSpan.Zero;
        CacheHitCount = 0;
        CacheMissCount = 0;
        CacheStoreCount = 0;
        LastCacheHitCount = 0;
        LastCacheMissCount = 0;
        LastCacheStoreCount = 0;
        _uptime.Restart();
    }

    internal void RecordPresentation(int commandCount)
    {
        PresentationCount++;
        LastCommandCount = commandCount;
        LastEncodedFrameCount = 0;
        LastDroppedFrameCount = 0;
        LastPayloadByteCount = 0;
        _lastPresentationDuration = TimeSpan.Zero;
        _lastEncodeDuration = TimeSpan.Zero;
        LastCacheHitCount = 0;
        LastCacheMissCount = 0;
        LastCacheStoreCount = 0;
    }

    internal void RecordPresentationDuration(TimeSpan elapsed)
    {
        _lastPresentationDuration = elapsed;
    }

    internal void RecordCacheActivity(long lastHitCount, long lastMissCount, long lastStoreCount)
    {
        LastCacheHitCount = Math.Max(0, lastHitCount);
        LastCacheMissCount = Math.Max(0, lastMissCount);
        LastCacheStoreCount = Math.Max(0, lastStoreCount);
        CacheHitCount += LastCacheHitCount;
        CacheMissCount += LastCacheMissCount;
        CacheStoreCount += LastCacheStoreCount;
    }

    internal void RecordEncodedFrame(TerminalEncodedImage image, TimeSpan encodeDuration)
    {
        EncodedFrameCount++;
        PayloadByteCount += image.PayloadByteLength;
        _totalEncodeDuration += encodeDuration;
        LastEncodedFrameCount++;
        LastPayloadByteCount += image.PayloadByteLength;
        _lastEncodeDuration += encodeDuration;
    }

    internal void RecordDroppedFrames(long count)
    {
        if (count > 0)
        {
            DroppedFrameCount += count;
            LastDroppedFrameCount += count;
        }
    }

    internal TerminalGraphicsPresenterDiagnostics GetDiagnosticsSnapshot(TerminalGraphicsProtocol protocol)
    {
        return new TerminalGraphicsPresenterDiagnostics
        {
            Name = "image",
            Protocol = protocol,
            PresentationCount = PresentationCount,
            LastCommandCount = LastCommandCount,
            LastPresentationDuration = LastPresentationDuration,
            EncodedFrameCount = EncodedFrameCount,
            LastEncodedFrameCount = LastEncodedFrameCount,
            TotalEncodeDuration = TotalEncodeDuration,
            AverageEncodeDuration = AverageEncodeDuration,
            LastEncodeDuration = LastEncodeDuration,
            PayloadByteCount = PayloadByteCount,
            LastPayloadByteCount = LastPayloadByteCount,
            DroppedFrameCount = DroppedFrameCount,
            LastDroppedFrameCount = LastDroppedFrameCount,
            EffectiveFramesPerSecond = EffectiveFramesPerSecond,
            CacheHitCount = CacheHitCount,
            CacheMissCount = CacheMissCount,
            CacheStoreCount = CacheStoreCount,
            LastCacheHitCount = LastCacheHitCount,
            LastCacheMissCount = LastCacheMissCount,
            LastCacheStoreCount = LastCacheStoreCount,
        };
    }
}
