// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal;

namespace XenoAtom.Terminal.UI;

public sealed class PointerEventArgs : RoutedEventArgs
{
    public required TerminalMouseEvent RawEvent { get; init; }

    /// <summary>
    /// Gets the pointer X in the root UI coordinate space (cells).
    /// </summary>
    public int UiX { get; init; }

    /// <summary>
    /// Gets the pointer Y in the root UI coordinate space (cells).
    /// </summary>
    public int UiY { get; init; }

    public TerminalMouseKind Kind => RawEvent.Kind;

    public TerminalMouseButton Button => RawEvent.Button;

    public TerminalModifiers Modifiers => RawEvent.Modifiers;

    public int X => RawEvent.X;

    public int Y => RawEvent.Y;

    public int WheelDelta => RawEvent.WheelDelta;

    public int ClickCount { get; init; } = 1;

    public int LocalX { get; init; }

    public int LocalY { get; init; }
}
