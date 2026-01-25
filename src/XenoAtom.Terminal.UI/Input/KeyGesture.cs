// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Text;

namespace XenoAtom.Terminal.UI.Input;

/// <summary>
/// Represents a key gesture defined by a key or character plus modifiers.
/// </summary>
public readonly record struct KeyGesture
{
    /// <summary>
    /// Initializes a new instance of the <see cref="KeyGesture"/> struct with a key.
    /// </summary>
    /// <param name="key">The terminal key.</param>
    /// <param name="modifiers">The modifier flags.</param>
    public KeyGesture(TerminalKey key, TerminalModifiers modifiers = TerminalModifiers.None)
    {
        Key = key;
        Char = null;
        Modifiers = modifiers;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="KeyGesture"/> struct with a character.
    /// </summary>
    /// <param name="ch">The character.</param>
    /// <param name="modifiers">The modifier flags.</param>
    public KeyGesture(char ch, TerminalModifiers modifiers = TerminalModifiers.None)
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
    /// Attempts to parse a textual representation produced by <see cref="ToString"/>.
    /// </summary>
    /// <param name="text">The input text.</param>
    /// <param name="gesture">The parsed gesture.</param>
    /// <returns><see langword="true"/> if parsing succeeded; otherwise <see langword="false"/>.</returns>
    public static bool TryParse(ReadOnlySpan<char> text, out KeyGesture gesture)
    {
        // This parser is intentionally scoped to what we emit today. It can be expanded if we need end-user parsing.
        text = text.Trim();
        if (text.IsEmpty)
        {
            gesture = default;
            return false;
        }

        var modifiers = TerminalModifiers.None;
        TerminalKey? key = null;
        char? ch = null;

        while (true)
        {
            var plusIndex = text.IndexOf('+');
            if (plusIndex < 0)
            {
                break;
            }

            var part = text[..plusIndex].Trim();
            if (part.Equals("Ctrl", StringComparison.OrdinalIgnoreCase))
            {
                modifiers |= TerminalModifiers.Ctrl;
            }
            else if (part.Equals("Alt", StringComparison.OrdinalIgnoreCase))
            {
                modifiers |= TerminalModifiers.Alt;
            }
            else if (part.Equals("Shift", StringComparison.OrdinalIgnoreCase))
            {
                modifiers |= TerminalModifiers.Shift;
            }
            else if (!part.IsEmpty)
            {
                gesture = default;
                return false;
            }

            text = text[(plusIndex + 1)..];
        }

        text = text.Trim();
        if (text.IsEmpty)
        {
            gesture = default;
            return false;
        }

        if (text.Length == 1)
        {
            ch = text[0];
        }
        else if (Enum.TryParse<TerminalKey>(text.ToString(), ignoreCase: true, out var parsedKey))
        {
            key = parsedKey;
        }
        else
        {
            gesture = default;
            return false;
        }

        if (key is { } k2)
        {
            gesture = new KeyGesture(k2, modifiers);
            return true;
        }

        var c = ch!.Value;
        if ((modifiers & TerminalModifiers.Ctrl) != 0 && char.IsLetter(c))
        {
            // Terminals typically emit a control character (1..26) for Ctrl+A..Ctrl+Z.
            // Parse "Ctrl+Z" into that control character so Matches works against TerminalKeyEvent.Char.
            var upper = char.ToUpperInvariant(c);
            if (upper is >= 'A' and <= 'Z')
            {
                c = (char)(upper - 'A' + 1);
            }
        }

        gesture = new KeyGesture(c, modifiers);
        return true;
    }

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

    /// <summary>
    /// Determines whether the gesture matches another gesture using the same comparison rules as <see cref="Matches(TerminalKeyEvent)"/>.
    /// </summary>
    /// <param name="other">The other gesture.</param>
    /// <returns><see langword="true"/> if the gestures match; otherwise <see langword="false"/>.</returns>
    public bool Matches(in KeyGesture other)
    {
        if (other.Modifiers != Modifiers)
        {
            return false;
        }

        if (Key != TerminalKey.Unknown)
        {
            return other.Key == Key;
        }

        if (Char is null)
        {
            return false;
        }

        if (other.Char is null)
        {
            return false;
        }

        var expected = Char.Value;
        var actual = other.Char.Value;
        if (char.IsLetter(expected) && char.IsLetter(actual))
        {
            return char.ToUpperInvariant(expected) == char.ToUpperInvariant(actual);
        }

        return expected == actual;
    }

    /// <inheritdoc />
    public override string ToString()
    {
        // The goal is a stable, readable representation suitable for key hints UI (e.g. "Ctrl+Z", "Ctrl+K Ctrl+P").
        // This is not intended to be a full localization layer.
        string keyText;
        if (Key != TerminalKey.Unknown)
        {
            keyText = Key.ToString();
        }
        else if (Char is { } ch)
        {
            // When terminals emit Ctrl+<letter>, they often provide a control character (1..26) + Ctrl modifier.
            // Prefer showing "Ctrl+Z" instead of a non-printable.
            if ((Modifiers & TerminalModifiers.Ctrl) != 0 && ch is >= (char)1 and <= (char)26)
            {
                keyText = ((char)('A' + ch - 1)).ToString();
            }
            else
            {
                keyText = ch.ToString();
            }
        }
        else
        {
            keyText = "<none>";
        }

        if (Modifiers == TerminalModifiers.None)
        {
            return keyText;
        }

        var builder = new StringBuilder();
        var needsPlus = false;
        if ((Modifiers & TerminalModifiers.Ctrl) != 0)
        {
            builder.Append("Ctrl");
            needsPlus = true;
        }

        if ((Modifiers & TerminalModifiers.Alt) != 0)
        {
            if (needsPlus) builder.Append('+');
            builder.Append("Alt");
            needsPlus = true;
        }

        if ((Modifiers & TerminalModifiers.Shift) != 0)
        {
            if (needsPlus) builder.Append('+');
            builder.Append("Shift");
            needsPlus = true;
        }

        builder.Append('+');
        builder.Append(keyText);
        return builder.ToString();
    }
}

