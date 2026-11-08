// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI.Tests;

/// <summary>
/// A clock that only moves when <see cref="Advance"/> is called, with a configurable frequency. The default frequency (1 kHz)
/// differs from <c>Stopwatch.Frequency</c> on every platform, so a conversion that still assumes the Stopwatch fails.
/// </summary>
internal sealed class ManualTimeProvider(long frequency = ManualTimeProvider.DefaultFrequency) : TimeProvider
{
    public const long DefaultFrequency = 1000;

    private long _timestamp = 1_000_000;

    public override long TimestampFrequency => frequency;

    public override long GetTimestamp() => Interlocked.Read(ref _timestamp);

    public void Advance(TimeSpan time) => Interlocked.Add(ref _timestamp, (long)(time.TotalSeconds * frequency));
}
