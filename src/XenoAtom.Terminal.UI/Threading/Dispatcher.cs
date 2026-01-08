// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI.Threading;

public sealed class Dispatcher
{
    public static Dispatcher Current { get; } = new();

    private int? _threadId;
    private TerminalApp? _app;
    private SynchronizationContext? _previousSynchronizationContext;

    private Dispatcher() { }

    internal TerminalApp? AttachedApp => _app;

    internal void BindToCurrentThread(TerminalApp app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var currentThreadId = Environment.CurrentManagedThreadId;
        var threadId = _threadId;
        if (threadId is not null && threadId.Value != currentThreadId)
        {
            throw new InvalidOperationException("The UI dispatcher is already bound to another thread.");
        }

        if (_app is not null && !ReferenceEquals(_app, app))
        {
            throw new InvalidOperationException("The UI dispatcher is already attached to another TerminalApp.");
        }

        _threadId = Environment.CurrentManagedThreadId;
        _app = app;

        if (_previousSynchronizationContext is null)
        {
            _previousSynchronizationContext = SynchronizationContext.Current;
        }

        SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(this));
    }

    internal void DetachFromThread(TerminalApp app)
    {
        if (!ReferenceEquals(_app, app))
        {
            return;
        }

        SynchronizationContext.SetSynchronizationContext(_previousSynchronizationContext);
        _previousSynchronizationContext = null;
        _app = null;
        _threadId = null;
    }

    public bool CheckAccess()
    {
        var threadId = _threadId;
        return threadId is null || Environment.CurrentManagedThreadId == threadId.Value;
    }

    public void VerifyAccess()
    {
        if (!CheckAccess())
        {
            throw new InvalidOperationException("Invalid thread access. Use TerminalApp.Dispatcher to marshal to the UI thread.");
        }
    }

    public void Post(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);
        var app = _app ?? throw new InvalidOperationException("Dispatcher is not attached to a running TerminalApp.");
        app.Post(action);
    }

    public Task InvokeAsync(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);

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

    public Task<T> InvokeAsync<T>(Func<T> func)
    {
        ArgumentNullException.ThrowIfNull(func);

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

    public Task InvokeAsync(Func<Task> func)
    {
        ArgumentNullException.ThrowIfNull(func);

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

    public Task<T> InvokeAsync<T>(Func<Task<T>> func)
    {
        ArgumentNullException.ThrowIfNull(func);

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
