// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Globalization;
using System.Text;
using XenoAtom.Terminal;
using XenoAtom.Terminal.UI.Hosting;

namespace XenoAtom.Terminal.UI;

/// <summary>
/// Provides configuration options for <see cref="TerminalApp"/>.
/// </summary>
public sealed class TerminalAppOptions
{
    /// <summary>
    /// Gets the initial focus behavior for fullscreen applications.
    /// </summary>
    /// <remarks>
    /// This controls whether <see cref="TerminalApp"/> assigns an initial focused element when the app starts.
    /// Users can still focus controls using the mouse or tab navigation.
    /// </remarks>
    public InitialFocusMode InitialFocusMode { get; init; } = InitialFocusMode.FirstFocusable;

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
    public Input.KeyGesture ToggleDebugOverlayGesture { get; init; } = new(TerminalKey.F12);

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
    public Input.KeyGesture? ExitGesture { get; init; }

    /// <summary>
    /// Gets the culture used for formatting values (for example when converting numbers to strings).
    /// </summary>
    /// <remarks>
    /// The default value is <see cref="CultureInfo.InvariantCulture"/>. You can override culture for a visual subtree
    /// by setting <see cref="Styling.CultureStyle.Key"/>.
    /// </remarks>
    public CultureInfo Culture { get; init; } = CultureInfo.InvariantCulture;

    /// <summary>
    /// Gets the host loop mode.
    /// </summary>
    public TerminalLoopMode LoopMode { get; init; } = TerminalLoopMode.Auto;

    /// <summary>
    /// Gets the optional presenter used to turn collected graphics commands into terminal protocol output.
    /// </summary>
    /// <remarks>
    /// When this value is <see langword="null"/>, graphics-capable visuals are still allowed to emit display-list
    /// commands, but no protocol output is written by the core UI host. A non-null presenter is reset when the app starts
    /// and ends, and disposed when the app is disposed.
    /// </remarks>
    public ITerminalGraphicsPresenter? GraphicsPresenter { get; init; }

    /// <summary>
    /// Gets the maximum coarse wait slice used in <see cref="TerminalLoopMode.Polling"/>.
    /// </summary>
    /// <remarks>
    /// In <see cref="TerminalLoopMode.Auto"/>, the loop is deadline/event-driven and this value is not treated as a
    /// frame cadence setting.
    /// </remarks>
    public global::System.TimeSpan UpdateWaitDuration { get; init; } = global::System.TimeSpan.FromMilliseconds(1);

    /// <summary>
    /// Gets the predicate used to widen additional runes to two terminal cells.
    /// </summary>
    /// <remarks>
    /// The default value is <see cref="TerminalWideRuneResolvers.Default"/>, which widens emoji-like scalars only.
    /// Use <see cref="TerminalWideRuneResolvers.NerdFontDoubleWidth"/> only when your terminal/font combination
    /// actually renders Nerd Font glyphs as two cells.
    /// </remarks>
    public Func<Rune, bool>? WideRuneResolver { get; init; } = TerminalWideRuneResolvers.Default;
}

/// <summary>
/// Specifies how <see cref="TerminalApp"/> initializes focus when starting a run.
/// </summary>
public enum InitialFocusMode
{
    /// <summary>
    /// Do not assign focus automatically. Focus will remain <see langword="null"/> until the user
    /// clicks a focusable control or uses tab navigation.
    /// </summary>
    None,

    /// <summary>
    /// Focus the first focusable element in the current focus scope.
    /// </summary>
    /// <remarks>
    /// If a visual has <see cref="Visual.AutoFocus"/> set, that visual is preferred.
    /// </remarks>
    FirstFocusable,
}

/// <summary>
/// Specifies how the host loop decides when to wake between ticks.
/// </summary>
public enum TerminalLoopMode
{
    /// <summary>
    /// Uses deadline/event-driven pacing with a default active cadence while the host update callback remains active.
    /// </summary>
    Auto,

    /// <summary>
    /// Uses <see cref="TerminalAppOptions.UpdateWaitDuration"/> as the maximum polling slice between re-evaluations.
    /// </summary>
    Polling,
}
