// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Text;

namespace XenoAtom.Terminal.UI;

/// <summary>
/// Provides options for fullscreen hosting via
/// <see cref="TerminalExtensions.Run(Visual, System.Func{TerminalRunningContext, TerminalLoopResult}, TerminalRunOptions)"/>.
/// </summary>
public readonly record struct TerminalRunOptions()
{
    /// <summary>
    /// Gets the culture used for formatting values (for example when converting numbers to strings).
    /// </summary>
    /// <remarks>
    /// When <see langword="null"/>, <see cref="System.Globalization.CultureInfo.InvariantCulture"/> is used.
    /// </remarks>
    public System.Globalization.CultureInfo? Culture { get; init; }

    /// <summary>
    /// Gets the key gesture used to exit the fullscreen application.
    /// </summary>
    /// <remarks>
    /// When <see langword="null"/>, the default gesture is used (<c>Ctrl+Q</c>).
    /// </remarks>
    public global::XenoAtom.Terminal.UI.Input.KeyGesture? ExitGesture { get; init; }

    /// <summary>
    /// Gets the optional presenter used to emit terminal graphics commands after each text frame.
    /// </summary>
    /// <remarks>
    /// Leave this value <see langword="null"/> when the application does not use graphics-renderable visuals.
    /// </remarks>
    public ITerminalGraphicsPresenter? GraphicsPresenter { get; init; }

    /// <summary>
    /// Gets the host loop mode.
    /// </summary>
    public TerminalLoopMode LoopMode { get; init; } = TerminalLoopMode.Auto;

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
    /// When <see langword="null"/>, <see cref="TerminalWideRuneResolvers.Default"/> is used.
    /// </remarks>
    public Func<Rune, bool>? WideRuneResolver { get; init; }
}
