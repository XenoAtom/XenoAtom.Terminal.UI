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
    /// <summary>
    /// Defines a function that composes a prompt visual from the prompt instance and the editor control.
    /// </summary>
    /// <remarks>
    /// The template is responsible for arranging <see cref="Message"/>, the editor, and optionally <see cref="Help"/>.
    /// </remarks>
    /// <param name="prompt">The prompt instance.</param>
    /// <param name="editor">The editor control used to capture input.</param>
    /// <returns>The composed prompt visual.</returns>
    public delegate Visual TerminalPromptTemplate(TerminalPrompt prompt, Visual editor);

    /// <summary>
    /// Initializes a new instance of the <see cref="TerminalPrompt"/> class.
    /// </summary>
    /// <remarks>
    /// The default prompt template is <see cref="TerminalPromptTemplates.Compact"/>.
    /// </remarks>
    private protected TerminalPrompt()
    {
        PromptTemplate = TerminalPromptTemplates.Compact;
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
    /// Gets or sets the template used to compose the prompt visual tree.
    /// </summary>
    /// <remarks>
    /// Prompts are inline/live UI, so templates should remain compact and avoid heavy nesting.
    /// </remarks>
    [Fluent]
    public Delegator<TerminalPromptTemplate> PromptTemplate { get; set; }

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
    /// Builds the prompt visual using <see cref="PromptTemplate"/>.
    /// </summary>
    /// <param name="editor">The editor visual.</param>
    /// <returns>The composed prompt visual.</returns>
    protected Visual BuildPromptVisual(Visual editor)
    {
        ArgumentNullException.ThrowIfNull(editor);

        var template = PromptTemplate.Invoke ?? TerminalPromptTemplates.Compact;
        return template(this, editor);
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
    public Delegator<Func<T, string?>> Validator { get; set; }

    internal abstract PromptSession<T> CreateSession();
}
