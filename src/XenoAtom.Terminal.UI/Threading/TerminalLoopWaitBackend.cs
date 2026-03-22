// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI.Threading;

internal enum TerminalLoopWaitResult
{
    Deadline = 0,
    WakeSignal = 1,
    Canceled = 2,
}

internal interface ITerminalLoopWaitBackend
{
    TerminalLoopWaitResult WaitUntil(long deadline, AutoResetEvent wakeSignal, CancellationToken cancellationToken);
}

internal sealed class TimeoutTerminalLoopWaitBackend : ITerminalLoopWaitBackend
{
    private readonly ITerminalLoopClock _clock;

    public TimeoutTerminalLoopWaitBackend(ITerminalLoopClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        _clock = clock;
    }

    public TerminalLoopWaitResult WaitUntil(long deadline, AutoResetEvent wakeSignal, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(wakeSignal);

        if (cancellationToken.IsCancellationRequested)
        {
            return TerminalLoopWaitResult.Canceled;
        }

        var remainingTicks = Math.Max(0, deadline - _clock.GetTimestamp());
        if (remainingTicks <= 0)
        {
            return TerminalLoopWaitResult.Deadline;
        }

        var timeout = ToTimeoutMilliseconds(remainingTicks, _clock.Frequency);
        if (timeout <= 0)
        {
            return TerminalLoopWaitResult.Deadline;
        }

        var handles = new WaitHandle[] { wakeSignal, cancellationToken.WaitHandle };
        var signaled = WaitHandle.WaitAny(handles, timeout);
        return signaled switch
        {
            WaitHandle.WaitTimeout => TerminalLoopWaitResult.Deadline,
            0 => TerminalLoopWaitResult.WakeSignal,
            1 => TerminalLoopWaitResult.Canceled,
            _ => TerminalLoopWaitResult.Deadline,
        };
    }

    internal static int ToTimeoutMilliseconds(long ticks, long frequency)
    {
        if (ticks <= 0 || frequency <= 0)
        {
            return 0;
        }

        var milliseconds = (ticks * 1000L) / frequency;
        if (milliseconds <= 0)
        {
            return 1;
        }

        return milliseconds >= int.MaxValue ? int.MaxValue : (int)milliseconds;
    }
}
