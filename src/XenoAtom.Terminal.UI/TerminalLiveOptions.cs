// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal;

namespace XenoAtom.Terminal.UI;

/// <summary>
/// Provides options for inline hosting via
/// <see cref="TerminalExtensions.Live(Visual, System.Func{TerminalRunningContext, TerminalLoopResult}, TerminalLiveOptions)"/>.
/// </summary>
public readonly record struct TerminalLiveOptions()
{
    /// <summary>
    /// Gets the culture used for formatting values (for example when converting numbers to strings).
    /// </summary>
    /// <remarks>
    /// When <see langword="null"/>, <see cref="System.Globalization.CultureInfo.InvariantCulture"/> is used.
    /// </remarks>
    public System.Globalization.CultureInfo? Culture { get; init; }

    /// <summary>
    /// Gets a value indicating whether mouse reporting is enabled for the live region.
    /// </summary>
    /// <remarks>
    /// The default value is <see langword="false"/> so the terminal emulator can keep its default mouse behavior
    /// (e.g. text selection).
    /// </remarks>
    public bool EnableMouse { get; init; }

    /// <summary>
    /// Gets the mouse reporting mode used when <see cref="EnableMouse"/> is <see langword="true"/>.
    /// </summary>
    public TerminalMouseMode MouseMode { get; init; } = TerminalMouseMode.Move;

    /// <summary>
    /// Gets the wait duration between update ticks.
    /// </summary>
    /// <remarks>
    /// The default is 1ms. Increase this value to reduce update frequency and CPU usage.
    /// </remarks>
    public global::System.TimeSpan UpdateWaitDuration { get; init; } = global::System.TimeSpan.FromMilliseconds(1);
}
