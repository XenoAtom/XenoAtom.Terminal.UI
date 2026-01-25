// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI.Input;

/// <summary>
/// Represents a user-facing command that can be invoked by a keyboard shortcut and/or exposed in UI surfaces
/// such as a command bar, menu, or command palette.
/// </summary>
public sealed class UiCommand
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
    /// Gets the optional description/help text of the command as markup.
    /// </summary>
    public string? DescriptionMarkup { get; init; }

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
    public UiCommandImportance Importance { get; init; } = UiCommandImportance.Secondary;

    /// <summary>
    /// Gets the presentation surfaces where the command should be surfaced.
    /// </summary>
    public UiCommandPresentation Presentation { get; init; } = UiCommandPresentation.CommandBar;

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

/// <summary>
/// Defines importance buckets for ordering commands in UI surfaces.
/// </summary>
public enum UiCommandImportance
{
    /// <summary>
    /// A primary command users are expected to discover quickly.
    /// </summary>
    Primary,

    /// <summary>
    /// A common command that should be discoverable without dominating the UI.
    /// </summary>
    Secondary,

    /// <summary>
    /// A command that is rarely needed or mostly contextual.
    /// </summary>
    Tertiary,
}

/// <summary>
/// Flags describing where a command should be presented.
/// </summary>
[Flags]
public enum UiCommandPresentation
{
    /// <summary>
    /// The command should not be automatically presented.
    /// </summary>
    None = 0,

    /// <summary>
    /// Present the command in a command bar UI surface.
    /// </summary>
    CommandBar = 1,

    /// <summary>
    /// Present the command in a command palette.
    /// </summary>
    CommandPalette = 2,

    /// <summary>
    /// Present the command in menus.
    /// </summary>
    Menu = 4,

    /// <summary>
    /// Present the command in context menus.
    /// </summary>
    ContextMenu = 8,
}
