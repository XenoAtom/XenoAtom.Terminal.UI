// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Threading;

namespace XenoAtom.Terminal.UI.Threading;

internal sealed class DispatcherSynchronizationContext : SynchronizationContext
{
    private readonly Dispatcher _dispatcher;
    private readonly Lock _lock = new();
    private HashSet<PendingCallback>? _pending;
    private bool _isDetached;

    public DispatcherSynchronizationContext(Dispatcher dispatcher)
    {
        _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
    }

    public override void Post(SendOrPostCallback d, object? state)
    {
        ArgumentNullException.ThrowIfNull(d);

        lock (_lock)
        {
            if (!_isDetached && _dispatcher.IsRunning)
            {
                var callback = new PendingCallback(this, d, state);
                (_pending ??= []).Add(callback);
                try
                {
                    _dispatcher.Post(callback.Invoke);
                    return;
                }
                catch (InvalidOperationException) when (!_dispatcher.IsRunning)
                {
                    // The run ended between the check and enqueue. Resume through the default context instead.
                    _pending.Remove(callback);
                }
                catch
                {
                    _pending.Remove(callback);
                    throw;
                }
            }
        }

        base.Post(d, state);
    }

    public override void Send(SendOrPostCallback d, object? state)
    {
        ArgumentNullException.ThrowIfNull(d);

        if (Volatile.Read(ref _isDetached))
        {
            base.Send(d, state);
            return;
        }

        if (_dispatcher.CheckAccess())
        {
            d(state);
            return;
        }

        using var evt = new ManualResetEventSlim(false);
        Exception? captured = null;

        Post(_ =>
        {
            try
            {
                d(state);
            }
            catch (Exception ex)
            {
                captured = ex;
            }
            finally
            {
                evt.Set();
            }
        }, null);

        evt.Wait();
        if (captured is not null)
        {
            throw new AggregateException(captured);
        }
    }

    public override SynchronizationContext CreateCopy() => this;

    internal void Detach()
    {
        PendingCallback[] pending;
        lock (_lock)
        {
            _isDetached = true;
            pending = _pending is null ? [] : [.. _pending];
            _pending?.Clear();
        }

        // A stopped run cannot process its queue. Release captured awaits without routing them to a later app.
        foreach (var callback in pending)
        {
            base.Post(static state => ((PendingCallback)state!).InvokeDetached(), callback);
        }
    }

    private sealed class PendingCallback(DispatcherSynchronizationContext context, SendOrPostCallback callback, object? state)
    {
        public void Invoke()
        {
            lock (context._lock)
            {
                if (context._pending?.Remove(this) != true)
                {
                    return;
                }
            }

            callback(state);
        }

        public void InvokeDetached() => callback(state);
    }
}

