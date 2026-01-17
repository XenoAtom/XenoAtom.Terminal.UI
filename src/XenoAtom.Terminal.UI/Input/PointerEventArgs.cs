// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI.Input;

/// <summary>
/// Provides data for pointer (mouse) routed events.
/// </summary>
public sealed class PointerEventArgs : RoutedEventArgs
{
    /// <summary>
    /// Gets the raw terminal mouse event.
    /// </summary>
    public required TerminalMouseEvent RawEvent { get; init; }

    /// <summary>
    /// Gets the pointer X in the root UI coordinate space (cells).
    /// </summary>
    public int UiX { get; init; }

    /// <summary>
    /// Gets the pointer Y in the root UI coordinate space (cells).
    /// </summary>
    public int UiY { get; init; }

    /// <summary>
    /// Gets the mouse event kind.
    /// </summary>
    public TerminalMouseKind Kind => RawEvent.Kind;

    /// <summary>
    /// Gets the mouse button associated with the event.
    /// </summary>
    public TerminalMouseButton Button => RawEvent.Button;

    /// <summary>
    /// Gets the modifier keys pressed during the event.
    /// </summary>
    public TerminalModifiers Modifiers => RawEvent.Modifiers;

    /// <summary>
    /// Gets the pointer X in the terminal coordinate space (cells).
    /// </summary>
    public int X => RawEvent.X;

    /// <summary>
    /// Gets the pointer Y in the terminal coordinate space (cells).
    /// </summary>
    public int Y => RawEvent.Y;

    /// <summary>
    /// Gets the wheel delta (when <see cref="Kind"/> indicates wheel scrolling).
    /// </summary>
    public int WheelDelta => RawEvent.WheelDelta;

    /// <summary>
    /// Gets the click count for the event.
    /// </summary>
    public int ClickCount { get; init; } = 1;

    /// <summary>
    /// Gets the X position relative to the event target.
    /// </summary>
    public int LocalX { get; init; }

    /// <summary>
    /// Gets the Y position relative to the event target.
    /// </summary>
    public int LocalY { get; init; }
}
