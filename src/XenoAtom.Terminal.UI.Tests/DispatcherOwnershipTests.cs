// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.Backends;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Hosting;
using XenoAtom.Terminal.UI.Threading;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class DispatcherOwnershipTests
{
    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void State_Ownership_Follows_The_App_Run_Not_The_Thread(bool readInNextApp)
    {
        TestThreads.Run(() =>
        {
            using var terminal = new TerminalInstance(new InMemoryTerminalBackend(new TerminalSize(30, 5)));
            var state = new State<int>(1);
            var first = CreateApp(new TextBlock(() => $"First {state.Value}"), terminal);
            first.BeginRun();
            try
            {
                first.Tick();
            }
            finally
            {
                first.EndRun();
                first.DisposeAsync().AsTask().GetAwaiter().GetResult();
            }

            var next = CreateApp(
                readInNextApp ? new TextBlock(() => $"Next {state.Value}") : new TextBlock("Unrelated"),
                terminal);
            next.BeginRun();
            try
            {
                next.Tick();
                TestThreads.Run(() =>
                {
                    if (readInNextApp)
                    {
                        Assert.IsFalse(state.CheckAccess());
                        Assert.AreSame(next.Dispatcher, state.Dispatcher);
                        Assert.Throws<InvalidOperationException>(() => state.Value = 2);
                    }
                    else
                    {
                        Assert.IsTrue(state.CheckAccess(), "An unrelated app must not retain the previous app's state ownership.");
                        Assert.AreSame(Dispatcher.Current, state.Dispatcher);
                        state.Value = 2;
                        Assert.AreEqual(2, state.Value);
                    }
                });
            }
            finally
            {
                next.EndRun();
                next.DisposeAsync().AsTask().GetAwaiter().GetResult();
            }
        });
    }

    [TestMethod]
    public void State_Cannot_Acquire_An_Owner_While_Another_Run_Owns_It()
    {
        var state = new State<int>(1);
        using var host = AppThread.Start(() =>
        {
            var terminal = new TerminalInstance(new InMemoryTerminalBackend(new TerminalSize(30, 5)));
            return CreateApp(new TextBlock(() => $"Owned {state.Value}"), terminal);
        });

        TestThreads.Run(() =>
        {
            using var terminal = new TerminalInstance(new InMemoryTerminalBackend(new TerminalSize(30, 5)));
            var app = CreateApp(new TextBlock("Other"), terminal);
            app.BeginRun();
            try
            {
                // A tracked read can race another app's first read between checking access and acquiring ownership.
                Assert.Throws<InvalidOperationException>(() => state.TryAcquireDispatcher(Dispatcher.Current));
                Assert.AreSame(host.App.Dispatcher, state.Dispatcher);
            }
            finally
            {
                app.EndRun();
                app.DisposeAsync().AsTask().GetAwaiter().GetResult();
            }
        });
    }

    [TestMethod]
    public void Rejected_Second_App_Does_Not_Change_The_Running_Apps_Terminal()
    {
        TestThreads.Run(() =>
        {
            using var terminal = new TerminalInstance(new InMemoryTerminalBackend(new TerminalSize(30, 5)));
            var first = CreateApp(new TextBlock("First"), terminal);
            var second = CreateApp(new TextBlock("Second"), terminal);
            first.BeginRun();
            try
            {
                first.Tick();
                var markupStyles = terminal.MarkupStyles;
                var synchronizationContext = SynchronizationContext.Current;

                Assert.Throws<InvalidOperationException>(second.BeginRun);

                Assert.AreSame(markupStyles, terminal.MarkupStyles);
                Assert.AreSame(first, Dispatcher.Current.AttachedApp);
                Assert.AreSame(synchronizationContext, SynchronizationContext.Current);
                Assert.AreSame(terminal, Terminal.Instance);
                first.Tick();
            }
            finally
            {
                first.EndRun();
                first.DisposeAsync().AsTask().GetAwaiter().GetResult();
                second.DisposeAsync().AsTask().GetAwaiter().GetResult();
            }
        });
    }

    private static TerminalApp CreateApp(Visual root, TerminalInstance terminal) => new(root, terminal, new TerminalAppOptions
    {
        HostKind = TerminalHostKind.Fullscreen,
        EnableMouse = false,
        EnableBracketedPaste = false,
    });
}
