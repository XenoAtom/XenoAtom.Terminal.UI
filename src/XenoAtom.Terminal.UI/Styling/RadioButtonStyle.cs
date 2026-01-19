// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Text;

namespace XenoAtom.Terminal.UI.Styling;

/// <summary>
/// Defines rendering and theming options for a <see cref="Controls.RadioButton"/>.
/// </summary>
public sealed record RadioButtonStyle : IStyle<RadioButtonStyle>
{
    /// <summary>
    /// Gets the default radio button style.
    /// </summary>
    public static RadioButtonStyle Default { get; } = new();

    /// <summary>
    /// Gets the environment key used to resolve a <see cref="RadioButtonStyle"/>.
    /// </summary>
    public static StyleKey<RadioButtonStyle> Key { get; } = new("RadioButtonStyle", Default);

    /// <summary>
    /// Gets the glyph used for the checked state.
    /// </summary>
    public Rune CheckedGlyph { get; init; } = new('◉');

    /// <summary>
    /// Gets the glyph used for the unchecked state.
    /// </summary>
    public Rune UncheckedGlyph { get; init; } = new('◯');

    /// <summary>
    /// Gets the optional style used for the normal state.
    /// </summary>
    public Style? Normal { get; init; }
    
    /// <summary>
    /// Gets the optional style used for the hovered state.
    /// </summary>
    public Style? Hovered { get; init; }
    
    /// <summary>
    /// Gets the optional style used for the focused state.
    /// </summary>
    public Style? Focused { get; init; }
    
    /// <summary>
    /// Gets the optional style used for the disabled state.
    /// </summary>
    public Style? Disabled { get; init; }

    /// <summary>
    /// Resolves the radio button style for the provided state.
    /// </summary>
    /// <param name="theme">The current theme.</param>
    /// <param name="enabled">Whether the control is enabled.</param>
    /// <param name="focused">Whether the control is focused.</param>
    /// <param name="hovered">Whether the control is hovered.</param>
    public Style Resolve(Theme theme, bool enabled, bool focused, bool hovered)
    {
        var baseStyle = theme.ForegroundTextStyle();

        if (!enabled)
        {
            if (Disabled is { } d)
            {
                return d;
            }

            var disabled = baseStyle | TextStyle.Dim;
            if (theme.Disabled is { } c)
            {
                disabled = disabled.WithForeground(c);
            }
            return disabled;
        }

        if (focused)
        {
            if (Focused is { } f)
            {
                return f;
            }

            var focusedStyle = baseStyle | TextStyle.Bold;
            if (theme.FocusBorder is { } c)
            {
                focusedStyle = focusedStyle.WithForeground(c);
            }
            return focusedStyle;
        }

        if (hovered)
        {
            if (Hovered is { } h)
            {
                return h;
            }

            var style = baseStyle;
            if (theme.Accent is { } selection)
            {
                style = style.WithForeground(selection);
            }
            return style;
        }

        return Normal ?? baseStyle;
    }
}
