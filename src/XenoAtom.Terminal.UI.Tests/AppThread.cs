// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Diagnostics;
using System.Runtime.ExceptionServices;

namespace XenoAtom.Terminal.UI.Tests;

/// <summary>
/// Creates and runs a <see cref="TerminalApp"/> with <see cref="TerminalApp.Run(CancellationToken)"/> on a dedicated thread.
/// </summary>
/// <remarks>
/// The app is created on the app thread by the factory. When the run ends, the app and its terminal are disposed on the app thread.
/// </remarks>
internal sealed class AppThread : IDisposable
{
    private readonly Func<TerminalApp> _createApp;
    private readonly Thread _thread;
    private readonly ManualResetEventSlim _running = new();
    private TerminalApp? _app;
    private ExceptionDispatchInfo? _failure;
    private int _managedThreadId;

    private AppThread(Func<TerminalApp> createApp, string name)
    {
        _createApp = createApp;
        _thread = new Thread(ThreadMain) { IsBackground = true, Name = name };
    }

    public TerminalApp App => _app ?? throw new InvalidOperationException("The app was not created.");

    public int ManagedThreadId => Volatile.Read(ref _managedThreadId);

    /// <summary>
    /// Starts the app thread and waits until the app has rendered its first frame.
    /// </summary>
    public static AppThread Start(Func<TerminalApp> createApp, string name = nameof(AppThread))
    {
        ArgumentNullException.ThrowIfNull(createApp);

        var appThread = new AppThread(createApp, name);
        appThread._thread.Start();
        if (!appThread._running.Wait(TestThreads.Timeout))
        {
            throw new TimeoutException($"The app on thread `{name}` did not start in time.");
        }

        appThread._failure?.Throw();
        appThread.WaitForFirstFrame();
        return appThread;
    }

    /// <summary>
    /// Stops the app from its own thread (through <see cref="Threading.DispatcherObject.Dispatcher"/>) and waits for the run to end.
    /// </summary>
    public void Stop()
    {
        var app = App;
        if (_thread.IsAlive)
        {
            if (!app.Dispatcher.InvokeAsync(app.Stop).Wait(TestThreads.Timeout))
            {
                throw new TimeoutException($"The app on thread `{_thread.Name}` did not process the stop request in time.");
            }
        }

        if (!_thread.Join(TestThreads.Timeout))
        {
            throw new TimeoutException($"The app on thread `{_thread.Name}` did not stop in time.");
        }

        _failure?.Throw();
    }

    private void WaitForFirstFrame()
    {
        var app = App;
        var start = Stopwatch.GetTimestamp();
        while (true)
        {
            var hasFrame = app.Dispatcher.InvokeAsync(() => HasRenderedFrame(app));
            if (!hasFrame.Wait(TestThreads.Timeout))
            {
                throw new TimeoutException($"The app on thread `{_thread.Name}` did not process a request in time.");
            }

            if (hasFrame.Result)
            {
                return;
            }

            if (Stopwatch.GetElapsedTime(start) > TestThreads.Timeout)
            {
                throw new TimeoutException($"The app on thread `{_thread.Name}` did not render a frame in time.");
            }

            Thread.Sleep(1);
        }
    }

    private static bool HasRenderedFrame(TerminalApp app)
    {
        try
        {
            _ = app.GetRequiredRenderBuffer();
            return true;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    public void Dispose()
    {
        if (_thread.IsAlive)
        {
            _app?.Stop();
            _thread.Join(TestThreads.Timeout);
        }
    }

    private void ThreadMain()
    {
        Volatile.Write(ref _managedThreadId, Environment.CurrentManagedThreadId);
        try
        {
            var app = _createApp();
            _app = app;

            // Pending actions run at the beginning of the first frame, once the app is bound to this thread.
            app.Post(_running.Set);
            try
            {
                app.Run();
            }
            finally
            {
                app.DisposeAsync().AsTask().GetAwaiter().GetResult();
                app.Terminal.Dispose();
            }
        }
        catch (Exception ex)
        {
            _failure = ExceptionDispatchInfo.Capture(ex);
        }
        finally
        {
            _running.Set();
        }
    }
}
