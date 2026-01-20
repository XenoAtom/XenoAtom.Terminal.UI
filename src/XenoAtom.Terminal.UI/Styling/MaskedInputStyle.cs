// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI.Styling;

/// <summary>
/// Defines styling for <see cref="Controls.MaskedInput"/>.
/// </summary>
/// <remarks>
/// This style inherits from <see cref="TextBoxStyle"/> to reuse common input visuals such as padding and background
/// fill resolution.
/// </remarks>
public sealed record MaskedInputStyle : TextBoxStyle, IStyle<MaskedInputStyle>
{
    /// <summary>
    /// Gets the default masked input style.
    /// </summary>
    public new static MaskedInputStyle Default { get; } = new();

    /// <summary>
    /// Gets the environment key for <see cref="MaskedInputStyle"/>.
    /// </summary>
    public new static StyleKey<MaskedInputStyle> Key { get; } = new("MaskedInputStyle", Default);

    /// <summary>
    /// Gets the default placeholder character used when the template does not specify one.
    /// </summary>
    public char DefaultPlaceholderChar { get; init; } = '_';

    /// <summary>
    /// Gets an optional foreground color override for literal separators (e.g. <c>-</c>, <c>/</c>).
    /// </summary>
    public Color? SeparatorForeground { get; init; }

    /// <summary>
    /// Resolves the style used for separator characters.
    /// </summary>
    /// <param name="theme">The current theme.</param>
    /// <param name="focused">Whether the control is focused.</param>
    public Style SeparatorCellStyle(Theme theme, bool focused)
    {
        var style = BackgroundStyle(theme, focused);
        var fg = SeparatorForeground ?? theme.Muted ?? theme.Foreground;
        if (fg is { } c)
        {
            style = style.WithForeground(c);
        }
        return style;
    }

    /// <summary>
    /// Resolves the style used for placeholder characters.
    /// </summary>
    /// <param name="theme">The current theme.</param>
    /// <param name="focused">Whether the control is focused.</param>
    public Style PlaceholderCellStyle(Theme theme, bool focused)
    {
        var style = BackgroundStyle(theme, focused);
        var fg = Placeholder ?? theme.Muted ?? theme.Foreground;
        if (fg is { } c)
        {
            style = style.WithForeground(c);
        }
        return style | TextStyle.Dim;
    }

    /// <summary>
    /// Resolves the style used for filled (user-entered) characters.
    /// </summary>
    /// <param name="theme">The current theme.</param>
    /// <param name="focused">Whether the control is focused.</param>
    public Style ValueStyle(Theme theme, bool focused)
        => BackgroundStyle(theme, focused);
}

