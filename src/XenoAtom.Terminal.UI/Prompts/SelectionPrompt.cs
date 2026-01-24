// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Collections.Generic;
using XenoAtom.Terminal.UI;
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
    [Fluent]
    public IReadOnlyList<T> Items { get; set; } = Array.Empty<T>();

    /// <summary>
    /// Gets or sets the initial selected index.
    /// </summary>
    [Fluent]
    public int InitialIndex { get; set; }

    /// <summary>
    /// Gets or sets the item template used by the underlying select control.
    /// </summary>
    [Fluent]
    public Templating.DataTemplate<T> ItemTemplate { get; set; }

    internal override PromptSession<T> CreateSession()
    {
        if (Items.Count == 0)
        {
            throw new InvalidOperationException("SelectionPrompt requires at least one item.");
        }

        var validator = Validator.Invoke;
        var select = new Select<T>()
            .Items(Items)
            .ItemTemplate(ItemTemplate);

        select.SelectedIndex = Math.Clamp(InitialIndex, 0, Items.Count - 1);

        var content = BuildPromptVisual(select);
        var session = new PromptSession<T>(
            tryGetValue: () =>
            {
                var index = Math.Clamp(select.SelectedIndex, 0, Math.Max(0, select.Items.Count - 1));
                return select.Items.Count == 0 ? (false, default!) : (true, select.Items[index]);
            },
            validator: validator,
            keepOnSuccess: KeepOnSuccess);

        var host = new PromptHost(content, session.TryConfirm, session.Cancel);
        session.SetRoot(host, select);
        return session;
    }
}
