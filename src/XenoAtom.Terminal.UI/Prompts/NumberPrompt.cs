// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Globalization;
using System.Numerics;
using XenoAtom.Terminal.UI.Controls;

namespace XenoAtom.Terminal.UI.Prompts;

/// <summary>
/// Represents an inline prompt for editing a numeric value.
/// </summary>
/// <typeparam name="T">The numeric type.</typeparam>
public sealed class NumberPrompt<T> : TerminalPrompt<T> where T : struct, INumber<T>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NumberPrompt{T}"/> class.
    /// </summary>
    public NumberPrompt()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NumberPrompt{T}"/> class with a message.
    /// </summary>
    /// <param name="message">The prompt message.</param>
    public NumberPrompt(Visual message)
    {
        Message = message;
    }

    /// <summary>
    /// Gets or sets the initial value displayed in the editor.
    /// </summary>
    public T InitialValue { get; init; }

    /// <summary>
    /// Gets or sets the message displayed when the input cannot be parsed as a number.
    /// </summary>
    public string? InvalidNumberMessage { get; init; }

    /// <summary>
    /// Gets or sets the number styles used to parse the input text.
    /// </summary>
    public NumberStyles ParseStyles { get; init; } = NumberStyles.Number;

    /// <summary>
    /// Gets or sets the format provider used to parse and format values.
    /// </summary>
    public IFormatProvider? FormatProvider { get; init; }

    internal override PromptSession<T> CreateSession()
    {
        var numberBox = new NumberBox<T>(InitialValue)
        {
            ShowValidationMessage = true,
            ParseStyles = ParseStyles,
            FormatProvider = FormatProvider,
        };

        if (!string.IsNullOrEmpty(InvalidNumberMessage))
        {
            numberBox.InvalidNumberMessage = InvalidNumberMessage;
        }

        if (Validator is { } validator)
        {
            numberBox.ValueValidator = validator;
        }

        var content = BuildPromptLayout(numberBox);

        var session = new PromptSession<T>(
            tryGetValue: () => TryParseCurrentText(numberBox),
            validator: Validator,
            keepOnSuccess: KeepOnSuccess);

        var host = new PromptHost(content, session.TryConfirm, session.Cancel);
        session.SetRoot(host, numberBox);
        return session;
    }

    private static (bool Ok, T Value) TryParseCurrentText(NumberBox<T> numberBox)
    {
        var text = numberBox.Text ?? string.Empty;
        if (text.Length == 0)
        {
            return (false, default);
        }

        return T.TryParse(text.AsSpan(), numberBox.ParseStyles, numberBox.FormatProvider, out var value)
            ? (true, value)
            : (false, default);
    }

    private Visual BuildPromptLayout(Visual editor)
    {
        var message = Message;
        var help = Help;

        var stack = help is null
            ? new VStack(message, editor).Spacing(1)
            : new VStack(message, editor, help).Spacing(1);

        return new Group()
            .Padding(1)
            .Content(stack);
    }
}

