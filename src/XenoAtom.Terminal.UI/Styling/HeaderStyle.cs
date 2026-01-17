// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI.Styling;

/// <summary>
/// Defines styling for <see cref="Controls.Header"/>.
/// </summary>
public sealed record HeaderStyle : IStyle<HeaderStyle>
{
    /// <summary>
    /// Gets the default header style.
    /// </summary>
    public static HeaderStyle Default { get; } = new();

    /// <summary>
    /// Gets the style key for headers.
    /// </summary>
    public static StyleKey<HeaderStyle> Key { get; } = new("HeaderStyle", Default);

    /// <summary>
    /// Gets the optional background color.
    /// </summary>
    public XenoAtom.Ansi.AnsiColor? Background { get; init; }

    /// <summary>
    /// Gets the optional foreground color.
    /// </summary>
    public XenoAtom.Ansi.AnsiColor? Foreground { get; init; }

    /// <summary>
    /// Resolves the header style for the given theme.
    /// </summary>
    /// <param name="theme">The current theme.</param>
    /// <returns>The resolved cell style.</returns>
    public CellStyle Resolve(Theme theme)
    {
        var style = CellStyle.None;
        var fg = Foreground ?? theme.Foreground;
        var bg = Background ?? theme.SurfaceAlt;

        if (fg is { } f) style = style.WithForeground(f);
        if (bg is { } b) style = style.WithBackground(b);
        style |= TextStyle.Bold;
        return style;
    }
}
