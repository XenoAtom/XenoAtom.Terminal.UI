// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI.Input;

public readonly record struct TerminalKeyGesture
{
    public TerminalKeyGesture(TerminalKey key, TerminalModifiers modifiers = TerminalModifiers.None)
    {
        Key = key;
        Char = null;
        Modifiers = modifiers;
    }

    public TerminalKeyGesture(char ch, TerminalModifiers modifiers = TerminalModifiers.None)
    {
        Key = TerminalKey.Unknown;
        Char = ch;
        Modifiers = modifiers;
    }

    public TerminalKey Key { get; }

    public char? Char { get; }

    public TerminalModifiers Modifiers { get; }

    public bool Matches(TerminalKeyEvent ev)
    {
        if (ev.Modifiers != Modifiers)
        {
            return false;
        }

        if (Key != TerminalKey.Unknown)
        {
            return ev.Key == Key;
        }

        if (Char is null)
        {
            return false;
        }

        if (ev.Char is null)
        {
            return false;
        }

        var expected = Char.Value;
        var actual = ev.Char.Value;
        if (char.IsLetter(expected) && char.IsLetter(actual))
        {
            return char.ToUpperInvariant(expected) == char.ToUpperInvariant(actual);
        }

        return expected == actual;
    }

    public override string ToString()
    {
        if (Key != TerminalKey.Unknown)
        {
            return $"{Modifiers}+{Key}";
        }

        return Char is null ? $"{Modifiers}+<none>" : $"{Modifiers}+{Char}";
    }
}

