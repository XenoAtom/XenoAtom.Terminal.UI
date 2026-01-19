// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Text;
using XenoAtom.Ansi;

namespace XenoAtom.Terminal.UI.Styling;

/// <summary>
/// Defines default drawing options for a <see cref="Controls.Canvas"/>.
/// </summary>
public sealed record CanvasStyle : IStyle<CanvasStyle>
{
    /// <summary>
    /// Gets the default style.
    /// </summary>
    public static CanvasStyle Default { get; } = new();

    /// <summary>
    /// Gets the environment key used to resolve a <see cref="CanvasStyle"/>.
    /// </summary>
    public static StyleKey<CanvasStyle> Key { get; } = new("CanvasStyle", Default);

    /// <summary>
    /// Gets the default rune used by drawing operations when no rune is explicitly provided.
    /// </summary>
    public Rune DefaultRune { get; init; } = new('█');

    /// <summary>
    /// Gets the default cell style used by drawing operations when no style is explicitly provided.
    /// </summary>
    /// <remarks>
    /// When <c>null</c>, the style is resolved from the current theme.
    /// </remarks>
    public Style? DefaultStyle { get; init; }

    /// <summary>
    /// Resolves the default drawing style for the specified theme.
    /// </summary>
    /// <param name="theme">The current theme.</param>
    /// <returns>The resolved style.</returns>
    public Style ResolveDefaultStyle(Theme theme)
    {
        ArgumentNullException.ThrowIfNull(theme);
        if (DefaultStyle is { } style)
        {
            return style;
        }

        // Match typical "ink" behavior: draw in the theme foreground using the terminal's default background.
        return theme.ForegroundTextStyle();
    }
}
