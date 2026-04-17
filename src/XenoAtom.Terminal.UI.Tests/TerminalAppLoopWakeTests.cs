// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal;
using XenoAtom.Terminal.Backends;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Hosting;
using XenoAtom.Terminal.UI.Threading;
using System.Reflection;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class TerminalAppLoopWakeTests
{
    [TestMethod]
    public async Task Run_WakesFromPost_WithoutWaitingForPollingTimeout()
    {
        var backend = new InMemoryTerminalBackend(new TerminalSize(30, 10));
        using var session = Terminal.Open(backend, new TerminalOptions { ImplicitStartInput = true }, force: true);
        var waitBackend = new ProbeWaitBackend();
        await using var app = CreateApp(session.Instance, waitBackend);

        var posted = new ManualResetEventSlim();
        var runTask = Task.Run(() => app.Run());

        Assert.IsTrue(waitBackend.WaitEntered.Wait(TimeSpan.FromSeconds(2)), "The app did not enter the blocking wait.");

        app.Post(() =>
        {
            posted.Set();
            app.Stop();
        });

        await runTask.WaitAsync(TimeSpan.FromSeconds(2));

        Assert.IsTrue(posted.IsSet, "The posted action was not executed on the UI thread.");
        Assert.IsTrue(waitBackend.WakeSignalCount >= 1, "Expected the wait backend to be interrupted by the wake signal.");
    }

    [TestMethod]
    public async Task Run_WakesFromRequestRender_AndReturnsToIdleWait()
    {
        var backend = new InMemoryTerminalBackend(new TerminalSize(30, 10));
        using var session = Terminal.Open(backend, new TerminalOptions { ImplicitStartInput = true }, force: true);
        var waitBackend = new ProbeWaitBackend();
        await using var app = CreateApp(session.Instance, waitBackend);

        var runTask = Task.Run(() => app.Run());

        Assert.IsTrue(waitBackend.WaitEntered.Wait(TimeSpan.FromSeconds(2)), "The app did not enter the blocking wait.");

        RequestRender(app);

        Assert.IsTrue(
            SpinWait.SpinUntil(() => waitBackend.WaitCount >= 2, TimeSpan.FromSeconds(2)),
            "Expected RequestRender() to wake the blocking loop and let it return to the idle wait.");

        app.Stop();
        await runTask.WaitAsync(TimeSpan.FromSeconds(2));
    }

    private static void RequestRender(TerminalApp app)
    {
        typeof(TerminalApp)
            .GetMethod("RequestRender", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(app, []);
    }

    [TestMethod]
    public async Task Run_WakesFromRequestAnimation_Immediately()
    {
        var backend = new InMemoryTerminalBackend(new TerminalSize(30, 10));
        using var session = Terminal.Open(backend, new TerminalOptions { ImplicitStartInput = true }, force: true);
        var waitBackend = new ProbeWaitBackend();
        await using var app = CreateApp(session.Instance, waitBackend);

        var runTask = Task.Run(() => app.Run());

        Assert.IsTrue(waitBackend.WaitEntered.Wait(TimeSpan.FromSeconds(2)), "The app did not enter the blocking wait.");

        app.RequestAnimation();

        Assert.IsTrue(
            SpinWait.SpinUntil(() => waitBackend.WaitCount >= 2, TimeSpan.FromSeconds(2)),
            "Expected RequestAnimation() to wake the blocking loop immediately.");

        app.Stop();
        await runTask.WaitAsync(TimeSpan.FromSeconds(2));
    }

    [TestMethod]
    public void Run_ToleratesUpdateOverruns_WithoutBreakingTheLoop()
    {
        var backend = new InMemoryTerminalBackend(new TerminalSize(30, 10));
        using var session = Terminal.Open(backend, new TerminalOptions { ImplicitStartInput = true }, force: true);
        var clock = new MutableLoopClock();
        var waitBackend = new ImmediateDeadlineWaitBackend(clock);
        var app = CreateApp(session.Instance, clock, waitBackend);

        try
        {
            var tickCount = 0;
            app.SetUpdateCallback(_ =>
            {
                tickCount++;
                clock.Advance(TimeSpan.FromMilliseconds(20).Ticks);
                return tickCount >= 2 ? TerminalLoopResult.Stop : TerminalLoopResult.Continue;
            });

            app.Run();

            Assert.AreEqual(2, tickCount);
            Assert.AreEqual(0, waitBackend.DeadlineWaitCount, "Expected an over-budget tick to skip waiting instead of replaying missed frames.");
        }
        finally
        {
            app.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }
    }

    [TestMethod]
    public void Run_ActiveCadence_UsesWholeFrameBudget_IncludingUpdateWork()
    {
        var backend = new InMemoryTerminalBackend(new TerminalSize(30, 10));
        using var session = Terminal.Open(backend, new TerminalOptions { ImplicitStartInput = true }, force: true);
        var clock = new MutableLoopClock();
        var waitBackend = new ImmediateDeadlineWaitBackend(clock);
        var app = CreateApp(session.Instance, clock, waitBackend);

        try
        {
            var tickCount = 0;
            app.SetUpdateCallback(_ =>
            {
                tickCount++;
                clock.Advance(TimeSpan.FromMilliseconds(5).Ticks);
                return tickCount >= 2 ? TerminalLoopResult.Stop : TerminalLoopResult.Continue;
            });

            app.Run();

            Assert.AreEqual(2, tickCount);
            Assert.AreEqual(TimeSpan.FromMilliseconds(20).Ticks, clock.GetTimestamp(), "Expected 5ms of work plus a 15ms cadence budget across two ticks.");
            Assert.AreEqual(1, waitBackend.DeadlineWaitCount);
        }
        finally
        {
            app.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }
    }

    private static TerminalApp CreateApp(TerminalInstance terminal, ITerminalLoopWaitBackend waitBackend)
    {
        return new TerminalApp(
            new TextBlock("Wake test"),
            terminal,
            new TerminalAppOptions { HostKind = TerminalHostKind.Inline, LoopMode = TerminalLoopMode.Auto },
            loopClock: ConstantLoopClock.Instance,
            waitBackend);
    }

    private static TerminalApp CreateApp(TerminalInstance terminal, ITerminalLoopClock clock, ITerminalLoopWaitBackend waitBackend)
    {
        return new TerminalApp(
            new TextBlock("Wake test"),
            terminal,
            new TerminalAppOptions { HostKind = TerminalHostKind.Inline, LoopMode = TerminalLoopMode.Auto },
            clock,
            waitBackend);
    }

    private sealed class ConstantLoopClock : ITerminalLoopClock
    {
        public static readonly ConstantLoopClock Instance = new();

        public long Frequency => TimeSpan.TicksPerSecond;

        public long GetTimestamp() => 0;
    }

    private sealed class ProbeWaitBackend : ITerminalLoopWaitBackend
    {
        public readonly ManualResetEventSlim WaitEntered = new();
        private int _waitCount;
        private int _wakeSignalCount;

        public int WaitCount => Volatile.Read(ref _waitCount);

        public int WakeSignalCount => Volatile.Read(ref _wakeSignalCount);

        public TerminalLoopWaitResult WaitUntil(long deadline, AutoResetEvent wakeSignal, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _waitCount);
            WaitEntered.Set();

            var signaled = WaitHandle.WaitAny([wakeSignal, cancellationToken.WaitHandle], TimeSpan.FromSeconds(5));
            if (signaled == 0)
            {
                Interlocked.Increment(ref _wakeSignalCount);
                return TerminalLoopWaitResult.WakeSignal;
            }

            return signaled == 1 ? TerminalLoopWaitResult.Canceled : TerminalLoopWaitResult.Deadline;
        }

        public TerminalLoopWaitDiagnostics GetDiagnosticsSnapshot() => default;

        public void Dispose()
        {
            WaitEntered.Dispose();
        }
    }

    private sealed class MutableLoopClock : ITerminalLoopClock
    {
        private long _timestamp;

        public long Frequency => TimeSpan.TicksPerSecond;

        public long GetTimestamp() => Volatile.Read(ref _timestamp);

        public void Advance(long ticks) => Interlocked.Add(ref _timestamp, ticks);
    }

    private sealed class ImmediateDeadlineWaitBackend(MutableLoopClock clock) : ITerminalLoopWaitBackend
    {
        private int _deadlineWaitCount;

        public int DeadlineWaitCount => Volatile.Read(ref _deadlineWaitCount);

        public TerminalLoopWaitResult WaitUntil(long deadline, AutoResetEvent wakeSignal, CancellationToken cancellationToken)
        {
            if (deadline != long.MaxValue)
            {
                clock.Advance(Math.Max(0, deadline - clock.GetTimestamp()));
            }

            Interlocked.Increment(ref _deadlineWaitCount);
            return cancellationToken.IsCancellationRequested ? TerminalLoopWaitResult.Canceled : TerminalLoopWaitResult.Deadline;
        }

        public TerminalLoopWaitDiagnostics GetDiagnosticsSnapshot() => default;

        public void Dispose()
        {
        }
    }
}
