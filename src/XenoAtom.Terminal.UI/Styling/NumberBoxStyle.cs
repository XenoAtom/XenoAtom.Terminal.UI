// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Ansi;
using XenoAtom.Terminal.UI.Geometry;

namespace XenoAtom.Terminal.UI.Styling;

/// <summary>
/// Defines styling options for <c>NumberBox&lt;T&gt;</c>.
/// </summary>
public sealed record NumberBoxStyle : IStyle<NumberBoxStyle>
{
    /// <summary>
    /// Gets the default number box style.
    /// </summary>
    public static NumberBoxStyle Default { get; } = new();

    /// <summary>
    /// Gets the environment key for <see cref="NumberBoxStyle"/>.
    /// </summary>
    public static StyleKey<NumberBoxStyle> Key { get; } = new("NumberBoxStyle", Default);

    /// <summary>
    /// Gets the padding applied to the validation message line.
    /// </summary>
    public Thickness ValidationPadding { get; init; } = new(1, 0, 1, 0);

    /// <summary>
    /// Gets the optional prefix displayed before the validation message.
    /// </summary>
    public string ValidationPrefix { get; init; } = "⚠ ";

    /// <summary>
    /// Gets an optional foreground color override for the validation message.
    /// </summary>
    public AnsiColor? ValidationForeground { get; init; }

    /// <summary>
    /// Gets an optional background color override for the validation message.
    /// </summary>
    public AnsiColor? ValidationBackground { get; init; }

    /// <summary>
    /// Resolves the style used to render the validation message.
    /// </summary>
    /// <param name="theme">The resolved theme.</param>
    /// <returns>The style used to render the validation message line.</returns>
    public CellStyle ValidationStyle(Theme theme)
    {
        var style = theme.BaseTextStyle();

        var fg = ValidationForeground ?? theme.Error ?? theme.Foreground;
        if (fg is { } f)
        {
            style = style.WithForeground(f);
        }

        var bg = ValidationBackground ?? theme.Background;
        if (bg is { } b)
        {
            style = style.WithBackground(b);
        }

        style |= TextStyle.Bold;
        return style;
    }
}

