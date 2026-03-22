// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace XenoAtom.Terminal.UI.Threading;

internal enum TerminalLoopWaitResult
{
    Deadline = 0,
    WakeSignal = 1,
    Canceled = 2,
}

internal interface ITerminalLoopWaitBackend : IDisposable
{
    TerminalLoopWaitResult WaitUntil(long deadline, AutoResetEvent wakeSignal, CancellationToken cancellationToken);
}

internal static class TerminalLoopWaitBackendFactory
{
    public static ITerminalLoopWaitBackend CreateDefault(ITerminalLoopClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);

        if (OperatingSystem.IsWindows() && WindowsTerminalLoopWaitBackend.TryCreate(clock, out var backend))
        {
            return backend;
        }

        return new TimeoutTerminalLoopWaitBackend(clock);
    }
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

    public void Dispose()
    {
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

internal sealed partial class WindowsTerminalLoopWaitBackend : ITerminalLoopWaitBackend
{
    private const uint CreateWaitableTimerHighResolution = 0x00000002;
    private const uint Synchronize = 0x00100000;
    private const uint TimerModifyState = 0x0002;
    private const uint WaitObject0 = 0x00000000;
    private const uint WaitFailed = 0xFFFFFFFF;

    private readonly ITerminalLoopClock _clock;
    private readonly SafeWaitHandle _timerHandle;
    private readonly TimeoutTerminalLoopWaitBackend _fallback;

    private WindowsTerminalLoopWaitBackend(ITerminalLoopClock clock, SafeWaitHandle timerHandle)
    {
        _clock = clock;
        _timerHandle = timerHandle;
        _fallback = new TimeoutTerminalLoopWaitBackend(clock);
    }

    public static bool TryCreate(ITerminalLoopClock clock, out ITerminalLoopWaitBackend backend)
    {
        ArgumentNullException.ThrowIfNull(clock);

        var desiredAccess = Synchronize | TimerModifyState;
        var timerHandle = Win32.CreateWaitableTimerEx(IntPtr.Zero, null, CreateWaitableTimerHighResolution, desiredAccess);
        if (timerHandle.IsInvalid)
        {
            timerHandle.Dispose();
            timerHandle = Win32.CreateWaitableTimerEx(IntPtr.Zero, null, flags: 0, desiredAccess);
        }

        if (timerHandle.IsInvalid)
        {
            timerHandle.Dispose();
            backend = null!;
            return false;
        }

        backend = new WindowsTerminalLoopWaitBackend(clock, timerHandle);
        return true;
    }

    public TerminalLoopWaitResult WaitUntil(long deadline, AutoResetEvent wakeSignal, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(wakeSignal);

        if (cancellationToken.IsCancellationRequested)
        {
            return TerminalLoopWaitResult.Canceled;
        }

        var now = _clock.GetTimestamp();
        if (deadline <= now)
        {
            return TerminalLoopWaitResult.Deadline;
        }

        var relativeDueTime = ToRelativeDueTime100Nanoseconds(deadline - now, _clock.Frequency);
        if (relativeDueTime == 0)
        {
            return TerminalLoopWaitResult.Deadline;
        }

        if (!Win32.SetWaitableTimerEx(_timerHandle, in relativeDueTime, 0, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, 0))
        {
            return _fallback.WaitUntil(deadline, wakeSignal, cancellationToken);
        }

        var addRefTimer = false;
        var addRefWake = false;
        var addRefCancel = false;
        try
        {
            _timerHandle.DangerousAddRef(ref addRefTimer);
            wakeSignal.SafeWaitHandle.DangerousAddRef(ref addRefWake);
            cancellationToken.WaitHandle.SafeWaitHandle.DangerousAddRef(ref addRefCancel);

            var handles = new[]
            {
                _timerHandle.DangerousGetHandle(),
                wakeSignal.SafeWaitHandle.DangerousGetHandle(),
                cancellationToken.WaitHandle.SafeWaitHandle.DangerousGetHandle(),
            };

            var result = Win32.WaitForMultipleObjects((uint)handles.Length, handles, waitAll: false, timeoutMilliseconds: uint.MaxValue);
            return result switch
            {
                WaitObject0 => TerminalLoopWaitResult.Deadline,
                WaitObject0 + 1 => TerminalLoopWaitResult.WakeSignal,
                WaitObject0 + 2 => TerminalLoopWaitResult.Canceled,
                WaitFailed => _fallback.WaitUntil(deadline, wakeSignal, cancellationToken),
                _ => TerminalLoopWaitResult.Deadline,
            };
        }
        finally
        {
            if (addRefCancel)
            {
                cancellationToken.WaitHandle.SafeWaitHandle.DangerousRelease();
            }

            if (addRefWake)
            {
                wakeSignal.SafeWaitHandle.DangerousRelease();
            }

            if (addRefTimer)
            {
                _timerHandle.DangerousRelease();
            }
        }
    }

    public void Dispose()
    {
        _fallback.Dispose();
        _timerHandle.Dispose();
    }

    private static long ToRelativeDueTime100Nanoseconds(long remainingStopwatchTicks, long frequency)
    {
        if (remainingStopwatchTicks <= 0 || frequency <= 0)
        {
            return 0;
        }

        var dueTime = (long)Math.Ceiling(remainingStopwatchTicks * 10_000_000d / frequency);
        return dueTime <= 0 ? -1L : -dueTime;
    }

    private static partial class Win32
    {
        [LibraryImport("kernel32.dll", EntryPoint = "CreateWaitableTimerExW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        private static partial SafeWaitHandle CreateWaitableTimerExCore(IntPtr timerAttributes, string? timerName, uint flags, uint desiredAccess);

        [LibraryImport("kernel32.dll", EntryPoint = "SetWaitableTimerEx", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool SetWaitableTimerEx(SafeWaitHandle timerHandle, in long dueTime, int periodMilliseconds, IntPtr completionRoutine, IntPtr arg, IntPtr wakeContext, uint tolerableDelayMilliseconds);

        [LibraryImport("kernel32.dll", EntryPoint = "WaitForMultipleObjects", SetLastError = true)]
        internal static partial uint WaitForMultipleObjects(uint count, IntPtr[] handles, [MarshalAs(UnmanagedType.Bool)] bool waitAll, uint timeoutMilliseconds);

        internal static SafeWaitHandle CreateWaitableTimerEx(IntPtr timerAttributes, string? timerName, uint flags, uint desiredAccess)
            => CreateWaitableTimerExCore(timerAttributes, timerName, flags, desiredAccess);
    }
}
