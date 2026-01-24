// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Globalization;
using System.Numerics;
using XenoAtom.Terminal.UI;
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
    /// Gets or sets the default value displayed in the editor.
    /// </summary>
    [Fluent("Default")]
    public T DefaultValue { get; set; }

    /// <summary>
    /// Gets or sets the message displayed when the input cannot be parsed as a number.
    /// </summary>
    [Fluent]
    public string? InvalidNumberMessage { get; set; }

    /// <summary>
    /// Gets or sets the number styles used to parse the input text.
    /// </summary>
    [Fluent]
    public NumberStyles ParseStyles { get; set; } = NumberStyles.Number;

    /// <summary>
    /// Gets or sets the format provider used to parse and format values.
    /// </summary>
    [Fluent]
    public IFormatProvider? FormatProvider { get; set; }

    internal override PromptSession<T> CreateSession()
    {
        var validator = Validator.Invoke;
        var numberBox = new NumberBox<T>(DefaultValue)
        {
            ShowValidationMessage = true,
            ParseStyles = ParseStyles,
            FormatProvider = FormatProvider,
        };

        if (!string.IsNullOrEmpty(InvalidNumberMessage))
        {
            numberBox.InvalidNumberMessage = InvalidNumberMessage;
        }

        if (validator is { } validate)
        {
            numberBox.ValueValidator = validate;
        }

        var content = BuildPromptLayout(numberBox);

        var session = new PromptSession<T>(
            tryGetValue: () => TryParseCurrentText(numberBox),
            validator: validator,
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

}
