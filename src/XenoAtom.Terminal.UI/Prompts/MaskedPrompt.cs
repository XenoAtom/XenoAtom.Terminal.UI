// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI;
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
    [Fluent]
    public string Template { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the default value (slot characters) used by the underlying <see cref="MaskedInput"/>.
    /// </summary>
    [Fluent("Default")]
    public string? DefaultValue { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the prompt returns <see cref="MaskedInput.CompactValue"/> instead of <see cref="MaskedInput.Value"/>.
    /// </summary>
    [Fluent]
    public bool UseCompactValue { get; set; } = true;

    internal override PromptSession<string> CreateSession()
    {
        var validator = Validator.Invoke;
        var input = new MaskedInput(Template);
        if (!string.IsNullOrEmpty(DefaultValue))
        {
            input.Value = DefaultValue;
        }

        Visual editor = input;
        if (validator is { } validate)
        {
            editor = new ValidationPresenter()
                .Content(input)
                .Message(() =>
                {
                    var value = UseCompactValue ? input.CompactValue : (input.Value ?? string.Empty);
                    var message = validate(value);
                    return string.IsNullOrEmpty(message)
                        ? null
                        : new ValidationMessage(ValidationSeverity.Error, message);
                });
        }

        var content = BuildPromptLayout(editor);
        var session = new PromptSession<string>(
            tryGetValue: () => (true, UseCompactValue ? input.CompactValue : (input.Value ?? string.Empty)),
            validator: validator,
            keepOnSuccess: KeepOnSuccess);

        var host = new PromptHost(content, session.TryConfirm, session.Cancel);
        session.SetRoot(host, input);
        return session;
    }
}
