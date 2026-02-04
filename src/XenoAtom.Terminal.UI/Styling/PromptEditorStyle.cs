// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Geometry;

namespace XenoAtom.Terminal.UI.Styling;

/// <summary>
/// Defines rendering and theming options for a <see cref="Controls.PromptEditor"/>.
/// </summary>
public sealed record PromptEditorStyle : IStyle<PromptEditorStyle>
{
    /// <summary>
    /// Gets the default prompt editor style.
    /// </summary>
    public static PromptEditorStyle Default { get; } = new();

    /// <summary>
    /// Gets the environment key used to resolve a <see cref="PromptEditorStyle"/>.
    /// </summary>
    public static StyleKey<PromptEditorStyle> Key { get; } = new("PromptEditorStyle", Default);

    /// <summary>
    /// Gets the padding between the control bounds and its content (prompt + editor surface).
    /// </summary>
    public Thickness Padding { get; init; } = new(1, 0, 1, 0);

    /// <summary>
    /// Gets the optional prompt prefix foreground color.
    /// </summary>
    public Color? PromptForeground { get; init; }

    /// <summary>
    /// Gets the optional continuation prompt prefix foreground color.
    /// </summary>
    public Color? ContinuationPromptForeground { get; init; }

    /// <summary>
    /// Gets the optional ghost completion foreground color.
    /// </summary>
    public Color? GhostForeground { get; init; }

    /// <summary>
    /// Gets the optional placeholder foreground color.
    /// </summary>
    public Color? PlaceholderForeground { get; init; }

    /// <summary>
    /// Gets the optional selection background color.
    /// </summary>
    public Color? Selection { get; init; }

    /// <summary>
    /// Gets the optional background color for the editor surface.
    /// </summary>
    public Color? Background { get; init; }

    /// <summary>
    /// Resolves the background style for the provided <paramref name="theme"/>.
    /// </summary>
    /// <param name="theme">The current theme.</param>
    /// <param name="focused">Whether the editor is focused.</param>
    public Style BackgroundStyle(Theme theme, bool focused)
    {
        var style = Style.None;
        if (theme.Foreground is { } fg) style = style.WithForeground(fg);
        var themeFill = focused ? (theme.InputFillFocused ?? theme.InputFill) : theme.InputFill;
        var bg = Background ?? themeFill ?? theme.SurfaceAlt ?? theme.Surface ?? theme.Background;
        if (bg is { } b) style = style.WithBackground(b);
        return style;
    }

    /// <summary>
    /// Resolves the selection style for the provided <paramref name="theme"/>.
    /// </summary>
    public Style SelectionStyle(Theme theme)
    {
        var style = Style.None;
        var color = Selection ?? theme.Selection;
        if (color is { } c)
        {
            style = style.WithBackground(c);
        }
        style |= TextStyle.Bold;
        return style;
    }

    /// <summary>
    /// Resolves the placeholder style for the provided <paramref name="theme"/>.
    /// </summary>
    /// <param name="theme">The current theme.</param>
    /// <param name="focused">Whether the editor is focused.</param>
    public Style PlaceholderStyle(Theme theme, bool focused)
    {
        var style = BackgroundStyle(theme, focused);
        var fg = PlaceholderForeground ?? theme.Muted ?? theme.Foreground;
        if (fg is { } c) style = style.WithForeground(c);
        return style;
    }

    /// <summary>
    /// Resolves the prompt prefix style for the provided <paramref name="theme"/>.
    /// </summary>
    /// <param name="theme">The current theme.</param>
    /// <param name="focused">Whether the editor is focused.</param>
    public Style PromptStyle(Theme theme, bool focused)
    {
        var style = BackgroundStyle(theme, focused);
        var fg = PromptForeground ?? theme.Accent ?? theme.Primary ?? theme.Foreground;
        if (fg is { } c) style = style.WithForeground(c);
        return style;
    }

    /// <summary>
    /// Resolves the continuation prompt prefix style for the provided <paramref name="theme"/>.
    /// </summary>
    /// <param name="theme">The current theme.</param>
    /// <param name="focused">Whether the editor is focused.</param>
    public Style ContinuationPromptStyle(Theme theme, bool focused)
    {
        var style = BackgroundStyle(theme, focused);
        var fg = ContinuationPromptForeground ?? theme.Muted ?? theme.Foreground;
        if (fg is { } c) style = style.WithForeground(c);
        style |= TextStyle.Dim;
        return style;
    }

    /// <summary>
    /// Resolves the ghost completion style for the provided <paramref name="theme"/>.
    /// </summary>
    /// <param name="theme">The current theme.</param>
    /// <param name="focused">Whether the editor is focused.</param>
    public Style GhostStyle(Theme theme, bool focused)
    {
        var style = BackgroundStyle(theme, focused);
        var fg = GhostForeground ?? theme.Muted ?? theme.Foreground;
        if (fg is { } c) style = style.WithForeground(c);
        style |= TextStyle.Dim;
        return style;
    }

    /// <summary>
    /// Resolves the style used to render word hints (for example underline the word under the caret).
    /// </summary>
    /// <param name="theme">The current theme.</param>
    public Style WordHintStyle(Theme theme)
    {
        var style = Style.None | TextStyle.Underline;
        var fg = theme.Accent ?? theme.Primary ?? theme.Foreground;
        if (fg is { } c) style = style.WithForeground(c);
        return style;
    }
}

