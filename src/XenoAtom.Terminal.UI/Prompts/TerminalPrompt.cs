// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;

namespace XenoAtom.Terminal.UI.Prompts;

/// <summary>
/// Defines an inline prompt that can be run using <see cref="TerminalPrompts"/>.
/// </summary>
public abstract class TerminalPrompt
{
    private protected TerminalPrompt()
    {
    }

    /// <summary>
    /// Gets or sets the prompt message displayed above the input.
    /// </summary>
    [Fluent]
    public Visual Message { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets optional help content displayed under the input.
    /// </summary>
    [Fluent]
    public Visual? Help { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the final visual should remain on screen when the prompt completes successfully.
    /// </summary>
    /// <remarks>
    /// When <see langword="true"/>, the prompt uses <see cref="TerminalLoopResult.StopAndKeepVisual"/>.
    /// When <see langword="false"/>, the prompt uses <see cref="TerminalLoopResult.Stop"/>.
    /// </remarks>
    [Fluent]
    public bool KeepOnSuccess { get; set; } = true;

    /// <summary>
    /// Builds the default prompt layout using the current <see cref="Message"/> and <see cref="Help"/>.
    /// </summary>
    /// <param name="editor">The editor visual.</param>
    /// <returns>The composed prompt visual.</returns>
    protected Visual BuildPromptLayout(Visual editor)
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

/// <summary>
/// Defines an inline prompt that can be run using <see cref="TerminalPrompts"/>.
/// </summary>
/// <typeparam name="T">The result type produced by the prompt.</typeparam>
/// <remarks>
/// Prompts are intended for inline/live hosting scenarios (see <see cref="TerminalExtensions.Live(Visual, System.Func{TerminalLoopResult})"/>).
/// They are composed from existing controls (for example <see cref="TextBox"/>, <see cref="NumberBox{T}"/>, <see cref="Select{T}"/>),
/// and use the binding system for validation message presentation.
/// </remarks>
public abstract class TerminalPrompt<T> : TerminalPrompt
{
    /// <summary>
    /// Gets or sets an optional validator.
    /// </summary>
    /// <remarks>
    /// The validator returns <see langword="null"/> when the value is valid; otherwise it returns an error message.
    /// </remarks>
    [Fluent("Validate")]
    public Func<T, string?>? Validator { get; set; }

    internal abstract PromptSession<T> CreateSession();
}
