// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI.Threading;

public sealed class Dispatcher
{
    private readonly TerminalApp _app;
    private int? _threadId;

    internal Dispatcher(TerminalApp app)
    {
        _app = app;
    }

    internal void BindToCurrentThread()
    {
        _threadId = Environment.CurrentManagedThreadId;
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

    public void Post(Action action) => _app.Post(action);

    public Task InvokeAsync(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);

        if (CheckAccess())
        {
            action();
            return Task.CompletedTask;
        }

        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _app.Post(() =>
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
        _app.Post(() =>
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
}
