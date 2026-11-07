// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Runtime.CompilerServices;

namespace XenoAtom.Terminal.UI.Threading;

/// <summary>
/// Provides access to the UI thread for terminal UI operations.
/// </summary>
/// <remarks>
/// Each thread has its own current dispatcher (see <see cref="Current"/>). A <see cref="TerminalApp"/> attaches it for the
/// duration of one run and retires it afterwards, so captured dispatchers never target a subsequent run.
/// Several apps can run concurrently, each on its own thread.
/// Use <see cref="TerminalApp"/>.<see cref="DispatcherObject.Dispatcher"/> (or <see cref="TerminalRunningContext.App"/>)
/// to reach a specific app from another thread.
/// </remarks>
public sealed class Dispatcher
{
    internal const string NotRunningMessage = "Dispatcher is not attached to a running TerminalApp on this thread. Use TerminalApp.Dispatcher (or TerminalRunningContext.App.Dispatcher) to reach a specific app from another thread.";

    [ThreadStatic]
    private static Dispatcher? _current;

    private readonly int _threadId;
    private TerminalApp? _app;
    private Ownership? _ownership;
    private DispatcherSynchronizationContext? _synchronizationContext;
    private SynchronizationContext? _previousSynchronizationContext;

    private Dispatcher() : this(Environment.CurrentManagedThreadId)
    {
    }

    private Dispatcher(int threadId)
    {
        _threadId = threadId;
    }

    /// <summary>
    /// Gets the dispatcher of the calling thread, creating it on first access.
    /// </summary>
    /// <remarks>
    /// Each thread has its own current dispatcher. It is attached to a <see cref="TerminalApp"/> for one run only;
    /// after the run, the next access creates a fresh dispatcher.
    /// From a background thread, this returns the background thread's own dispatcher, which is not attached to any app:
    /// use <see cref="TerminalApp"/>.<see cref="DispatcherObject.Dispatcher"/> to marshal work to a running app instead.
    /// </remarks>
    public static Dispatcher Current
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _current ?? CreateForCurrentThread();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Dispatcher CreateForCurrentThread() => _current = new Dispatcher();

    /// <summary>
    /// Creates a dispatcher that belongs to no thread (managed thread ids are positive) and can never be attached to an app.
    /// </summary>
    internal static Dispatcher CreateUnbound() => new(threadId: 0);

    internal int ThreadId => _threadId;

    internal TerminalApp? AttachedApp => Volatile.Read(ref _app);

    internal bool IsRunning => Volatile.Read(ref _app) is not null;

    internal Ownership? CurrentOwnership => Volatile.Read(ref _ownership);

    internal void BindToCurrentThread(TerminalApp app)
    {
        ArgumentNullException.ThrowIfNull(app);

        if (Environment.CurrentManagedThreadId != _threadId)
        {
            throw new InvalidOperationException("A dispatcher can only be attached to a TerminalApp from its own thread.");
        }

        var current = _app;
        if (current is not null)
        {
            if (!ReferenceEquals(current, app))
            {
                throw new InvalidOperationException("The dispatcher of this thread is already attached to another running TerminalApp. Only one app can run on a thread at a time.");
            }

            return;
        }

        _previousSynchronizationContext = SynchronizationContext.Current;
        Volatile.Write(ref _ownership, new Ownership(this));
        Volatile.Write(ref _app, app);
        _synchronizationContext = new DispatcherSynchronizationContext(this);
        SynchronizationContext.SetSynchronizationContext(_synchronizationContext);
    }

    internal void DetachFromThread(TerminalApp app)
    {
        if (!ReferenceEquals(_app, app))
        {
            return;
        }

        if (Environment.CurrentManagedThreadId != _threadId)
        {
            throw new InvalidOperationException("A dispatcher can only be detached from a TerminalApp on its own thread.");
        }

        var synchronizationContext = _synchronizationContext;
        _synchronizationContext = null;
        SynchronizationContext.SetSynchronizationContext(_previousSynchronizationContext);
        _previousSynchronizationContext = null;
        Volatile.Write(ref _ownership, null);
        Volatile.Write(ref _app, null);
        if (ReferenceEquals(_current, this))
        {
            _current = null;
        }
        synchronizationContext?.Detach();
    }

    // One token per run, shared by its objects: a subsequent app on the same thread must not revive old ownership.
    internal sealed class Ownership
    {
        internal Ownership(Dispatcher dispatcher)
        {
            Dispatcher = dispatcher;
            ThreadId = dispatcher.ThreadId;
        }

        internal Dispatcher Dispatcher { get; }

        internal int ThreadId { get; }

        internal bool IsRunning => ReferenceEquals(Dispatcher.CurrentOwnership, this);
    }

    /// <summary>
    /// Determines whether the caller is running on the thread of this dispatcher.
    /// </summary>
    /// <returns><see langword="true"/> if the caller has access; otherwise <see langword="false"/>.</returns>
    public bool CheckAccess() => Environment.CurrentManagedThreadId == _threadId;

