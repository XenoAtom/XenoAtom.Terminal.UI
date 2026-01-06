// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI.Input;

public sealed class KeyEventArgs : RoutedEventArgs
{
    public required TerminalKeyEvent RawEvent { get; init; }

    public TerminalKey Key => RawEvent.Key;

    public char? Char => RawEvent.Char;

    public TerminalModifiers Modifiers => RawEvent.Modifiers;
}

