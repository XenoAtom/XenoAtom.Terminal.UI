// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI.Styling;

/// <summary>
/// Defines styling for <see cref="Controls.StatusBar"/>.
/// </summary>
public sealed record StatusBarStyle : IStyle<StatusBarStyle>
{
    /// <summary>
    /// Gets the default status bar style.
    /// </summary>
    public static StatusBarStyle Default { get; } = new();

    /// <summary>
    /// Gets the style key for status bars.
    /// </summary>
    public static StyleKey<StatusBarStyle> Key { get; } = new("StatusBarStyle", Default);

    /// <summary>
    /// Gets the optional background color.
    /// </summary>
    public Color? Background { get; init; }

    /// <summary>
    /// Gets the optional foreground color.
    /// </summary>
    public Color? Foreground { get; init; }

    /// <summary>
    /// Resolves the status bar style for the given theme.
    /// </summary>
    /// <param name="theme">The current theme.</param>
    /// <returns>The resolved cell style.</returns>
    public Style Resolve(Theme theme)
    {
        var style = Style.None;
        var fg = Foreground ?? theme.Foreground;
        var bg = Background;

        if (fg is { } f) style = style.WithForeground(f);
        if (bg is { } b) style = style.WithBackground(b);
        style |= TextStyle.Bold;
        return style;
    }
}
