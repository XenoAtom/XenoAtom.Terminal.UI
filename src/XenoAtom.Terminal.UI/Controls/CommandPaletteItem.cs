// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI.Controls;

public sealed record CommandPaletteItem(Func<Visual> ContentFactory, Action? Action = null)
{
    public CommandPaletteItem(string text, Action? action = null)
        : this(() => new TextBlock(text), action)
    {
        SearchText = text;
    }

    public string? SearchText { get; init; }

    public bool IsEnabled { get; init; } = true;

    public Func<Visual>? ShortcutFactory { get; init; }

    public Func<Visual>? DescriptionFactory { get; init; }

    public Visual CreateContent() => ContentFactory();

    public Visual? CreateShortcut() => ShortcutFactory?.Invoke();

    public Visual? CreateDescription() => DescriptionFactory?.Invoke();
}

