// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Diagnostics;

namespace XenoAtom.Terminal.UI.Threading;

internal interface ITerminalLoopClock
{
    long Frequency { get; }

    long GetTimestamp();
}

internal sealed class StopwatchTerminalLoopClock : ITerminalLoopClock
{
    public static StopwatchTerminalLoopClock Instance { get; } = new();

    private StopwatchTerminalLoopClock()
    {
    }

    public long Frequency => Stopwatch.Frequency;

    public long GetTimestamp() => Stopwatch.GetTimestamp();
}
