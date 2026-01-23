// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Collections.Generic;
using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;

namespace XenoAtom.Terminal.UI.Prompts;

/// <summary>
/// Represents an inline prompt that captures multiple selections from a list of items.
/// </summary>
/// <typeparam name="T">The item type.</typeparam>
public sealed class MultiSelectionPrompt<T> : TerminalPrompt<IReadOnlyList<T>>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MultiSelectionPrompt{T}"/> class.
    /// </summary>
    public MultiSelectionPrompt()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MultiSelectionPrompt{T}"/> class with a message.
    /// </summary>
    /// <param name="message">The prompt message.</param>
    public MultiSelectionPrompt(Visual message)
    {
        Message = message;
    }

    /// <summary>
    /// Gets the selectable items.
    /// </summary>
    [Fluent]
    public IReadOnlyList<T> Items { get; set; } = Array.Empty<T>();

    /// <summary>
    /// Gets the indexes of items that should be initially checked.
    /// </summary>
    [Fluent]
    public IReadOnlyList<int> InitialCheckedIndices { get; set; } = Array.Empty<int>();

    /// <summary>
    /// Gets or sets the item template used by the underlying selection list control.
    /// </summary>
    [Fluent]
    public Templating.DataTemplate<T> ItemTemplate { get; set; }

    internal override PromptSession<IReadOnlyList<T>> CreateSession()
    {
        var list = new SelectionList<T>()
            .Items(Items)
            .ItemTemplate(ItemTemplate);

        list.Checked.Clear();
        for (var i = 0; i < list.Items.Count; i++)
        {
            list.Checked.Add(false);
        }

        for (var i = 0; i < InitialCheckedIndices.Count; i++)
        {
            var index = InitialCheckedIndices[i];
            if ((uint)index < (uint)list.Checked.Count)
            {
                list.Checked[index] = true;
            }
        }

        Visual editor = list;
        if (Validator is { } validator)
        {
            editor = new ValidationPresenter()
                .Content(list)
                .Message(() =>
                {
                    var selected = BuildSelectionSnapshot(list);
                    var message = validator(selected);
                    return string.IsNullOrEmpty(message)
                        ? null
                        : new ValidationMessage(ValidationSeverity.Error, message);
                });
        }

        var content = BuildPromptLayout(editor);
        var session = new PromptSession<IReadOnlyList<T>>(
            tryGetValue: () => (true, BuildSelectionSnapshot(list)),
            validator: Validator,
            keepOnSuccess: KeepOnSuccess);

        var host = new PromptHost(content, session.TryConfirm, session.Cancel);
        session.SetRoot(host, list);
        return session;
    }

    private static IReadOnlyList<T> BuildSelectionSnapshot(SelectionList<T> list)
    {
        var result = new List<T>();
        var count = Math.Min(list.Items.Count, list.Checked.Count);
        for (var i = 0; i < count; i++)
        {
            if (list.Checked[i])
            {
                result.Add(list.Items[i]);
            }
        }

        return result;
    }

}
