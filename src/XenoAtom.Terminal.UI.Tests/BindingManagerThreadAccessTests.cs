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
    public async Task NotifyValueChanged_Throws_For_State_Owned_By_App_Off_The_UI_Thread()
    {
        var backend = new InMemoryTerminalBackend(new TerminalSize(30, 10));
        using var session = Terminal.Open(backend, new TerminalOptions { ImplicitStartInput = true }, force: true);
        var waitBackend = new ProbeWaitBackend();
        var state = new State<int>(0);
        await using var app = CreateApp(session.Instance, waitBackend, state);

        var runTask = Task.Run(() => app.Run());

        Assert.IsTrue(waitBackend.WaitEntered.Wait(TimeSpan.FromSeconds(2)), "The app did not enter the blocking wait.");

        Binding binding = state;
        var ex = Assert.Throws<InvalidOperationException>(() =>
            BindingManager.Current.NotifyValueChanged(binding.Owner, binding.Accessor));

        StringAssert.Contains(ex.Message, ".Value");
        StringAssert.Contains(ex.Message, "InvokeAsync");

        app.Stop();
        await runTask.WaitAsync(TimeSpan.FromSeconds(2));
    }

    [TestMethod]
    public async Task SetValue_Throws_For_State_Owned_By_App_Off_The_UI_Thread()
    {
        var backend = new InMemoryTerminalBackend(new TerminalSize(30, 10));
        using var session = Terminal.Open(backend, new TerminalOptions { ImplicitStartInput = true }, force: true);
        var waitBackend = new ProbeWaitBackend();
        var state = new State<int>(0);
        await using var app = CreateApp(session.Instance, waitBackend, state);

        var runTask = Task.Run(() => app.Run());

        Assert.IsTrue(waitBackend.WaitEntered.Wait(TimeSpan.FromSeconds(2)), "The app did not enter the blocking wait.");

        var ex = Assert.Throws<InvalidOperationException>(() => state.Value = 42);

        StringAssert.Contains(ex.Message, ".Value");
        StringAssert.Contains(ex.Message, "InvokeAsync");

        app.Stop();
        await runTask.WaitAsync(TimeSpan.FromSeconds(2));
        Assert.AreEqual(0, state.Value);
    }

    [TestMethod]
    public async Task SetValue_Through_App_Dispatcher_Updates_State_Owned_By_App()
    {
        var backend = new InMemoryTerminalBackend(new TerminalSize(30, 10));
        using var session = Terminal.Open(backend, new TerminalOptions { ImplicitStartInput = true }, force: true);
        var waitBackend = new ProbeWaitBackend();
        var state = new State<int>(0);
        await using var app = CreateApp(session.Instance, waitBackend, state);

        var runTask = Task.Run(() => app.Run());

        Assert.IsTrue(waitBackend.WaitEntered.Wait(TimeSpan.FromSeconds(2)), "The app did not enter the blocking wait.");

        await app.Dispatcher.InvokeAsync(() => state.Value = 42).WaitAsync(TimeSpan.FromSeconds(2));
        var value = await app.Dispatcher.InvokeAsync(() => state.Value).WaitAsync(TimeSpan.FromSeconds(2));
        Assert.AreEqual(42, value);

        app.Stop();
        await runTask.WaitAsync(TimeSpan.FromSeconds(2));
    }

    [TestMethod]
    public async Task SetValue_Does_Not_Throw_For_NonDispatcherOwner_Off_The_UI_Thread()
    {
        // Plain (non-DispatcherObject) owners carry no thread affinity: with one dispatcher per thread, there is no way to know
        // which app (if any) observes them. Derive view models from DispatcherObject or use State<T> to get thread checks.
        var backend = new InMemoryTerminalBackend(new TerminalSize(30, 10));
        using var session = Terminal.Open(backend, new TerminalOptions { ImplicitStartInput = true }, force: true);
        var waitBackend = new ProbeWaitBackend();
        await using var app = CreateApp(session.Instance, waitBackend, new State<int>(0));
        var owner = new PlainBindableOwner();

        var runTask = Task.Run(() => app.Run());

        Assert.IsTrue(waitBackend.WaitEntered.Wait(TimeSpan.FromSeconds(2)), "The app did not enter the blocking wait.");

        owner.SetValue(42);
        BindingManager.Current.NotifyValueChanged(owner, PlainBindableOwner.ValueAccessor);
        Assert.AreEqual(42, owner.Value);

        app.Stop();
        await runTask.WaitAsync(TimeSpan.FromSeconds(2));
    }

    [TestMethod]
    public async Task State_Ownership_Is_Released_When_App_Stops()
    {
        var backend = new InMemoryTerminalBackend(new TerminalSize(30, 10));
        using var session = Terminal.Open(backend, new TerminalOptions { ImplicitStartInput = true }, force: true);
        var waitBackend = new ProbeWaitBackend();
        var state = new State<int>(0);
        await using var app = CreateApp(session.Instance, waitBackend, state);

        var runTask = Task.Run(() => app.Run());

        Assert.IsTrue(waitBackend.WaitEntered.Wait(TimeSpan.FromSeconds(2)), "The app did not enter the blocking wait.");
        Assert.IsFalse(state.CheckAccess());

        app.Stop();
        await runTask.WaitAsync(TimeSpan.FromSeconds(2));

        Assert.IsTrue(state.CheckAccess());
        state.Value = 42;
        Assert.AreEqual(42, state.Value);
    }

    private static TerminalApp CreateApp(TerminalInstance terminal, ITerminalLoopWaitBackend waitBackend, State<int> state)
    {
        return new TerminalApp(
            new TextBlock(() => $"Thread access {state.Value}"),
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
