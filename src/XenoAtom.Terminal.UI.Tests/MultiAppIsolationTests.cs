// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Diagnostics;
using XenoAtom.Terminal.Backends;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Hosting;
using XenoAtom.Terminal.UI.Threading;

namespace XenoAtom.Terminal.UI.Tests;

/// <summary>
/// Runs several apps concurrently, one per thread, each with its own terminal, and checks that they never observe each other.
/// </summary>
[TestClass]
public sealed class MultiAppIsolationTests
{
    private const int AppCount = 8;
    private const int Width = 80;
    private const int Height = 25;

    [TestMethod]
    public void Concurrent_Apps_Driven_By_Tick_Are_Isolated()
    {
        for (var iteration = 0; iteration < 20; iteration++)
        {
            RunTickedApps(iteration);
        }
    }

    [TestMethod]
    public void Concurrent_Apps_Driven_By_Run_Are_Isolated()
    {
        for (var iteration = 0; iteration < 20; iteration++)
        {
            RunRunningApps(iteration);
        }
    }

    private static void RunTickedApps(int iteration)
    {
        using var barrier = new Barrier(AppCount);
        var dispatchers = new Dispatcher?[AppCount];
        var bindingManagers = new BindingManager?[AppCount];

        TestThreads.RunConcurrently(AppCount, index =>
        {
            var participating = true;
            try
            {
                RunTickedApp(index, iteration, barrier, out dispatchers[index], out bindingManagers[index]);
            }
            catch
            {
                // Let the other apps proceed (and report their own state) instead of waiting for this one at the barrier.
                if (participating)
                {
                    participating = false;
                    barrier.RemoveParticipant();
                }

                throw;
            }
        }, "TickedApp");

        Assert.HasCount(AppCount, dispatchers.Distinct());
        Assert.HasCount(AppCount, bindingManagers.Distinct());
    }

    private static void RunTickedApp(int index, int iteration, Barrier barrier, out Dispatcher? dispatcher, out BindingManager? bindingManager)
    {
        var app = CreateApp(index, out var backend, out var model);

        // All the apps are built, then start at the same time.
        SignalAndWait(barrier);
        var timestamp = Stopwatch.GetTimestamp();
        var tickStep = Math.Max(1, Stopwatch.Frequency / 100);
        void Tick(int count = 1)
        {
            for (var i = 0; i < count; i++)
            {
                timestamp += tickStep;
                app.Tick(timestamp);
            }
        }

        app.BeginRun();
        try
        {
            dispatcher = Dispatcher.Current;
            bindingManager = BindingManager.Current;
            Assert.AreSame(app, dispatcher.AttachedApp, $"App {index}, iteration {iteration}");
            Assert.AreSame(dispatcher, app.Dispatcher);
            Assert.AreSame(app.Terminal, Terminal.Instance);

            Tick();
            Assert.AreSame(model.TextBox, app.FocusedElement, $"App {index}, iteration {iteration}: unexpected initial focus.");

            backend.PushEvent(new TerminalTextEvent { Text = model.TypedText });
            Tick(2);
            Assert.AreEqual(model.TypedText, model.TextBox.Text);
            Assert.AreEqual(model.TypedText, model.Text.Value);
            Assert.AreSame(model.TextBox, app.FocusedElement);

            // Every app is running and has processed input: run the rest while all of them are alive.
            SignalAndWait(barrier);

            backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Tab });
            Tick();
            Assert.AreSame(model.Button, app.FocusedElement, $"App {index}, iteration {iteration}: Tab did not move the focus to the button.");

            backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Enter });
            Tick(2);
            Assert.AreEqual(1, model.Clicks.Value);

            AssertOutputIsIsolated(index, backend.GetOutText(), model);
            Assert.AreSame(app, Dispatcher.Current.AttachedApp);
            Assert.AreSame(app.Terminal, Terminal.Instance);

            SignalAndWait(barrier);
        }
        finally
        {
            app.EndRun();
            app.DisposeAsync().AsTask().GetAwaiter().GetResult();
            app.Terminal.Dispose();
        }

        Assert.IsNull(Dispatcher.Current.AttachedApp);
        Assert.IsNull(model.TextBox.App);
        Assert.IsTrue(model.Text.CheckAccess());
    }

    private static void RunRunningApps(int iteration)
    {
        var hosts = new AppThread?[AppCount];
        var backends = new InMemoryTerminalBackend?[AppCount];
        var models = new AppModel?[AppCount];
        try
        {
            using var barrier = new Barrier(AppCount);
            TestThreads.RunConcurrently(AppCount, index =>
            {
                SignalAndWait(barrier);
                hosts[index] = AppThread.Start(() =>
                {
                    var app = CreateApp(index, out var backend, out var model);
                    backends[index] = backend;
                    models[index] = model;
                    return app;
                }, $"RunningApp-{index}");
            }, "StartApp");

            var appThreadIds = hosts.Select(x => x!.ManagedThreadId).Distinct().ToArray();
            Assert.HasCount(AppCount, appThreadIds);

            // Input is pushed from the test thread; each app reads it through its own input relay.
            for (var index = 0; index < AppCount; index++)
            {
                Assert.AreSame(models[index]!.TextBox, Invoke(hosts[index]!, app => app.FocusedElement), $"App {index}, iteration {iteration}: unexpected initial focus.");
                backends[index]!.PushEvent(new TerminalTextEvent { Text = models[index]!.TypedText });
            }

            for (var index = 0; index < AppCount; index++)
            {
                var model = models[index]!;
                WaitUntil(hosts[index]!, _ => model.Text.Value == model.TypedText, $"App {index}, iteration {iteration}: text input was not processed.");
                backends[index]!.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Tab });
                WaitUntil(hosts[index]!, app => ReferenceEquals(app.FocusedElement, model.Button), $"App {index}, iteration {iteration}: Tab was not processed.");
                backends[index]!.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Enter });
            }

            for (var index = 0; index < AppCount; index++)
            {
                var model = models[index]!;
                var host = hosts[index]!;
                WaitUntil(host, _ => model.Clicks.Value == 1, $"App {index}, iteration {iteration}: Enter was not processed.");

                // The output is written by the app thread: read it once the app has rendered the last state.
                var backend = backends[index]!;
                WaitUntil(host, _ => RenderedText(backend.GetOutText()).Contains(model.ExpectedStatus, StringComparison.Ordinal), $"App {index}, iteration {iteration}: final state was not rendered.");
                Assert.AreSame(host.App, Invoke(host, app => Dispatcher.Current.AttachedApp));
                Assert.AreSame(host.App.Terminal, Invoke(host, _ => Terminal.Instance));
                Assert.IsFalse(model.Text.CheckAccess());
            }

            for (var index = 0; index < AppCount; index++)
            {
                var backend = backends[index]!;
                AssertOutputIsIsolated(index, Invoke(hosts[index]!, _ => backend.GetOutText()), models[index]!);
            }
        }
        finally
        {
            StopAll(hosts);
        }

        foreach (var model in models)
        {
            Assert.IsNotNull(model);
            Assert.IsTrue(model.Text.CheckAccess());
            Assert.IsNull(model.TextBox.App);
        }
    }

    private static TerminalApp CreateApp(int index, out InMemoryTerminalBackend backend, out AppModel model)
    {
        backend = new InMemoryTerminalBackend(new TerminalSize(Width, Height));
        var terminal = new TerminalInstance(backend);

        model = new AppModel(index);
        var captured = model;
        var root = new VStack
        {
            new TextBlock(() => $"{captured.Marker} clicks={captured.Clicks.Value} text={captured.Text.Value}"),
            captured.TextBox,
            captured.Button,
        };

        return new TerminalApp(root, terminal, new TerminalAppOptions
        {
            HostKind = TerminalHostKind.Fullscreen,
            InitialFocusMode = InitialFocusMode.FirstFocusable,
        });
    }

    private static void AssertOutputIsIsolated(int index, string output, AppModel model)
    {
        StringAssert.Contains(RenderedText(output), model.ExpectedStatus, $"App {index} did not render its own state.");

        for (var other = 0; other < AppCount; other++)
        {
            if (other == index)
            {
                continue;
            }

            Assert.IsFalse(output.Contains(AppModel.GetMarker(other), StringComparison.Ordinal), $"App {index} output contains the marker of app {other}.");
            Assert.IsFalse(output.Contains(AppModel.GetTypedText(other), StringComparison.Ordinal), $"App {index} output contains the text typed in app {other}.");
            Assert.IsFalse(output.Contains(AppModel.GetButtonLabel(other), StringComparison.Ordinal), $"App {index} output contains the button of app {other}.");
        }
    }

    private static string RenderedText(string output)
    {
        var screen = new AnsiTestScreen(Width, Height);
        screen.Apply(output);
        return screen.GetText();
    }

    private static void StopAll(AppThread?[] hosts)
    {
        List<Exception>? errors = null;
        foreach (var host in hosts)
        {
            if (host is null)
            {
                continue;
            }

            try
            {
                host.Stop();
            }
            catch (Exception ex)
            {
                (errors ??= []).Add(ex);
            }
            finally
            {
                host.Dispose();
            }
        }

        if (errors is not null)
        {
            throw new AggregateException("Some apps failed to stop.", errors);
        }
    }

    private static T Invoke<T>(AppThread host, Func<TerminalApp, T> func)
    {
        var task = host.App.Dispatcher.InvokeAsync(() => func(host.App));
        Assert.IsTrue(task.Wait(TestThreads.Timeout), "The app did not process the request in time.");
        return task.Result;
    }

    private static void WaitUntil(AppThread host, Func<TerminalApp, bool> condition, string message)
    {
        var start = Stopwatch.GetTimestamp();
        while (!Invoke(host, condition))
        {
            if (Stopwatch.GetElapsedTime(start) > TestThreads.Timeout)
            {
                Assert.Fail(message);
            }

            Thread.Sleep(1);
        }
    }

    private static void SignalAndWait(Barrier barrier)
    {
        if (!barrier.SignalAndWait(TestThreads.Timeout))
        {
            throw new TimeoutException("The other apps did not reach the barrier in time.");
        }
    }

    private sealed class AppModel
    {
        public AppModel(int index)
        {
            Marker = GetMarker(index);
            TypedText = GetTypedText(index);
            ButtonLabel = GetButtonLabel(index);
            Text = new State<string?>(string.Empty);
            Clicks = new State<int>(0);
            TextBox = new TextBox().Text(Text);
            Button = new Button(ButtonLabel);
            Button.Click((_, _) => Clicks.Value++);
        }

        public static string GetMarker(int index) => $"<app-{index}>";

        public static string GetTypedText(int index) => $"typed-{index}-text";

        public static string GetButtonLabel(int index) => $"Go-{index}!";

        public string Marker { get; }

        public string TypedText { get; }

        public string ButtonLabel { get; }

        public string ExpectedStatus => $"{Marker} clicks=1 text={TypedText}";

        public State<string?> Text { get; }

        public State<int> Clicks { get; }

        public TextBox TextBox { get; }

        public Button Button { get; }
    }
}
