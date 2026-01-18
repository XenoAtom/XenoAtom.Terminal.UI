// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI;

/// <summary>
/// Represents the result of a hosted update callback invoked by
/// <see cref="TerminalExtensions.Live(Visual, System.Func{TerminalLoopResult})"/> or
/// <see cref="TerminalExtensions.Run(Visual, System.Func{TerminalLoopResult})"/>.
/// </summary>
public enum TerminalLoopResult
{
    /// <summary>
    /// Continue running.
    /// </summary>
    Continue = 0,

    /// <summary>
    /// Stop running and remove the live region (inline hosting).
    /// </summary>
    /// <remarks>
    /// For fullscreen hosting, this value simply stops the application.
    /// </remarks>
    Stop = 1,

    /// <summary>
    /// Stop running and keep the final live region on screen (inline hosting).
    /// </summary>
    /// <remarks>
    /// For fullscreen hosting, this value is equivalent to <see cref="Stop"/>.
    /// </remarks>
    StopAndKeepVisual = 2,
}
