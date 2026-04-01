// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal;
using XenoAtom.Terminal.Backends;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Hosting;
using XenoAtom.Terminal.UI.Threading;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class BindingManagerThreadAccessTests
{
    [TestMethod]
    public async Task NotifyValueChanged_Throws_For_NonDispatcherOwner_Off_The_UI_Thread()
    {
        var backend = new InMemoryTerminalBackend(new TerminalSize(30, 10));
        using var session = Terminal.Open(backend, new TerminalOptions { ImplicitStartInput = true }, force: true);
        var waitBackend = new ProbeWaitBackend();
        await using var app = CreateApp(session.Instance, waitBackend);
        var owner = new PlainBindableOwner();

        var runTask = Task.Run(() => app.Run());

        Assert.IsTrue(waitBackend.WaitEntered.Wait(TimeSpan.FromSeconds(2)), "The app did not enter the blocking wait.");

        var ex = Assert.Throws<InvalidOperationException>(() =>
            BindingManager.Current.NotifyValueChanged(owner, PlainBindableOwner.ValueAccessor));

        StringAssert.Contains(ex.Message, "PlainBindableOwner.Value");
        StringAssert.Contains(ex.Message, "InvokeAsync");

        app.Stop();
        await runTask.WaitAsync(TimeSpan.FromSeconds(2));
    }

    [TestMethod]
    public async Task SetValue_Throws_For_NonDispatcherOwner_Off_The_UI_Thread()
    {
        var backend = new InMemoryTerminalBackend(new TerminalSize(30, 10));
        using var session = Terminal.Open(backend, new TerminalOptions { ImplicitStartInput = true }, force: true);
        var waitBackend = new ProbeWaitBackend();
        await using var app = CreateApp(session.Instance, waitBackend);
        var owner = new PlainBindableOwner();

        var runTask = Task.Run(() => app.Run());

        Assert.IsTrue(waitBackend.WaitEntered.Wait(TimeSpan.FromSeconds(2)), "The app did not enter the blocking wait.");

        var ex = Assert.Throws<InvalidOperationException>(() => owner.SetValue(42));

        StringAssert.Contains(ex.Message, "PlainBindableOwner.Value");
        StringAssert.Contains(ex.Message, "InvokeAsync");
        Assert.AreEqual(0, owner.Value);

        app.Stop();
        await runTask.WaitAsync(TimeSpan.FromSeconds(2));
    }

    private static TerminalApp CreateApp(TerminalInstance terminal, ITerminalLoopWaitBackend waitBackend)
    {
        return new TerminalApp(
            new TextBlock("Thread access"),
            terminal,
            new TerminalAppOptions { HostKind = TerminalHostKind.Inline, LoopMode = TerminalLoopMode.Auto },
            loopClock: ConstantLoopClock.Instance,
            waitBackend);
    }

    private sealed class PlainBindableOwner
    {
        public static readonly BindingAccessor<int> ValueAccessor = new(
            "Value",
            owner => ((PlainBindableOwner)owner).Value,
            (owner, value) => ((PlainBindableOwner)owner).Value = value);

        public int Value;

        public void SetValue(int value)
        {
            BindingManager.Current.SetValue(this, ref Value, value, ValueAccessor);
        }
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

        public TerminalLoopWaitResult WaitUntil(long deadline, AutoResetEvent wakeSignal, CancellationToken cancellationToken)
        {
            WaitEntered.Set();

            var signaled = WaitHandle.WaitAny([wakeSignal, cancellationToken.WaitHandle], TimeSpan.FromSeconds(5));
            if (signaled == 0)
            {
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
}
