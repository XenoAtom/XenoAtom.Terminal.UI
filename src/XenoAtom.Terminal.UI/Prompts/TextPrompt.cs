// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;

namespace XenoAtom.Terminal.UI.Prompts;

/// <summary>
/// Represents an inline prompt for editing a string value.
/// </summary>
public sealed class TextPrompt : TerminalPrompt<string>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TextPrompt"/> class.
    /// </summary>
    public TextPrompt()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TextPrompt"/> class with a message.
    /// </summary>
    /// <param name="message">The prompt message.</param>
    public TextPrompt(Visual message)
    {
        Message = message;
    }

    /// <summary>
    /// Gets or sets optional placeholder text displayed when the input is empty.
    /// </summary>
    [Fluent]
    public string? Placeholder { get; set; }

    /// <summary>
    /// Gets or sets the default value displayed in the editor.
    /// </summary>
    [Fluent("Default")]
    public string? DefaultValue { get; set; }

    internal override PromptSession<string> CreateSession()
    {
        var validator = Validator.Invoke;
        var textBox = new TextBox(DefaultValue);
        if (!string.IsNullOrEmpty(Placeholder))
        {
            textBox.Placeholder(Placeholder);
        }

        Visual editor = textBox;
        if (validator is { } validate)
        {
            editor = new ValidationPresenter()
                .Content(textBox)
                .Message(() =>
                {
                    var value = textBox.Text ?? string.Empty;
                    var message = validate(value);
                    return string.IsNullOrEmpty(message)
                        ? null
                        : new ValidationMessage(ValidationSeverity.Error, message);
                });
        }

        var content = BuildPromptVisual(editor);
        var session = new PromptSession<string>(
            tryGetValue: () => (true, textBox.Text ?? string.Empty),
            validator: validator,
            keepOnSuccess: KeepOnSuccess);

        var host = new PromptHost(content, session.TryConfirm, session.Cancel);
        session.SetRoot(host, textBox);
        return session;
    }
}
