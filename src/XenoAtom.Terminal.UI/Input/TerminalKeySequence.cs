// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Text;

namespace XenoAtom.Terminal.UI.Input;

/// <summary>
/// Represents a multi-stroke keyboard shortcut (a sequence of <see cref="KeyGesture"/> values).
/// </summary>
/// <remarks>
/// Sequences are typically short (for example, <c>Ctrl+K</c> then <c>Ctrl+P</c>), and are primarily intended to support
/// Emacs-style “prefix” shortcuts without complicating single-stroke bindings.
/// </remarks>
public readonly record struct TerminalKeySequence
{
    private readonly KeyGesture[] _gestures;

    /// <summary>
    /// Initializes a new instance of the <see cref="TerminalKeySequence"/> struct.
    /// </summary>
    /// <param name="gestures">The gestures making up the sequence.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="gestures"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="gestures"/> is empty.</exception>
    public TerminalKeySequence(params KeyGesture[] gestures)
    {
        ArgumentNullException.ThrowIfNull(gestures);
        if (gestures.Length == 0)
        {
            throw new ArgumentException("A key sequence must contain at least one gesture.", nameof(gestures));
        }

        _gestures = new KeyGesture[gestures.Length];
        gestures.CopyTo(_gestures, 0);
    }

    /// <summary>
    /// Gets the gestures making up the sequence.
    /// </summary>
    public ReadOnlySpan<KeyGesture> Gestures => _gestures;

    /// <summary>
    /// Gets the number of gestures in the sequence.
    /// </summary>
    public int Count => _gestures.Length;

    /// <summary>
    /// Gets the gesture at a given index.
    /// </summary>
    /// <param name="index">The gesture index.</param>
    /// <returns>The gesture.</returns>
    public KeyGesture this[int index] => _gestures[index];

    /// <inheritdoc />
    public override string ToString()
    {
        if (_gestures.Length == 1)
        {
            return _gestures[0].ToString();
        }

        var builder = new StringBuilder();
        for (var i = 0; i < _gestures.Length; i++)
        {
            if (i > 0)
            {
                builder.Append(' ');
            }

            builder.Append(_gestures[i].ToString());
        }

        return builder.ToString();
    }

    /// <summary>
    /// Attempts to parse a textual representation produced by <see cref="ToString"/>.
    /// </summary>
    /// <param name="text">The input text.</param>
    /// <param name="sequence">The parsed sequence.</param>
    /// <returns><see langword="true"/> if parsing succeeded; otherwise <see langword="false"/>.</returns>
    public static bool TryParse(ReadOnlySpan<char> text, out TerminalKeySequence sequence)
    {
        // This parser is intentionally conservative: it targets sequences formatted by KeyGesture.ToString().
        // It can be expanded later if we want richer parsing for user input.
        text = text.Trim();
        if (text.IsEmpty)
        {
            sequence = default;
            return false;
        }

        // Split on whitespace.
        Span<KeyGesture> gestures = stackalloc KeyGesture[4];
        var count = 0;
        while (!text.IsEmpty)
        {
            text = text.TrimStart();
            var nextSpace = text.IndexOfAny(" \t\r\n".AsSpan());
            var token = nextSpace < 0 ? text : text[..nextSpace];
            if (!KeyGesture.TryParse(token, out var gesture))
            {
                sequence = default;
                return false;
            }

            if (count >= gestures.Length)
            {
                // Sequences are expected to be short; reject unusually long inputs in v1.
                sequence = default;
                return false;
            }

            gestures[count++] = gesture;
            text = nextSpace < 0 ? ReadOnlySpan<char>.Empty : text[(nextSpace + 1)..];
        }

        var array = new KeyGesture[count];
        for (var i = 0; i < count; i++)
        {
            array[i] = gestures[i];
        }

        sequence = new TerminalKeySequence(array);
        return true;
    }
}
