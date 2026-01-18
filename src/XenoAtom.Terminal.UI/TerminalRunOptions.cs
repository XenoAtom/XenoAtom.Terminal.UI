// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI;

/// <summary>
/// Provides options for fullscreen hosting via
/// <see cref="TerminalExtensions.Run(Visual, System.Func{TerminalRunningContext, TerminalLoopResult}, TerminalRunOptions)"/>.
/// </summary>
public readonly record struct TerminalRunOptions
{
    /// <summary>
    /// Gets the key gesture used to exit the fullscreen application.
    /// </summary>
    /// <remarks>
    /// When <see langword="null"/>, the default gesture is used (<c>Ctrl+Q</c>).
    /// </remarks>
    public global::XenoAtom.Terminal.UI.Input.TerminalKeyGesture? ExitGesture { get; init; }
}
