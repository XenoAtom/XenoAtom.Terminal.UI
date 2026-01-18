// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal;
using XenoAtom.Terminal.UI.Hosting;

namespace XenoAtom.Terminal.UI;

/// <summary>
/// Provides configuration options for <see cref="TerminalApp"/>.
/// </summary>
public sealed class TerminalAppOptions
{
    /// <summary>
    /// Gets the host kind used to run the application.
    /// </summary>
    public TerminalHostKind HostKind { get; init; } = TerminalHostKind.Inline;

    /// <summary>
    /// Gets the raw mode configuration for terminal input.
    /// </summary>
    public TerminalRawModeKind RawMode { get; init; } = TerminalRawModeKind.CBreak;

    /// <summary>
    /// Gets a value indicating whether input echo is disabled.
    /// </summary>
    public bool DisableInputEcho { get; init; } = true;

    /// <summary>
    /// Gets a value indicating whether mouse reporting is enabled.
    /// </summary>
    public bool EnableMouse { get; init; } = true;

    /// <summary>
    /// Gets the mouse reporting mode.
    /// </summary>
    public TerminalMouseMode MouseMode { get; init; } = TerminalMouseMode.Move;

    /// <summary>
    /// Gets a value indicating whether bracketed paste mode is enabled.
    /// </summary>
    public bool EnableBracketedPaste { get; init; } = true;

    /// <summary>
    /// Gets the key gesture used to toggle the debug overlay.
    /// </summary>
    public Input.TerminalKeyGesture ToggleDebugOverlayGesture { get; init; } = new(TerminalKey.F12);

    /// <summary>
    /// Gets the key gesture used to request application exit.
    /// </summary>
    /// <remarks>
    /// When <see langword="null"/>, the default gesture is host-dependent:
    /// <list type="bullet">
    /// <item><description>Inline: <c>Escape</c></description></item>
    /// <item><description>Fullscreen: <c>Ctrl+Q</c></description></item>
    /// </list>
    /// </remarks>
    public Input.TerminalKeyGesture? ExitGesture { get; init; }
}
