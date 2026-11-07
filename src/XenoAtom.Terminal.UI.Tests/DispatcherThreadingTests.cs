// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.Backends;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Hosting;
using XenoAtom.Terminal.UI.Threading;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class DispatcherThreadingTests
{
    private const string NotRunningMessage = "Dispatcher is not attached to a running TerminalApp on this thread. Use TerminalApp.Dispatcher (or TerminalRunningContext.App.Dispatcher) to reach a specific app from another thread.";

    [TestMethod]
    public void Current_Is_Distinct_Per_Thread_And_Stable()
    {
        var current = Dispatcher.Current;
        Assert.AreSame(current, Dispatcher.Current);
        Assert.IsTrue(current.CheckAccess());

        Dispatcher? other = null;
        var otherIsStable = false;
        var testDispatcherAccessFromOther = true;
        TestThreads.Run(() =>
        {
            other = Dispatcher.Current;
            otherIsStable = ReferenceEquals(other, Dispatcher.Current);
            testDispatcherAccessFromOther = current.CheckAccess();
        });

        Assert.IsNotNull(other);
        Assert.AreNotSame(current, other);
        Assert.IsTrue(otherIsStable);
        Assert.IsFalse(testDispatcherAccessFromOther);
        Assert.IsFalse(other.CheckAccess());
        Assert.Throws<InvalidOperationException>(other.VerifyAccess);
    }

    [TestMethod]
    public void Post_Throws_When_No_App_Is_Running()
    {
        TestThreads.Run(() =>
        {
            var ex = Assert.Throws<InvalidOperationException>(() => Dispatcher.Current.Post(() => { }));
            Assert.AreEqual(NotRunningMessage, ex.Message);
        });
    }

    [TestMethod]
    public void InvokeAsync_Throws_When_No_App_Is_Running()
    {
        TestThreads.Run(() =>
        {
            var dispatcher = Dispatcher.Current;
            var invoked = false;

            var ex = Assert.Throws<InvalidOperationException>(() => { _ = dispatcher.InvokeAsync(() => { invoked = true; }); });
            Assert.AreEqual(NotRunningMessage, ex.Message);
            Assert.Throws<InvalidOperationException>(() => { _ = dispatcher.InvokeAsync(() => 1); });
            Assert.Throws<InvalidOperationException>(() => { _ = dispatcher.InvokeAsync(() => Task.CompletedTask); });
            Assert.Throws<InvalidOperationException>(() => { _ = dispatcher.InvokeAsync(() => Task.FromResult(1)); });
            Assert.IsFalse(invoked);
        });
    }

    [TestMethod]
    public void Dispatcher_Current_On_Worker_Thread_Does_Not_Reach_Running_App()
    {
        using var host = AppThread.Start(() => CreateApp(new TextBlock("Worker")));
        var app = host.App;

        var sameAsApp = true;
        Exception? postError = null;
        Exception? invokeError = null;
        TestThreads.Run(() =>
        {
            sameAsApp = ReferenceEquals(Dispatcher.Current, app.Dispatcher);
            postError = Capture(() => Dispatcher.Current.Post(() => { }));
            invokeError = Capture(() => { _ = Dispatcher.Current.InvokeAsync(() => { }); });
        });

        Assert.IsFalse(sameAsApp);
        Assert.IsInstanceOfType<InvalidOperationException>(postError);
        Assert.AreEqual(NotRunningMessage, postError.Message);
        Assert.IsInstanceOfType<InvalidOperationException>(invokeError);

        host.Stop();
    }

    [TestMethod]
    public async Task App_Dispatcher_InvokeAsync_From_Background_Thread_Runs_On_App_Thread()
    {
        using var host = AppThread.Start(() => CreateApp(new TextBlock("Invoke")));
        var app = host.App;

        Assert.IsFalse(app.CheckAccess());
        Assert.IsFalse(app.Dispatcher.CheckAccess());
        Assert.AreSame(app, app.Dispatcher.AttachedApp);

        var ranOn = 0;
        await app.Dispatcher.InvokeAsync(() => { ranOn = Environment.CurrentManagedThreadId; }).WaitAsync(TestThreads.Timeout);
        Assert.AreEqual(host.ManagedThreadId, ranOn);

        var result = await app.Dispatcher.InvokeAsync(() => Environment.CurrentManagedThreadId).WaitAsync(TestThreads.Timeout);
        Assert.AreEqual(host.ManagedThreadId, result);

        var asyncResult = await app.Dispatcher.InvokeAsync(async () =>
        {
            await Task.Yield();
            return Environment.CurrentManagedThreadId;
        }).WaitAsync(TestThreads.Timeout);
        Assert.AreEqual(host.ManagedThreadId, asyncResult);

        var asyncRanOn = 0;
        await app.Dispatcher.InvokeAsync(async () =>
        {
            await Task.Delay(1);
            asyncRanOn = Environment.CurrentManagedThreadId;
        }).WaitAsync(TestThreads.Timeout);
        Assert.AreEqual(host.ManagedThreadId, asyncRanOn);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => app.Dispatcher.InvokeAsync(() => throw new InvalidOperationException("boom")).WaitAsync(TestThreads.Timeout));
        Assert.AreEqual("boom", ex.Message);

        host.Stop();
    }

    [TestMethod]
    public void Await_In_Async_Update_Callback_Resumes_On_App_Thread()
    {
        var startedOn = 0;
        var resumedOn = 0;
        var resumedContextIsDispatcher = false;
        using var resumed = new ManualResetEventSlim();

        using var host = AppThread.Start(() =>
        {
            var app = CreateApp(new TextBlock("Async"));
            var first = true;
            app.SetUpdateCallback(async _ =>
            {
                if (first)
                {
                    first = false;
                    startedOn = Environment.CurrentManagedThreadId;
                    await Task.Delay(10);
                    resumedOn = Environment.CurrentManagedThreadId;
                    resumedContextIsDispatcher = SynchronizationContext.Current is DispatcherSynchronizationContext;
                    resumed.Set();
                }

                return TerminalLoopResult.Continue;
            });
            return app;
        });

        Assert.IsTrue(resumed.Wait(TestThreads.Timeout), "The async update callback did not resume.");
        Assert.AreEqual(host.ManagedThreadId, startedOn);
        Assert.AreEqual(host.ManagedThreadId, resumedOn);
        Assert.IsTrue(resumedContextIsDispatcher);

        host.Stop();
    }

    [TestMethod]
    public void EndRun_Restores_SynchronizationContext_And_Detaches_Dispatcher()
    {
        TestThreads.Run(() =>
        {
            var previous = new SynchronizationContext();
            SynchronizationContext.SetSynchronizationContext(previous);

            using var outerTerminal = new TerminalInstance(new InMemoryTerminalBackend(new TerminalSize(20, 5)));
            using var terminal = new TerminalInstance(new InMemoryTerminalBackend(new TerminalSize(20, 5)));
            using var outerScope = Terminal.UseInstance(outerTerminal);

            var root = new TextBlock("Sync");
            var app = new TerminalApp(root, terminal, FullscreenOptions());
            var dispatcher = Dispatcher.Current;

            // Before the run, an app has no owner yet: its dispatcher is the one of the calling thread.
            Assert.AreSame(dispatcher, app.Dispatcher);
            Assert.IsNull(dispatcher.AttachedApp);

            app.BeginRun();
            try
            {
                Assert.IsInstanceOfType<DispatcherSynchronizationContext>(SynchronizationContext.Current);
                Assert.AreSame(app, dispatcher.AttachedApp);
                Assert.AreSame(dispatcher, app.Dispatcher);
                Assert.AreSame(dispatcher, root.Dispatcher);
                Assert.AreSame(terminal, Terminal.Instance);
                app.Tick();
            }
            finally
            {
                app.EndRun();
            }

            Assert.AreSame(previous, SynchronizationContext.Current);
            Assert.IsNull(dispatcher.AttachedApp);
            Assert.IsNull(app.OwnerDispatcher);
            Assert.IsNull(root.OwnerDispatcher);
            Assert.AreSame(outerTerminal, Terminal.Instance);

            app.DisposeAsync().AsTask().GetAwaiter().GetResult();
        });
    }

    [TestMethod]
    public void EndRun_With_Undisposed_Ambient_Terminal_Scope_Throws_And_Still_Detaches_Dispatcher()
    {
        TestThreads.Run(() =>
        {
            var previous = new SynchronizationContext();
            SynchronizationContext.SetSynchronizationContext(previous);

            using var terminal = new TerminalInstance(new InMemoryTerminalBackend(new TerminalSize(20, 5)));
            using var leakedTerminal = new TerminalInstance(new InMemoryTerminalBackend(new TerminalSize(20, 5)));
            var dispatcher = Dispatcher.Current;
            var root = new TextBlock("Leak");
            var app = new TerminalApp(root, terminal, FullscreenOptions());

            app.BeginRun();
            app.Tick();

            // User code on the app thread opens an ambient scope and leaves it open past the end of the run:
            // the app's own scope is no longer the innermost one, so restoring it is out of order and throws.
            var leakedScope = Terminal.UseInstance(leakedTerminal);
            Assert.Throws<InvalidOperationException>(app.EndRun);

            // The run still releases the thread.
            Assert.IsNull(dispatcher.AttachedApp);
            Assert.IsNull(app.OwnerDispatcher);
            Assert.IsNull(root.OwnerDispatcher);
            Assert.AreSame(previous, SynchronizationContext.Current);
            Assert.AreSame(leakedTerminal, Terminal.Instance);
            leakedScope.Dispose();

            // Another app can run on the same thread.
            using var nextTerminal = new TerminalInstance(new InMemoryTerminalBackend(new TerminalSize(20, 5)));
            var next = new TerminalApp(new TextBlock("Next"), nextTerminal, FullscreenOptions());
            next.BeginRun();
            try
            {
                Assert.AreSame(next, dispatcher.AttachedApp);
                Assert.AreSame(nextTerminal, Terminal.Instance);
                next.Tick();
            }
            finally
            {
                next.EndRun();
            }

            Assert.IsNull(dispatcher.AttachedApp);
            next.DisposeAsync().AsTask().GetAwaiter().GetResult();
            app.DisposeAsync().AsTask().GetAwaiter().GetResult();
        });
    }

    [TestMethod]
    public void Sequential_Apps_On_Same_Thread_Do_Not_Leak()
    {
        TestThreads.Run(() =>
        {
            var backend = new InMemoryTerminalBackend(new TerminalSize(40, 6));
            using var terminal = new TerminalInstance(backend);
            var originalMarkupStyles = terminal.MarkupStyles;
            var dispatcher = Dispatcher.Current;

            var stateA = new State<string>("first");
            var textBoxA = new TextBox();
            var appA = new TerminalApp(new VStack { new TextBlock(() => $"A:{stateA.Value}"), textBoxA }, terminal, FullscreenOptions());
            appA.BeginRun();
            try
            {
                appA.Tick();
                Assert.AreSame(textBoxA, appA.FocusedElement);
                Assert.AreSame(appA, dispatcher.AttachedApp);
                Assert.AreNotSame(originalMarkupStyles, terminal.MarkupStyles);
            }
            finally
            {
                appA.EndRun();
                appA.DisposeAsync().AsTask().GetAwaiter().GetResult();
            }

            Assert.AreSame(originalMarkupStyles, terminal.MarkupStyles);
            Assert.IsNull(dispatcher.AttachedApp);
            Assert.IsNull(textBoxA.App);
            Assert.IsNull(appA.FocusedElement);

            var stateB = new State<string>("second");
            var textBoxB = new TextBox();
            var appB = new TerminalApp(new VStack { new TextBlock(() => $"B:{stateB.Value}"), textBoxB }, terminal, FullscreenOptions());
            appB.BeginRun();
            try
            {
                appB.Tick();
                Assert.AreSame(appB, dispatcher.AttachedApp);
                Assert.AreSame(dispatcher, appB.Dispatcher);
                Assert.AreSame(textBoxB, appB.FocusedElement);
                Assert.IsFalse(textBoxA.HasFocus);

                // The first app no longer observes its state: changing it must not affect the second app.
                stateA.Value = "changed after A ended";
                stateB.Value = "updated";
                appB.Tick();

                var screen = new AnsiTestScreen(40, 6);
                screen.Apply(backend.GetOutText());
                var rendered = screen.GetText();
                StringAssert.Contains(rendered, "B:updated");
                Assert.IsFalse(rendered.Contains("changed after A ended", StringComparison.Ordinal));
            }
            finally
            {
                appB.EndRun();
                appB.DisposeAsync().AsTask().GetAwaiter().GetResult();
            }

            Assert.AreSame(originalMarkupStyles, terminal.MarkupStyles);
            Assert.IsNull(dispatcher.AttachedApp);
        });
    }

    [TestMethod]
    public void Second_App_On_Same_Thread_Throws_While_First_Is_Running()
    {
        TestThreads.Run(() =>
        {
            using var terminalA = new TerminalInstance(new InMemoryTerminalBackend(new TerminalSize(20, 5)));
            using var terminalB = new TerminalInstance(new InMemoryTerminalBackend(new TerminalSize(20, 5)));
            var appA = new TerminalApp(new TextBlock("A"), terminalA, FullscreenOptions());
            var appB = new TerminalApp(new TextBlock("B"), terminalB, FullscreenOptions());

            appA.BeginRun();
            try
            {
                var ex = Assert.Throws<InvalidOperationException>(appB.BeginRun);
                StringAssert.Contains(ex.Message, "already attached");
                Assert.AreSame(appA, Dispatcher.Current.AttachedApp);
                Assert.IsInstanceOfType<DispatcherSynchronizationContext>(SynchronizationContext.Current);
                Assert.AreSame(terminalA, Terminal.Instance);
                appA.Tick();
            }
            finally
            {
                appA.EndRun();
            }

            // Once the first app has ended, the second one can run on the same thread.
            appB.BeginRun();
            try
            {
                appB.Tick();
                Assert.AreSame(appB, Dispatcher.Current.AttachedApp);
            }
            finally
            {
                appB.EndRun();
            }

            appA.DisposeAsync().AsTask().GetAwaiter().GetResult();
            appB.DisposeAsync().AsTask().GetAwaiter().GetResult();
        });
    }

    [TestMethod]
    public void BindToCurrentThread_Throws_From_Another_Thread()
    {
        using var terminal = new TerminalInstance(new InMemoryTerminalBackend(new TerminalSize(20, 5)));
        var app = new TerminalApp(new TextBlock("Bind"), terminal, FullscreenOptions());
        var dispatcher = Dispatcher.Current;

        Exception? error = null;
        TestThreads.Run(() => error = Capture(() => dispatcher.BindToCurrentThread(app)));

        Assert.IsInstanceOfType<InvalidOperationException>(error);
        Assert.IsNull(dispatcher.AttachedApp);
    }

    [TestMethod]
    public void Tree_Built_On_One_Thread_Runs_On_Another_And_Is_Owned_By_It()
    {
        var textBox = new TextBox { Text = "built here" };
        var root = new VStack { new TextBlock("Owned"), textBox };

        using var host = AppThread.Start(() => CreateApp(root));
        var app = host.App;

        Assert.AreSame(app.Dispatcher, textBox.Dispatcher);
        Assert.IsFalse(textBox.CheckAccess());
        Assert.Throws<InvalidOperationException>(textBox.VerifyAccess);
        Assert.Throws<InvalidOperationException>(() => textBox.Text = "from the test thread");
        Assert.Throws<InvalidOperationException>(() => _ = textBox.Text);

        app.Dispatcher.InvokeAsync(() => textBox.Text = "through the dispatcher").Wait(TestThreads.Timeout);
        var text = app.Dispatcher.InvokeAsync(() => textBox.Text).GetAwaiter().GetResult();
        Assert.AreEqual("through the dispatcher", text);

        host.Stop();

        // Once the app has ended, the tree has no owner anymore.
        Assert.IsTrue(textBox.CheckAccess());
        Assert.AreSame(Dispatcher.Current, textBox.Dispatcher);
        textBox.Text = "after run";
        Assert.AreEqual("after run", textBox.Text);
    }

    [TestMethod]
    public void Visual_Elements_Follow_The_App_Of_Their_Owner_Visual()
    {
        var item = new BarChartItem("A", 5);
        var chart = new BarChart { Minimum = 0, Maximum = 10 };
        chart.Items.Add(item);
        var task = new ProgressTask("Work") { Value = 0.5 };
        var group = new ProgressTaskGroup();
        group.Tasks.Add(task);
        var editor = new CodeEditor();

        Assert.IsTrue(item.CheckAccess());
        Assert.IsTrue(task.CheckAccess());

        using var host = AppThread.Start(() => CreateApp(new VStack { chart, group, editor }));
        var app = host.App;

        Assert.IsFalse(item.CheckAccess());
        Assert.AreSame(app.Dispatcher, item.Dispatcher);
        Assert.IsFalse(task.CheckAccess());
        Assert.AreSame(app.Dispatcher, task.Dispatcher);
        Assert.Throws<InvalidOperationException>(() => task.Value = 0.75);

        // CodeEditor marshals syntax refreshes through its Dispatcher when called off its thread: it must be the app's one.
        Assert.IsFalse(editor.CheckAccess());
        Assert.AreSame(app.Dispatcher, editor.Dispatcher);

        app.Dispatcher.InvokeAsync(() => task.Value = 0.75).Wait(TestThreads.Timeout);

        host.Stop();

        Assert.IsTrue(item.CheckAccess());
        Assert.IsTrue(task.CheckAccess());
        Assert.IsTrue(editor.CheckAccess());
        Assert.AreEqual(0.75, task.Value);
    }

    [TestMethod]
    public void State_Read_By_Running_App_Is_Owned_By_Its_Thread()
    {
        var owned = new State<int>(1);
        var unread = new State<int>(1);

        using var host = AppThread.Start(() => CreateApp(new TextBlock(() => $"Owned {owned.Value}")));
        var app = host.App;

        Assert.IsFalse(owned.CheckAccess());
        Assert.AreSame(app.Dispatcher, owned.Dispatcher);
        Assert.Throws<InvalidOperationException>(() => _ = owned.Value);
        Assert.Throws<InvalidOperationException>(() => owned.Value = 2);

        // A state that no app has read has no owner.
        Assert.IsTrue(unread.CheckAccess());
        unread.Value = 2;

        host.Stop();

        Assert.IsTrue(owned.CheckAccess());
        owned.Value = 3;
        Assert.AreEqual(3, owned.Value);
    }

    private static TerminalApp CreateApp(Visual root)
    {
        var terminal = new TerminalInstance(new InMemoryTerminalBackend(new TerminalSize(40, 10)));
        return new TerminalApp(root, terminal, FullscreenOptions());
    }

    private static TerminalAppOptions FullscreenOptions() => new()
    {
        HostKind = TerminalHostKind.Fullscreen,
        EnableMouse = false,
        EnableBracketedPaste = false,
    };

    private static Exception? Capture(Action action)
    {
        try
        {
            action();
            return null;
        }
        catch (Exception ex)
        {
            return ex;
        }
    }
}
