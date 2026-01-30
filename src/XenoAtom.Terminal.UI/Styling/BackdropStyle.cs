// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Text;

namespace XenoAtom.Terminal.UI.Styling;

/// <summary>
/// Defines rendering and theming options for a <see cref="Controls.Backdrop"/>.
/// </summary>
public sealed record BackdropStyle : IStyle<BackdropStyle>
{
    /// <summary>
    /// Gets the default backdrop style used for modal dimming.
    /// </summary>
    public static BackdropStyle Default { get; } = new();

    /// <summary>
    /// Gets a backdrop style that fills the surface with the current theme background.
    /// </summary>
    public static BackdropStyle ThemeBackground { get; } = Default with { UseThemeBackground = true, Dim = false };

    /// <summary>
    /// Gets the environment key used to resolve a <see cref="BackdropStyle"/>.
    /// </summary>
    public static StyleKey<BackdropStyle> Key { get; } = new("BackdropStyle", Default);

    /// <summary>
    /// Gets the rune used to fill the backdrop.
    /// </summary>
    public Rune FillRune { get; init; } = new(' ');

    /// <summary>
    /// Gets a value indicating whether the rendered fill uses a dim text attribute.
    /// </summary>
    /// <remarks>
    /// This is used by modal backdrops to reduce the perceived contrast of content behind the modal overlay.
    /// </remarks>
    public bool Dim { get; init; } = true;

    /// <summary>
    /// Gets a value indicating whether the backdrop should use <see cref="Theme.Background"/> as fill background.
    /// </summary>
    /// <remarks>
    /// When <see langword="false"/>, the default implementation uses <see cref="Theme.Disabled"/> as the backdrop background,
    /// which is appropriate for dimming behind modal dialogs.
    /// </remarks>
    public bool UseThemeBackground { get; init; }

    /// <summary>
    /// Gets an optional explicit fill background color.
    /// </summary>
    /// <remarks>
    /// When set, this color takes precedence over <see cref="UseThemeBackground"/> and theme-derived defaults.
    /// </remarks>
    public Color? Background { get; init; }

    /// <summary>
    /// Gets an optional explicit fill foreground color.
    /// </summary>
    public Color? Foreground { get; init; }

    internal Style Resolve(Theme theme)
    {
        ArgumentNullException.ThrowIfNull(theme);

        var style = Style.None;

        var fg = Foreground ?? theme.Foreground ?? Color.Default;
        style = style.WithForeground(fg);

        var bg = Background;
        if (bg is null)
        {
            bg = UseThemeBackground ? theme.Background : theme.Disabled;
        }

        if (bg is { } bgc)
        {
            style = style.WithBackground(bgc);
        }

        if (Dim)
        {
            style |= TextStyle.Dim;
        }

        // Backdrops typically fill with a blank rune; explicitly specify the text style to avoid inheriting
        // decorations (e.g. underline) from the underlay.
        return style.WithTextStyle(style.TextStyle);
    }
}
