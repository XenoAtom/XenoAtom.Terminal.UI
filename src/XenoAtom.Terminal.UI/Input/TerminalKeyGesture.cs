// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI.Input;

/// <summary>
/// Represents a key gesture defined by a key or character plus modifiers.
/// </summary>
public readonly record struct TerminalKeyGesture
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TerminalKeyGesture"/> struct with a key.
    /// </summary>
    /// <param name="key">The terminal key.</param>
    /// <param name="modifiers">The modifier flags.</param>
    public TerminalKeyGesture(TerminalKey key, TerminalModifiers modifiers = TerminalModifiers.None)
    {
        Key = key;
        Char = null;
        Modifiers = modifiers;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TerminalKeyGesture"/> struct with a character.
    /// </summary>
    /// <param name="ch">The character.</param>
    /// <param name="modifiers">The modifier flags.</param>
    public TerminalKeyGesture(char ch, TerminalModifiers modifiers = TerminalModifiers.None)
    {
        Key = TerminalKey.Unknown;
        Char = ch;
        Modifiers = modifiers;
    }

    /// <summary>
    /// Gets the key if the gesture is key-based; otherwise <see cref="TerminalKey.Unknown"/>.
    /// </summary>
    public TerminalKey Key { get; }

    /// <summary>
    /// Gets the character if the gesture is character-based.
    /// </summary>
    public char? Char { get; }

    /// <summary>
    /// Gets the modifier flags for the gesture.
    /// </summary>
    public TerminalModifiers Modifiers { get; }

    /// <summary>
    /// Determines whether the gesture matches a key event.
    /// </summary>
    /// <param name="ev">The key event.</param>
    /// <returns><see langword="true"/> if the event matches; otherwise <see langword="false"/>.</returns>
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

    /// <inheritdoc />
    public override string ToString()
    {
        if (Key != TerminalKey.Unknown)
        {
            return $"{Modifiers}+{Key}";
        }

        return Char is null ? $"{Modifiers}+<none>" : $"{Modifiers}+{Char}";
    }
}
