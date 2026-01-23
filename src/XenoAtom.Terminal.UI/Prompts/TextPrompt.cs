// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

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
    public string? Placeholder { get; init; }

    /// <summary>
    /// Gets or sets the initial value displayed in the editor.
    /// </summary>
    public string? InitialValue { get; init; }

    internal override PromptSession<string> CreateSession()
    {
        var textBox = new TextBox(InitialValue);
        if (!string.IsNullOrEmpty(Placeholder))
        {
            textBox.Placeholder(Placeholder);
        }

        Visual editor = textBox;
        if (Validator is { } validator)
        {
            editor = new ValidationPresenter()
                .Content(textBox)
                .Message(() =>
                {
                    var value = textBox.Text ?? string.Empty;
                    var message = validator(value);
                    return string.IsNullOrEmpty(message)
                        ? null
                        : new ValidationMessage(ValidationSeverity.Error, message);
                });
        }

        var content = BuildPromptLayout(editor);
        var session = new PromptSession<string>(
            tryGetValue: () => (true, textBox.Text ?? string.Empty),
            validator: Validator,
            keepOnSuccess: KeepOnSuccess);

        var host = new PromptHost(content, session.TryConfirm, session.Cancel);
        session.SetRoot(host, textBox);
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

