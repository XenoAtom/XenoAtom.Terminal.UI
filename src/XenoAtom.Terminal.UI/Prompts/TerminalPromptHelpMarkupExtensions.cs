// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Ansi;
using XenoAtom.Terminal.UI.Controls;

namespace XenoAtom.Terminal.UI.Prompts;

/// <summary>
/// Provides helper methods for configuring prompt help content using ANSI markup.
/// </summary>
public static class TerminalPromptHelpMarkupExtensions
{
    /// <summary>
    /// Sets <see cref="TerminalPrompt.Help"/> to a <see cref="Markup"/> control using the provided markup text.
    /// </summary>
    /// <typeparam name="TPrompt">The prompt type.</typeparam>
    /// <param name="prompt">The prompt to configure.</param>
    /// <param name="markup">The markup text to render.</param>
    /// <returns>The same prompt instance for chaining.</returns>
    public static TPrompt HelpMarkup<TPrompt>(this TPrompt prompt, string markup)
        where TPrompt : TerminalPrompt
    {
        ArgumentNullException.ThrowIfNull(prompt);
        prompt.Help = new Markup(markup);
        return prompt;
    }

    /// <summary>
    /// Sets <see cref="TerminalPrompt.Help"/> to a <see cref="Markup"/> control using an interpolated markup handler.
    /// </summary>
    /// <typeparam name="TPrompt">The prompt type.</typeparam>
    /// <param name="prompt">The prompt to configure.</param>
    /// <param name="handler">The interpolated markup handler.</param>
    /// <returns>The same prompt instance for chaining.</returns>
    public static TPrompt HelpMarkup<TPrompt>(this TPrompt prompt, ref AnsiMarkupInterpolatedStringHandler handler)
        where TPrompt : TerminalPrompt
    {
        ArgumentNullException.ThrowIfNull(prompt);
        prompt.Help = new Markup(ref handler);
        return prompt;
    }
}
