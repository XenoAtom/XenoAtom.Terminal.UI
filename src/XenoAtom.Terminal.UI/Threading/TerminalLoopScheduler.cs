// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI.Threading;

internal static class TerminalLoopScheduler
{
    public static long ToStopwatchTicks(TimeSpan duration, long frequency)
    {
        if (duration <= TimeSpan.Zero)
        {
            return 0;
        }

        var ticks = (long)(duration.TotalSeconds * frequency);
        return ticks <= 0 ? 1 : ticks;
    }

    public static long ComputePollingDeadline(long now, long animationDeadline, long pollingSliceTicks)
    {
        if (animationDeadline == 0)
        {
            return now;
        }

        if (animationDeadline != long.MaxValue && animationDeadline <= now)
        {
            return now;
        }

        if (pollingSliceTicks <= 0)
        {
            return animationDeadline == long.MaxValue ? now : animationDeadline;
        }

        var pollingDeadline = now + pollingSliceTicks;
        if (animationDeadline == long.MaxValue)
        {
            return pollingDeadline;
        }

        return Math.Min(pollingDeadline, animationDeadline);
    }
}
