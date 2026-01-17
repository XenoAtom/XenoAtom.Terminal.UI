// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI.Controls;

/// <summary>
/// Represents an item displayed by a <see cref="CommandPalette"/>.
/// </summary>
/// <param name="ContentFactory">A factory that creates the item content visual.</param>
/// <param name="Action">An optional action executed when the item is activated.</param>
public sealed record CommandPaletteItem(Func<Visual> ContentFactory, Action? Action = null)
{
    /// <summary>
    /// Initializes a new command palette item from a text label.
    /// </summary>
    /// <param name="text">The item label.</param>
    /// <param name="action">The optional action executed when activated.</param>
    public CommandPaletteItem(string text, Action? action = null)
        : this(() => new TextBlock(text), action)
    {
        SearchText = text;
    }

    /// <summary>
    /// Gets the text used for searching. When <see langword="null"/>, the item might not be searchable.
    /// </summary>
    public string? SearchText { get; init; }

    /// <summary>
    /// Gets a value indicating whether the item is enabled.
    /// </summary>
    public bool IsEnabled { get; init; } = true;

    /// <summary>
    /// Gets an optional factory creating the shortcut visual.
    /// </summary>
    public Func<Visual>? ShortcutFactory { get; init; }

    /// <summary>
    /// Gets an optional factory creating the description visual.
    /// </summary>
    public Func<Visual>? DescriptionFactory { get; init; }

    /// <summary>
    /// Creates the content visual for the item.
    /// </summary>
    public Visual CreateContent() => ContentFactory();

    /// <summary>
    /// Creates the shortcut visual for the item.
    /// </summary>
    public Visual? CreateShortcut() => ShortcutFactory?.Invoke();

    /// <summary>
    /// Creates the description visual for the item.
    /// </summary>
    public Visual? CreateDescription() => DescriptionFactory?.Invoke();
}
