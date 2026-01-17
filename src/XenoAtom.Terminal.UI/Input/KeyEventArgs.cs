// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI.Input;

/// <summary>
/// Provides data for key events.
/// </summary>
public sealed class KeyEventArgs : RoutedEventArgs
{
    /// <summary>
    /// Gets the raw key event from the terminal.
    /// </summary>
    public required TerminalKeyEvent RawEvent { get; init; }

    /// <summary>
    /// Gets the logical key.
    /// </summary>
    public TerminalKey Key => RawEvent.Key;

    /// <summary>
    /// Gets the character associated with the key, if any.
    /// </summary>
    public char? Char => RawEvent.Char;

    /// <summary>
    /// Gets the modifier keys.
    /// </summary>
    public TerminalModifiers Modifiers => RawEvent.Modifiers;
}
