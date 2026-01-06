// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Threading;

namespace XenoAtom.Terminal.UI.Threading;

internal sealed class DispatcherSynchronizationContext : SynchronizationContext
{
    private readonly Dispatcher _dispatcher;

    public DispatcherSynchronizationContext(Dispatcher dispatcher)
    {
        _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
    }

    public override void Post(SendOrPostCallback d, object? state)
    {
        ArgumentNullException.ThrowIfNull(d);
        _dispatcher.Post(() => d(state));
    }

    public override void Send(SendOrPostCallback d, object? state)
    {
        ArgumentNullException.ThrowIfNull(d);

        if (_dispatcher.CheckAccess())
        {
            d(state);
            return;
        }

        using var evt = new ManualResetEventSlim(false);
        Exception? captured = null;

        _dispatcher.Post(() =>
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
        });

        evt.Wait();
        if (captured is not null)
        {
            throw new AggregateException(captured);
        }
    }

    public override SynchronizationContext CreateCopy() => this;
}

