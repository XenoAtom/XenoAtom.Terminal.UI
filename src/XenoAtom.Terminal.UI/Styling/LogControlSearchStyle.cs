// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI.Styling;

/// <summary>
/// Defines search styling for <see cref="Controls.LogControl"/> (match highlighting and search UI accents).
/// </summary>
public sealed record LogControlSearchStyle : IStyle<LogControlSearchStyle>
{
    /// <summary>
    /// Gets the default search style.
    /// </summary>
    public static LogControlSearchStyle Default { get; } = new();

    /// <summary>
    /// Gets the environment key used to resolve a <see cref="LogControlSearchStyle"/>.
    /// </summary>
    public static StyleKey<LogControlSearchStyle> Key { get; } = new("LogControlSearchStyle", Default);

    /// <summary>
    /// Gets the style applied to all matches.
    /// </summary>
    public Style? MatchStyle { get; init; }

    /// <summary>
    /// Gets the style applied to the active match.
    /// </summary>
    public Style? ActiveMatchStyle { get; init; }

    /// <summary>
    /// Gets the style used to render search errors (e.g. invalid regex).
    /// </summary>
    public Style? ErrorStyle { get; init; }

    /// <summary>
    /// Resolves the match highlight style for the provided <paramref name="theme"/>.
    /// </summary>
    public Style ResolveMatchStyle(Theme theme)
    {
        if (MatchStyle is { } s)
        {
            return s;
        }

        var bg = theme.Accent is { } accent ? accent.WithAlpha(0x35) : theme.Selection;
        var style = Style.None;
        if (bg is { } b)
        {
            style = style.WithBackground(b);
        }

        return style;
    }

    /// <summary>
    /// Resolves the active match highlight style for the provided <paramref name="theme"/>.
    /// </summary>
    public Style ResolveActiveMatchStyle(Theme theme)
    {
        if (ActiveMatchStyle is { } s)
        {
            return s;
        }

        var bg = theme.Accent is { } accent ? accent.WithAlpha(0x55) : theme.Selection;
        var style = Style.None;
        if (bg is { } b)
        {
            style = style.WithBackground(b);
        }
        style |= TextStyle.Bold;
        return style;
    }

    /// <summary>
    /// Resolves the style used to render search errors.
    /// </summary>
    public Style ResolveErrorStyle(Theme theme)
    {
        if (ErrorStyle is { } s)
        {
            return s;
        }

        var style = theme.ForegroundTextStyle();
        if (theme.Error is { } fg)
        {
            style = style.WithForeground(fg);
        }
        return style;
    }
}

