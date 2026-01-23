// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Controls;

namespace XenoAtom.Terminal.UI.Prompts;

/// <summary>
/// Represents an inline prompt for editing a value using a <see cref="MaskedInput"/> template.
/// </summary>
public sealed class MaskedPrompt : TerminalPrompt<string>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MaskedPrompt"/> class.
    /// </summary>
    public MaskedPrompt()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MaskedPrompt"/> class with a message.
    /// </summary>
    /// <param name="message">The prompt message.</param>
    public MaskedPrompt(Visual message)
    {
        Message = message;
    }

    /// <summary>
    /// Gets or sets the template string used by the underlying <see cref="MaskedInput"/>.
    /// </summary>
    public string Template { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the initial value (slot characters) used by the underlying <see cref="MaskedInput"/>.
    /// </summary>
    public string? InitialValue { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether the prompt returns <see cref="MaskedInput.CompactValue"/> instead of <see cref="MaskedInput.Value"/>.
    /// </summary>
    public bool UseCompactValue { get; init; } = true;

    internal override PromptSession<string> CreateSession()
    {
        var input = new MaskedInput(Template);
        if (!string.IsNullOrEmpty(InitialValue))
        {
            input.Value = InitialValue;
        }

        Visual editor = input;
        if (Validator is { } validator)
        {
            editor = new ValidationPresenter()
                .Content(input)
                .Message(() =>
                {
                    var value = UseCompactValue ? input.CompactValue : (input.Value ?? string.Empty);
                    var message = validator(value);
                    return string.IsNullOrEmpty(message)
                        ? null
                        : new ValidationMessage(ValidationSeverity.Error, message);
                });
        }

        var content = BuildPromptLayout(editor);
        var session = new PromptSession<string>(
            tryGetValue: () => (true, UseCompactValue ? input.CompactValue : (input.Value ?? string.Empty)),
            validator: Validator,
            keepOnSuccess: KeepOnSuccess);

        var host = new PromptHost(content, session.TryConfirm, session.Cancel);
        session.SetRoot(host, input);
        return session;
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

