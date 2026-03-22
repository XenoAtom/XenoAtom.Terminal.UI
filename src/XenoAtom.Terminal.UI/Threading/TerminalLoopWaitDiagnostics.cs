// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI.Threading;

[Flags]
internal enum TerminalLoopWakeReason
{
    None = 0,
    Deadline = 1 << 0,
    Post = 1 << 1,
    Render = 1 << 2,
    Animation = 1 << 3,
    Input = 1 << 4,
    AsyncUpdate = 1 << 5,
    Shutdown = 1 << 6,
}

internal readonly record struct TerminalLoopWaitDiagnostics(
    string BackendName,
    long YieldWindowTicks,
    long AverageOvershootTicks,
    long P95OvershootTicks,
    long AverageYieldTicks);

internal sealed class TerminalLoopWaitTelemetry
{
    private const int SampleWindowSize = 64;

    private readonly string _backendName;
    private readonly long _minYieldWindowTicks;
    private readonly long _maxYieldWindowTicks;
    private readonly long[] _overshootSamples = new long[SampleWindowSize];
    private readonly long[] _yieldSamples = new long[SampleWindowSize];

    private int _overshootCount;
    private int _overshootIndex;
    private int _yieldCount;
    private int _yieldIndex;
    private long _yieldWindowTicks;

    public TerminalLoopWaitTelemetry(string backendName, long frequency)
    {
        ArgumentException.ThrowIfNullOrEmpty(backendName);

        _backendName = backendName;
        _minYieldWindowTicks = TerminalLoopScheduler.ToStopwatchTicks(TimeSpan.FromMilliseconds(0.25), frequency);
        _maxYieldWindowTicks = TerminalLoopScheduler.ToStopwatchTicks(TimeSpan.FromMilliseconds(2), frequency);
        _yieldWindowTicks = _maxYieldWindowTicks;
    }

    public long YieldWindowTicks => _yieldWindowTicks;

    public void RecordOvershoot(long ticks)
    {
        ticks = Math.Max(0, ticks);
        RecordSample(_overshootSamples, ref _overshootCount, ref _overshootIndex, ticks);

        var nextYieldWindow = ((_yieldWindowTicks * 7) + ticks) / 8;
        _yieldWindowTicks = Math.Clamp(nextYieldWindow, _minYieldWindowTicks, _maxYieldWindowTicks);
    }

    public void RecordYieldDuration(long ticks)
    {
        ticks = Math.Max(0, ticks);
        RecordSample(_yieldSamples, ref _yieldCount, ref _yieldIndex, ticks);
    }

    public TerminalLoopWaitDiagnostics GetSnapshot()
    {
        return new TerminalLoopWaitDiagnostics(
            _backendName,
            _yieldWindowTicks,
            ComputeAverage(_overshootSamples, _overshootCount),
            ComputePercentile95(_overshootSamples, _overshootCount),
            ComputeAverage(_yieldSamples, _yieldCount));
    }

    private static void RecordSample(long[] samples, ref int count, ref int index, long value)
    {
        samples[index] = value;
        index = (index + 1) % samples.Length;
        if (count < samples.Length)
        {
            count++;
        }
    }

    private static long ComputeAverage(long[] samples, int count)
    {
        if (count <= 0)
        {
            return 0;
        }

        long total = 0;
        for (var i = 0; i < count; i++)
        {
            total += samples[i];
        }

        return total / count;
    }

    private static long ComputePercentile95(long[] samples, int count)
    {
        if (count <= 0)
        {
            return 0;
        }

        Span<long> scratch = count <= 128 ? stackalloc long[count] : new long[count];
        samples.AsSpan(0, count).CopyTo(scratch);
        scratch.Sort();
        var index = (int)Math.Ceiling(count * 0.95) - 1;
        index = Math.Clamp(index, 0, count - 1);
        return scratch[index];
    }
}
