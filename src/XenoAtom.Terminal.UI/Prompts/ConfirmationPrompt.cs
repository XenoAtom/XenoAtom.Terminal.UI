// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;

namespace XenoAtom.Terminal.UI.Prompts;

/// <summary>
/// Represents an inline prompt that captures a boolean confirmation.
/// </summary>
public sealed class ConfirmationPrompt : TerminalPrompt<bool>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConfirmationPrompt"/> class.
    /// </summary>
    public ConfirmationPrompt()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfirmationPrompt"/> class with a message.
    /// </summary>
    /// <param name="message">The prompt message.</param>
    public ConfirmationPrompt(Visual message)
    {
        Message = message;
    }

    /// <summary>
    /// Gets or sets the default value.
    /// </summary>
    [Fluent("Default")]
    public bool DefaultValue { get; set; }

    /// <summary>
    /// Gets or sets the label displayed next to the toggle.
    /// </summary>
    [Fluent]
    public Visual Label { get; set; } = "Confirm";

    internal override PromptSession<bool> CreateSession()
    {
        var toggle = new Switch(Label) { IsOn = DefaultValue };

        var content = BuildPromptLayout(toggle);
        var validator = Validator.Invoke;
        var session = new PromptSession<bool>(
            tryGetValue: () => (true, toggle.IsOn),
            validator: validator,
            keepOnSuccess: KeepOnSuccess);

        var host = new PromptHost(content, session.TryConfirm, session.Cancel);
        session.SetRoot(host, toggle);
        return session;
    }
}