    /// <summary>
    /// Throws if the caller is not running on the thread of this dispatcher.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the caller is not on the thread of this dispatcher.</exception>
    public void VerifyAccess()
    {
        if (!CheckAccess())
        {
            ThrowInvalidThreadAccess();
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void ThrowInvalidThreadAccess()
    {
        throw new InvalidOperationException("Invalid thread access. Use TerminalApp.Dispatcher to marshal to the UI thread.");
    }

    /// <summary>
    /// Posts an action to the thread of the app running on this dispatcher.
    /// </summary>
    /// <param name="action">The action to execute.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="action"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">Thrown when no <see cref="TerminalApp"/> is running on this dispatcher.</exception>
    public void Post(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);
        GetRunningApp().Post(action);
    }

    private TerminalApp GetRunningApp() => Volatile.Read(ref _app) ?? throw new InvalidOperationException(NotRunningMessage);

    /// <summary>
    /// Invokes an action on the UI thread.
    /// </summary>
    /// <param name="action">The action to execute.</param>
    /// <returns>A task that completes when the action finishes.</returns>
    /// <remarks>When called from the thread of this dispatcher, the action runs synchronously.</remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="action"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">Thrown when no <see cref="TerminalApp"/> is running on this dispatcher.</exception>
    public Task InvokeAsync(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);
        _ = GetRunningApp();

        if (CheckAccess())
        {
            action();
            return Task.CompletedTask;
        }

        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Post(() =>
        {
            try
            {
                action();
                tcs.SetResult();
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        });
        return tcs.Task;
    }

    /// <summary>
    /// Invokes a function on the UI thread and returns its result.
    /// </summary>
    /// <typeparam name="T">The result type.</typeparam>
    /// <param name="func">The function to execute.</param>
    /// <returns>A task that completes with the function result.</returns>
    /// <remarks>When called from the thread of this dispatcher, the function runs synchronously.</remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="func"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">Thrown when no <see cref="TerminalApp"/> is running on this dispatcher.</exception>
    public Task<T> InvokeAsync<T>(Func<T> func)
    {
        ArgumentNullException.ThrowIfNull(func);
        _ = GetRunningApp();

        if (CheckAccess())
        {
            return Task.FromResult(func());
        }

        var tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        Post(() =>
        {
            try
            {
                tcs.SetResult(func());
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        });
        return tcs.Task;
    }

    /// <summary>
    /// Invokes an asynchronous delegate on the UI thread.
    /// </summary>
    /// <param name="func">The delegate to execute.</param>
    /// <returns>A task that completes when the delegate finishes.</returns>
    /// <remarks>When called from the thread of this dispatcher, the delegate is started synchronously.</remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="func"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">Thrown when no <see cref="TerminalApp"/> is running on this dispatcher.</exception>
    public Task InvokeAsync(Func<Task> func)
    {
        ArgumentNullException.ThrowIfNull(func);
        _ = GetRunningApp();

        if (CheckAccess())
        {
            return func();
        }

        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Post(() =>
        {
            Task task;
            try
            {
                task = func();
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
                return;
            }

            task.ContinueWith(static (t, state) =>
            {
                var completion = (TaskCompletionSource)state!;
                if (t.IsFaulted)
                {
                    completion.TrySetException(t.Exception!);
                }
                else if (t.IsCanceled)
                {
                    completion.TrySetCanceled();
                }
                else
                {
                    completion.TrySetResult();
                }
            }, tcs, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
        });

        return tcs.Task;
    }

    /// <summary>
    /// Invokes an asynchronous delegate on the UI thread and returns its result.
    /// </summary>
    /// <typeparam name="T">The result type.</typeparam>
    /// <param name="func">The delegate to execute.</param>
    /// <returns>A task that completes with the result.</returns>
    /// <remarks>When called from the thread of this dispatcher, the delegate is started synchronously.</remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="func"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">Thrown when no <see cref="TerminalApp"/> is running on this dispatcher.</exception>
    public Task<T> InvokeAsync<T>(Func<Task<T>> func)
    {
        ArgumentNullException.ThrowIfNull(func);
        _ = GetRunningApp();

        if (CheckAccess())
        {
            return func();
        }

        var tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        Post(() =>
        {
            Task<T> task;
            try
            {
                task = func();
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
                return;
            }

            task.ContinueWith(static (t, state) =>
            {
                var completion = (TaskCompletionSource<T>)state!;
                if (t.IsFaulted)
                {
                    completion.TrySetException(t.Exception!);
                }
                else if (t.IsCanceled)
                {
                    completion.TrySetCanceled();
                }
                else
                {
                    completion.TrySetResult(t.Result);
                }
            }, tcs, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
        });

        return tcs.Task;
    }
}
