// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Input;

namespace XenoAtom.Terminal.UI.Commands;

/// <summary>
/// Represents a user-facing command that can be invoked by a keyboard shortcut and/or exposed in UI surfaces
/// such as a command bar, menu, or command palette.
/// </summary>
public sealed class Command
{
    /// <summary>
    /// Gets the unique identifier of the command.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Gets the label of the command as markup text.
    /// </summary>
    /// <remarks>
    /// Markup tokens should align with the current <see cref="Styling.Theme"/> (e.g. <c>[primary]</c>, <c>[dim]</c>).
    /// </remarks>
    public required string LabelMarkup { get; init; }

    /// <summary>
    /// Gets an optional stable textual name for this command (without markup).
    /// </summary>
    /// <remarks>
    /// This value can be used by command surfaces that support “typed commands” (for example, a command palette or a future
    /// command prompt). When set, it should be stable, lowercased, and identifier-like (e.g. <c>open</c>, <c>theme.set</c>).
    /// </remarks>
    public string? Name { get; init; }

    /// <summary>
    /// Gets the optional description/help text of the command as markup.
    /// </summary>
    public string? DescriptionMarkup { get; init; }

    /// <summary>
    /// Gets optional additional search terms for command discovery surfaces.
    /// </summary>
    /// <remarks>
    /// This can include synonyms, tags, or aliases (for example, <c>"find search replace"</c>).
    /// </remarks>
    public string? SearchText { get; init; }

    /// <summary>
    /// Gets the single-stroke gesture for this command.
    /// </summary>
    /// <remarks>
    /// A command can define either <see cref="Gesture"/> or <see cref="Sequence"/> (but not both).
    /// </remarks>
    public KeyGesture? Gesture { get; init; }

    /// <summary>
    /// Gets the multi-stroke shortcut sequence for this command.
    /// </summary>
    /// <remarks>
    /// A command can define either <see cref="Gesture"/> or <see cref="Sequence"/> (but not both).
    /// </remarks>
    public KeySequence? Sequence { get; init; }

    /// <summary>
    /// Gets the importance of the command for display ordering.
    /// </summary>
    public CommandImportance Importance { get; init; } = CommandImportance.Secondary;

    /// <summary>
    /// Gets the presentation surfaces where the command should be surfaced.
    /// </summary>
    public CommandPresentation Presentation { get; init; } = CommandPresentation.CommandBar;

    /// <summary>
    /// Gets the action executed by this command.
    /// </summary>
    public required Action<Visual> Execute { get; init; }

    /// <summary>
    /// Gets an optional predicate that determines whether the command can execute in the given context.
    /// </summary>
    public Func<Visual, bool>? CanExecute { get; init; }

    /// <summary>
    /// Gets an optional predicate that determines whether the command is visible in the given context.
    /// </summary>
    public Func<Visual, bool>? IsVisible { get; init; }

    /// <summary>
    /// Gets a value indicating whether a matching gesture is treated as handled when the command is hidden or cannot execute.
    /// </summary>
    /// <remarks>
    /// When set to <see langword="false"/>, gesture routing continues to the next matching command instead of consuming the key.
    /// </remarks>
    public bool ConsumesGestureWhenUnavailable { get; init; } = true;

    /// <summary>
    /// Returns <see langword="true"/> if the command is visible for the specified <paramref name="target"/>.
    /// </summary>
    public bool IsVisibleFor(Visual target) => IsVisible is null || IsVisible(target);

    /// <summary>
    /// Returns <see langword="true"/> if the command can execute for the specified <paramref name="target"/>.
    /// </summary>
    public bool CanExecuteFor(Visual target) => CanExecute is null || CanExecute(target);

    /// <summary>
    /// Validates the command definition.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when the command is invalid.</exception>
    internal void Validate()
    {
        if (string.IsNullOrWhiteSpace(Id))
        {
            throw new ArgumentException("A command Id must be provided.", nameof(Id));
        }

        if (Gesture is not null && Sequence is not null)
        {
            throw new ArgumentException("A command cannot specify both a gesture and a sequence.");
        }

        if (Sequence is { Count: 0 })
        {
            throw new ArgumentException("A command sequence cannot be empty.");
        }
    }
}
