// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Threading;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class TerminalLoopSchedulerTests
{
    [TestMethod]
    public void ComputePollingDeadline_ReturnsNow_ForImmediateAnimation()
    {
        var deadline = TerminalLoopScheduler.ComputePollingDeadline(100, animationDeadline: 0, pollingSliceTicks: 25);
        Assert.AreEqual(100, deadline);
    }

    [TestMethod]
    public void ComputePollingDeadline_PrefersAnimationDeadline_WhenSoonerThanPollingSlice()
    {
        var deadline = TerminalLoopScheduler.ComputePollingDeadline(100, animationDeadline: 110, pollingSliceTicks: 25);
        Assert.AreEqual(110, deadline);
    }

    [TestMethod]
    public void ComputePollingDeadline_UsesPollingSlice_WhenNoAnimationDeadline()
    {
        var deadline = TerminalLoopScheduler.ComputePollingDeadline(100, animationDeadline: long.MaxValue, pollingSliceTicks: 25);
        Assert.AreEqual(125, deadline);
    }

    [TestMethod]
    public void ComputePollingDeadline_ReturnsNow_WhenAnimationDeadlineAlreadyExpired()
    {
        var deadline = TerminalLoopScheduler.ComputePollingDeadline(100, animationDeadline: 99, pollingSliceTicks: 25);
        Assert.AreEqual(100, deadline);
    }

    [TestMethod]
    public void ToStopwatchTicks_ClampsSmallPositiveDurationToAtLeastOneTick()
    {
        var ticks = TerminalLoopScheduler.ToStopwatchTicks(TimeSpan.FromTicks(1), frequency: 1);
        Assert.AreEqual(1L, ticks);
    }

    [TestMethod]
    public void ComputeNextActiveDeadline_StartsFromTickStart_NotCurrentTime()
    {
        var deadline = TerminalLoopScheduler.ComputeNextActiveDeadline(
            tickStart: 100,
            now: 105,
            previousDeadline: long.MaxValue,
            activeFrameTicks: 15);

        Assert.AreEqual(115, deadline);
    }

    [TestMethod]
    public void ComputeNextActiveDeadline_AdvancesFromPreviousScheduledDeadline()
    {
        var deadline = TerminalLoopScheduler.ComputeNextActiveDeadline(
            tickStart: 130,
            now: 135,
            previousDeadline: 115,
            activeFrameTicks: 15);

        Assert.AreEqual(145, deadline);
    }

    [TestMethod]
    public void TimeoutWaitBackend_ReturnsWakeSignal_WhenSignalIsAlreadySet()
    {
        using var signal = new AutoResetEvent(initialState: true);
        var waitBackend = new TimeoutTerminalLoopWaitBackend(new FakeClock(100, frequency: 1000));

        var result = waitBackend.WaitUntil(deadline: 200, signal, CancellationToken.None);

        Assert.AreEqual(TerminalLoopWaitResult.WakeSignal, result);
    }

    [TestMethod]
    public void TimeoutWaitBackend_ReturnsCanceled_WhenTokenIsCanceled()
    {
        using var signal = new AutoResetEvent(initialState: false);
        var waitBackend = new TimeoutTerminalLoopWaitBackend(new FakeClock(100, frequency: 1000));
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var result = waitBackend.WaitUntil(deadline: 200, signal, cts.Token);

        Assert.AreEqual(TerminalLoopWaitResult.Canceled, result);
    }

    [TestMethod]
    public void TimeoutWaitBackend_ReturnsDeadline_WhenDeadlineHasAlreadyPassed()
    {
        using var signal = new AutoResetEvent(initialState: false);
        var waitBackend = new TimeoutTerminalLoopWaitBackend(new FakeClock(100, frequency: 1000));

        var result = waitBackend.WaitUntil(deadline: 100, signal, CancellationToken.None);

        Assert.AreEqual(TerminalLoopWaitResult.Deadline, result);
    }

    [TestMethod]
    public void CreateDefaultWaitBackend_ReturnsBackendThatCanWakeFromSignal()
    {
        using var signal = new AutoResetEvent(initialState: true);
        using var waitBackend = TerminalLoopWaitBackendFactory.CreateDefault(new FakeClock(100, frequency: 1000));

        var result = waitBackend.WaitUntil(deadline: 200, signal, CancellationToken.None);

        Assert.AreEqual(TerminalLoopWaitResult.WakeSignal, result);
    }

    private sealed class FakeClock(long timestamp, long frequency) : ITerminalLoopClock
    {
        public long Frequency { get; } = frequency;

        public long GetTimestamp() => timestamp;
    }
}
