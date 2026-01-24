// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.Prompts;

/// <summary>
/// Provides built-in templates for composing <see cref="TerminalPrompt"/> visuals.
/// </summary>
public static class TerminalPromptTemplates
{
    /// <summary>
    /// A compact template: message on the left, editor on the right, help below if present.
    /// </summary>
    public static TerminalPrompt.TerminalPromptTemplate Compact { get; } = CompactTemplate;

    /// <summary>
    /// A compact template wrapped in a rounded group.
    /// </summary>
    public static TerminalPrompt.TerminalPromptTemplate CompactRoundedGroup { get; } = (prompt, editor)
        => InGroup(prompt, editor, GroupStyle.Rounded, compact: true);

    /// <summary>
    /// A compact template wrapped in a square (single-line) group.
    /// </summary>
    public static TerminalPrompt.TerminalPromptTemplate CompactSquareGroup { get; } = (prompt, editor)
        => InGroup(prompt, editor, GroupStyle.Single, compact: true);

    /// <summary>
    /// A more verbose template wrapped in a rounded group (similar to early prompt layouts).
    /// </summary>
    public static TerminalPrompt.TerminalPromptTemplate VerboseRoundedGroup { get; } = (prompt, editor)
        => InGroup(prompt, editor, GroupStyle.Rounded, compact: false);

    /// <summary>
    /// A more verbose template wrapped in a square (single-line) group (similar to early prompt layouts).
    /// </summary>
    public static TerminalPrompt.TerminalPromptTemplate VerboseSquareGroup { get; } = (prompt, editor)
        => InGroup(prompt, editor, GroupStyle.Single, compact: false);

    private static Visual CompactTemplate(TerminalPrompt prompt, Visual editor)
    {
        // Make the common case easy to read in inline prompts:
        // `Message` and the input on the same line, with optional help below.
        var message = prompt.Message;
        var help = prompt.Help;

        editor.HorizontalAlignment(Align.Stretch);

        var row = new HStack(message, editor)
            .Spacing(1)
            .HorizontalAlignment(Align.Stretch);

        return help is null
            ? row
            : new VStack(row, help).Spacing(0);
    }

    private static Visual InGroup(TerminalPrompt prompt, Visual editor, GroupStyle groupStyle, bool compact)
    {
        var message = prompt.Message;
        var help = prompt.Help;

        editor.HorizontalAlignment(Align.Stretch);

        Visual content;
        if (compact)
        {
            var row = new HStack(message, editor)
                .Spacing(1)
                .HorizontalAlignment(Align.Stretch);

            content = help is null ? row : new VStack(row, help).Spacing(0);
        }
        else
        {
            // Keep the older layout as an opt-in "verbose" style.
            content = help is null
                ? new VStack(message, editor).Spacing(1)
                : new VStack(message, editor, help).Spacing(1);
        }

        return new Group()
            .Padding(1)
            .Style(groupStyle)
            .Content(content);
    }
}

