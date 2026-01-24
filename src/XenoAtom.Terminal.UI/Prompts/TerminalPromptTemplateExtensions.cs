// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI.Prompts;

/// <summary>
/// Provides convenience methods for selecting built-in prompt templates.
/// </summary>
public static class TerminalPromptTemplateExtensions
{
    /// <summary>
    /// Uses the default compact prompt template.
    /// </summary>
    public static T StyleCompact<T>(this T prompt) where T : TerminalPrompt
        => prompt.PromptTemplate(TerminalPromptTemplates.Compact);

    /// <summary>
    /// Uses a compact template wrapped in a rounded group.
    /// </summary>
    public static T StyleInGroup<T>(this T prompt) where T : TerminalPrompt
        => prompt.PromptTemplate(TerminalPromptTemplates.CompactRoundedGroup);

    /// <summary>
    /// Uses a compact template wrapped in a square (single-line) group.
    /// </summary>
    public static T StyleInSquareGroup<T>(this T prompt) where T : TerminalPrompt
        => prompt.PromptTemplate(TerminalPromptTemplates.CompactSquareGroup);

    /// <summary>
    /// Uses the verbose (legacy-style) template wrapped in a rounded group.
    /// </summary>
    public static T StyleVerboseInGroup<T>(this T prompt) where T : TerminalPrompt
        => prompt.PromptTemplate(TerminalPromptTemplates.VerboseRoundedGroup);

    /// <summary>
    /// Uses the verbose (legacy-style) template wrapped in a square (single-line) group.
    /// </summary>
    public static T StyleVerboseInSquareGroup<T>(this T prompt) where T : TerminalPrompt
        => prompt.PromptTemplate(TerminalPromptTemplates.VerboseSquareGroup);
}
