// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI.Styling;

/// <summary>
/// Defines rendering and theming options for a <see cref="Controls.TextFiglet"/>.
/// </summary>
public sealed record TextFigletStyle : IStyle<TextFigletStyle>
{
    /// <summary>
    /// Gets the default style.
    /// </summary>
    public static TextFigletStyle Default { get; } = new();

    /// <summary>
    /// Gets the environment key used to resolve a <see cref="TextFigletStyle"/>.
    /// </summary>
    public static StyleKey<TextFigletStyle> Key { get; } = new("TextFigletStyle", Default);

    /// <summary>
    /// Gets the cell style used to render the FIGlet text.
    /// </summary>
    /// <remarks>
    /// When <c>null</c>, the style is resolved from the current theme foreground.
    /// </remarks>
    public CellStyle? TextStyle { get; init; }

    /// <summary>
    /// Resolves the FIGlet text style for the specified theme.
    /// </summary>
    public CellStyle ResolveTextStyle(Theme theme)
    {
        ArgumentNullException.ThrowIfNull(theme);
        return TextStyle ?? theme.ForegroundTextStyle();
    }
}

