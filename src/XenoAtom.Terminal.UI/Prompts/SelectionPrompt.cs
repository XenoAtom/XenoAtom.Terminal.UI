// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Collections.Generic;
using XenoAtom.Terminal.UI.Controls;

namespace XenoAtom.Terminal.UI.Prompts;

/// <summary>
/// Represents an inline prompt that captures a single selection from a list of items.
/// </summary>
/// <typeparam name="T">The item type.</typeparam>
public sealed class SelectionPrompt<T> : TerminalPrompt<T>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SelectionPrompt{T}"/> class.
    /// </summary>
    public SelectionPrompt()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SelectionPrompt{T}"/> class with a message.
    /// </summary>
    /// <param name="message">The prompt message.</param>
    public SelectionPrompt(Visual message)
    {
        Message = message;
    }

    /// <summary>
    /// Gets the selectable items.
    /// </summary>
    public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();

    /// <summary>
    /// Gets or sets the initial selected index.
    /// </summary>
    public int InitialIndex { get; init; }

    /// <summary>
    /// Gets or sets the item template used by the underlying select control.
    /// </summary>
    public Templating.DataTemplate<T> ItemTemplate { get; init; }

    internal override PromptSession<T> CreateSession()
    {
        if (Items.Count == 0)
        {
            throw new InvalidOperationException("SelectionPrompt requires at least one item.");
        }

        var select = new Select<T>()
            .Items(Items)
            .ItemTemplate(ItemTemplate);

        select.SelectedIndex = Math.Clamp(InitialIndex, 0, Items.Count - 1);

        var content = BuildPromptLayout(select);
        var session = new PromptSession<T>(
            tryGetValue: () =>
            {
                var index = Math.Clamp(select.SelectedIndex, 0, Math.Max(0, select.Items.Count - 1));
                return select.Items.Count == 0 ? (false, default!) : (true, select.Items[index]);
            },
            validator: Validator,
            keepOnSuccess: KeepOnSuccess);

        var host = new PromptHost(content, session.TryConfirm, session.Cancel);
        session.SetRoot(host, select);
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

