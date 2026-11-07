// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.Backends;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Hosting;
using XenoAtom.Terminal.UI.Threading;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class DispatcherLifecycleTests
{
    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void Inactive_App_Dispatcher_Does_Not_Target_Another_Running_App(bool previouslyRun)
    {
        TestThreads.Run(() =>
        {
            using var terminal = CreateTerminal();
            var inactive = CreateApp(new TextBlock("Inactive"), terminal);
            var running = CreateApp(new TextBlock("Running"), terminal);
            if (previouslyRun)
            {
                inactive.BeginRun();
                inactive.Tick();
                inactive.EndRun();
            }

            running.BeginRun();
            try
            {
                var invoked = false;
                Assert.AreNotSame(running.Dispatcher, inactive.Dispatcher);
                Assert.Throws<InvalidOperationException>(() => inactive.Dispatcher.Post(() => invoked = true));
                Assert.Throws<InvalidOperationException>(() => inactive.Dispatcher.InvokeAsync(() => invoked = true));
                running.Tick();
                Assert.IsFalse(invoked);
            }
            finally
            {
                running.EndRun();
                running.DisposeAsync().AsTask().GetAwaiter().GetResult();
                inactive.DisposeAsync().AsTask().GetAwaiter().GetResult();
            }
        });
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void Completed_Run_Context_Posts_To_ThreadPool_Not_The_Next_App(bool startNextApp)
    {
        TestThreads.Run(() =>
        {
            using var terminal = CreateTerminal();
            var app = CreateApp(new TextBlock("First"), terminal);
            app.BeginRun();
            var context = SynchronizationContext.Current!;
            var appThreadId = Environment.CurrentManagedThreadId;
            app.EndRun();

            var next = CreateApp(new TextBlock("Next"), terminal);
            if (startNextApp)
            {
                next.BeginRun();
            }

            try
            {
                var completed = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
                context.Post(_ => completed.TrySetResult(Environment.CurrentManagedThreadId), null);
                if (startNextApp)
                {
                    next.Tick();
                }

                var resumedOn = completed.Task.WaitAsync(TestThreads.Timeout).GetAwaiter().GetResult();
                Assert.AreNotEqual(appThreadId, resumedOn);
            }
            finally
            {
                if (startNextApp)
                {
                    next.EndRun();
                }
                next.DisposeAsync().AsTask().GetAwaiter().GetResult();
                app.DisposeAsync().AsTask().GetAwaiter().GetResult();
            }
        });
    }

    [TestMethod]
    public void Queued_Continuation_Is_Released_When_Run_Ends_And_Is_Not_Repeated()
    {
        TestThreads.Run(() =>
        {
            using var terminal = CreateTerminal();
            var state = new State<int>(1);
            var app = CreateApp(new TextBlock(() => $"Queue {state.Value}"), terminal);
            var completed = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            var calls = 0;
            app.BeginRun();
            app.Tick();
            var appThreadId = Environment.CurrentManagedThreadId;
            SynchronizationContext.Current!.Post(_ =>
            {
                try
                {
                    state.Value = 2;
                    Interlocked.Increment(ref calls);
                    completed.TrySetResult(Environment.CurrentManagedThreadId);
                }
                catch (Exception exception)
                {
                    completed.TrySetException(exception);
                }
            }, null);
            app.EndRun();

            try
            {
                // No frame processes the queued continuation: ending the run must release it itself.
                var resumedOn = completed.Task.WaitAsync(TimeSpan.FromSeconds(2)).GetAwaiter().GetResult();
                Assert.AreNotEqual(appThreadId, resumedOn);
                app.BeginRun();
                try
                {
                    app.Tick();
                    Assert.AreEqual(1, Volatile.Read(ref calls));
                }
                finally
                {
                    app.EndRun();
                }
            }
            finally
            {
                app.DisposeAsync().AsTask().GetAwaiter().GetResult();
            }
        });
    }

    [TestMethod]
    public void Cleanup_Failure_Still_Detaches_Root_And_Binding_Handler()
    {
        TestThreads.Run(() =>
        {
            using var terminal = CreateTerminal();
            var state = new State<int>(1);
            var root = new TextBlock(() => $"Value {state.Value}");
            var presenter = new ThrowingResetPresenter();
            var app = new TerminalApp(root, terminal, new TerminalAppOptions
            {
                HostKind = TerminalHostKind.Fullscreen,
                GraphicsPresenter = presenter,
            });
            app.BeginRun();
            app.Tick();
            try
            {
                var error = Assert.Throws<InvalidOperationException>(app.EndRun);
                Assert.AreEqual("Reset failed.", error.Message);
                Assert.IsNull(root.App);
                Assert.IsNull(app.OwnerDispatcher);
                Assert.IsNull(Dispatcher.Current.AttachedApp);
            }
            finally
            {
                app.DisposeAsync().AsTask().GetAwaiter().GetResult();
            }

            var next = CreateApp(new TextBlock("Next"), terminal);
            next.BeginRun();
            try
            {
                // A leaked handler would signal the first app's disposed wake handle on this write.
                state.Value = 2;
                next.Tick();
            }
            finally
            {
                next.EndRun();
                next.DisposeAsync().AsTask().GetAwaiter().GetResult();
            }
        });
    }

    [TestMethod]
    public void Concurrent_Starts_Of_Running_App_Do_Not_Change_Its_Owner()
    {
        using var host = AppThread.Start(() => CreateApp(new TextBlock("Owner"), CreateTerminal()));
        var dispatcher = host.App.Dispatcher;
        TestThreads.RunConcurrently(8, _ =>
        {
            Assert.Throws<InvalidOperationException>(() => host.App.Run());
            Assert.AreSame(dispatcher, host.App.Dispatcher);
        });
        host.Stop();
    }

    [TestMethod]
    public void Run_Failure_And_Leaked_Ambient_Scope_Restore_The_Previous_Terminal()
    {
        TestThreads.Run(() =>
        {
            using var previousTerminal = CreateTerminal();
            using var previousScope = Terminal.UseInstance(previousTerminal);
            using var terminal = CreateTerminal();
            using var innerTerminal = CreateTerminal();
            var app = CreateApp(new TextBlock("Failure"), terminal);
            TerminalScope innerScope = default;
            app.SetUpdateCallback((Func<TerminalRunningContext, TerminalLoopResult>)(_ =>
            {
                innerScope = Terminal.UseInstance(innerTerminal);
                throw new ApplicationException("Original failure.");
            }));

            try
            {
                var error = Assert.Throws<ApplicationException>(() => app.Run());
                Assert.AreEqual("Original failure.", error.Message);
            }
            finally
            {
                app.DisposeAsync().AsTask().GetAwaiter().GetResult();
                terminal.Dispose();
                innerScope.Dispose();
            }

            Assert.AreSame(previousTerminal, Terminal.Instance);
            Terminal.Write("Still usable.");
        });
    }

    [TestMethod]
    public void Run_Failure_And_Cleanup_Failure_Report_Both_Errors()
    {
        TestThreads.Run(() =>
        {
            using var terminal = CreateTerminal();
            var app = new TerminalApp(new TextBlock("Failure"), terminal, new TerminalAppOptions
            {
                HostKind = TerminalHostKind.Fullscreen,
                GraphicsPresenter = new ThrowingResetPresenter(),
            });
            app.SetUpdateCallback((Func<TerminalRunningContext, TerminalLoopResult>)(_ => throw new ApplicationException("Original failure.")));
            try
            {
                var error = Assert.Throws<AggregateException>(() => app.Run()).Flatten();
                Assert.IsTrue(error.InnerExceptions.Any(x => x is ApplicationException { Message: "Original failure." }));
                Assert.IsTrue(error.InnerExceptions.Any(x => x is InvalidOperationException { Message: "Reset failed." }));
                Assert.IsNull(app.Root.App);
                Assert.IsNull(Dispatcher.Current.AttachedApp);
            }
            finally
            {
                app.DisposeAsync().AsTask().GetAwaiter().GetResult();
            }
        });
    }

    [TestMethod]
    public void DisposeAsync_Waits_For_Run_Cleanup_To_Finish()
    {
        using var terminal = CreateTerminal();
        using var presenter = new BlockingResetPresenter();
        using var cleanupEntered = presenter.CleanupEntered;
        using var releaseCleanup = presenter.ReleaseCleanup;
        using var running = new ManualResetEventSlim();
        var app = new TerminalApp(new TextBlock("Dispose"), terminal, new TerminalAppOptions
        {
            HostKind = TerminalHostKind.Fullscreen,
            GraphicsPresenter = presenter,
        });
        app.Post(running.Set);
        var run = Task.Run(() => app.Run());
        Task? disposal = null;
        try
        {
            Assert.IsTrue(running.Wait(TestThreads.Timeout));
            disposal = app.DisposeAsync().AsTask();
            Assert.IsTrue(presenter.CleanupEntered.Wait(TestThreads.Timeout));
            Assert.IsFalse(disposal.IsCompleted, "Disposal must not release resources while the app is still cleaning up.");
        }
        finally
        {
            presenter.ReleaseCleanup.Set();
            run.WaitAsync(TestThreads.Timeout).GetAwaiter().GetResult();
            disposal?.WaitAsync(TestThreads.Timeout).GetAwaiter().GetResult();
        }
    }

    private static TerminalInstance CreateTerminal() => new(new InMemoryTerminalBackend(new TerminalSize(30, 5)));

    private static TerminalApp CreateApp(Visual root, TerminalInstance terminal) => new(root, terminal, new TerminalAppOptions
    {
        HostKind = TerminalHostKind.Fullscreen,
        EnableMouse = false,
        EnableBracketedPaste = false,
    });

    private sealed class ThrowingResetPresenter : ITerminalGraphicsPresenter
    {
        private int _resetCalls;

        public TerminalGraphicsCapabilities Capabilities => TerminalGraphicsCapabilities.None;

        public ValueTask PresentAsync(GraphicsCommandBuffer current, TerminalGraphicsPresentContext context, CancellationToken cancellationToken = default)
            => ValueTask.CompletedTask;

        public void Reset()
        {
            if (++_resetCalls == 2)
            {
                throw new InvalidOperationException("Reset failed.");
            }
        }

        public void Dispose()
        {
        }
    }

    private sealed class BlockingResetPresenter : ITerminalGraphicsPresenter
    {
        private int _resetCalls;
        public ManualResetEventSlim CleanupEntered { get; } = new();
        public ManualResetEventSlim ReleaseCleanup { get; } = new();
        public TerminalGraphicsCapabilities Capabilities => TerminalGraphicsCapabilities.None;

        public ValueTask PresentAsync(GraphicsCommandBuffer current, TerminalGraphicsPresentContext context, CancellationToken cancellationToken = default)
            => ValueTask.CompletedTask;

        public void Reset()
        {
            if (++_resetCalls == 2)
            {
                CleanupEntered.Set();
                if (!ReleaseCleanup.Wait(TestThreads.Timeout))
                {
                    throw new TimeoutException("Cleanup was not released.");
                }
            }
        }

        public void Dispose()
        {
            // The test owns these events, including while it checks whether app disposal has completed.
        }
    }
}
