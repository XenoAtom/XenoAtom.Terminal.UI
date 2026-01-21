// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Geometry;

namespace XenoAtom.Terminal.UI.Styling;

/// <summary>
/// Defines rendering and theming options for a <see cref="Controls.LogControl"/>.
/// </summary>
public sealed record LogControlStyle : IStyle<LogControlStyle>
{
    /// <summary>
    /// Gets the default log control style.
    /// </summary>
    public static LogControlStyle Default { get; } = new();

    /// <summary>
    /// Gets the environment key used to resolve a <see cref="LogControlStyle"/>.
    /// </summary>
    public static StyleKey<LogControlStyle> Key { get; } = new("LogControlStyle", Default);

    /// <summary>
    /// Gets the padding applied around the scrollable log viewport.
    /// </summary>
    public Thickness Padding { get; init; } = new(1, 0, 1, 0);

    /// <summary>
    /// Gets the optional border color.
    /// </summary>
    public Color? Border { get; init; }

    /// <summary>
    /// Gets the optional border color when focused.
    /// </summary>
    public Color? FocusBorder { get; init; }

    /// <summary>
    /// Gets the optional selection background color.
    /// </summary>
    public Color? Selection { get; init; }

    /// <summary>
    /// Gets the optional background color for the log surface.
    /// </summary>
    public Color? Background { get; init; }

    /// <summary>
    /// Resolves the border style for the provided <paramref name="theme"/>.
    /// </summary>
    /// <param name="theme">The current theme.</param>
    /// <param name="focused">Whether the control is focused.</param>
    public Style BorderStyle(Theme theme, bool focused)
    {
        var color = focused ? (FocusBorder ?? theme.FocusBorder) : (Border ?? theme.Border);
        var style = Style.None;
        if (color is { } c)
        {
            style = style.WithForeground(c);
        }
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
    /// Resolves the background style for the provided <paramref name="theme"/>.
    /// </summary>
    /// <param name="theme">The current theme.</param>
    /// <param name="focused">Whether the log viewer is focused.</param>
    public Style BackgroundStyle(Theme theme, bool focused)
    {
        var style = Style.None;
        if (theme.Foreground is { } fg) style = style.WithForeground(fg);
        var themeFill = focused ? (theme.InputFillFocused ?? theme.InputFill) : theme.InputFill;
        var bg = Background ?? themeFill ?? theme.SurfaceAlt ?? theme.Surface ?? theme.Background;
        if (bg is { } b) style = style.WithBackground(b);
        return style;
    }
}

