// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Text;
using XenoAtom.Terminal.UI.Geometry;

namespace XenoAtom.Terminal.UI.Styling;

/// <summary>
/// Defines rendering and theming options for a <see cref="Controls.Button"/>.
/// </summary>
public sealed record ButtonStyle : IStyle<ButtonStyle>
{
    /// <summary>
    /// Gets the default button style.
    /// </summary>
    public static ButtonStyle Default { get; } = new();

    /// <summary>
    /// Gets the environment key used to resolve a <see cref="ButtonStyle"/>.
    /// </summary>
    public static StyleKey<ButtonStyle> Key { get; } = new("ButtonStyle", Default);

    /// <summary>
    /// Gets the padding applied around the content.
    /// </summary>
    public Thickness Padding { get; init; } = new(2, 0, 2, 0);

    /// <summary>
    /// Gets a value indicating whether a border is rendered.
    /// </summary>
    public bool ShowBorder { get; init; }

    /// <summary>
    /// Gets the glyph used for the top border.
    /// </summary>
    public Rune BorderTopGlyph { get; init; } = new(' '); // U+2594

    /// <summary>
    /// Gets the glyph used for the bottom border.
    /// </summary>
    public Rune BorderBottomGlyph { get; init; } = new('▁'); // U+2581

    /// <summary>
    /// Gets the optional style for the normal state.
    /// </summary>
    public Style? Normal { get; init; }

    /// <summary>
    /// Gets the optional style for the hovered state.
    /// </summary>
    public Style? Hovered { get; init; }

    /// <summary>
    /// Gets the optional style for the pressed state.
    /// </summary>
    public Style? Pressed { get; init; }

    /// <summary>
    /// Gets the optional style for the focused state.
    /// </summary>
    public Style? Focused { get; init; }

    /// <summary>
    /// Gets the optional style for the disabled state.
    /// </summary>
    public Style? Disabled { get; init; }

    /// <summary>
    /// Resolves the button style for the provided state.
    /// </summary>
    /// <param name="theme">The current theme.</param>
    /// <param name="enabled">Whether the button is enabled.</param>
    /// <param name="focused">Whether the button is focused.</param>
    /// <param name="hovered">Whether the pointer is hovering over the button.</param>
    /// <param name="pressed">Whether the button is pressed.</param>
    /// <param name="tone">The semantic tone of the button.</param>
    public Style Resolve(Theme theme, bool enabled, bool focused, bool hovered, bool pressed, ControlTone tone)
    {
        ArgumentNullException.ThrowIfNull(theme);

        var normal = Normal ?? ResolveDefaultNormal(theme, tone);

        if (!enabled)
        {
            if (Disabled is { } disabled)
            {
                return disabled;
            }

            if (theme.Disabled is { } disabledFg)
            {
                normal = normal.WithForeground(disabledFg);
            }

            return normal | TextStyle.Dim;
        }

        if (pressed)
        {
            return Pressed ?? ResolveDefaultPressed(theme, normal);
        }

        var style = normal;
        if (hovered)
        {
            style = Hovered ?? ResolveDefaultHovered(theme, style, tone);
        }

        if (focused)
        {
            style = Focused ?? ResolveDefaultFocused(theme, style, tone);
        }

        return style;
    }

    private static Style ResolveDefaultNormal(Theme theme, ControlTone tone)
    {
        var (fg, bg) = tone switch
        {
            ControlTone.Primary => (theme.Background ?? theme.Foreground, theme.Primary ?? theme.Accent),
            ControlTone.Success => (theme.Background ?? theme.Foreground, theme.Success),
            ControlTone.Warning => (theme.Background ?? theme.Foreground, theme.Warning),
            ControlTone.Error => (theme.Background ?? theme.Foreground, theme.Error),
            _ => (theme.Foreground, theme.ControlFill ?? theme.SurfaceAlt ?? theme.Surface ?? theme.Background),
        };

        var resolved = Style.None;
        if (fg is { } fgc) resolved = resolved.WithForeground(fgc);
        if (bg is { } bgc) resolved = resolved.WithBackground(bgc);
        return resolved;
    }

    private static Style ResolveDefaultHovered(Theme theme, Style normal, ControlTone tone)
    {
        if (tone == ControlTone.Default && (theme.ControlFillHover ?? theme.SurfaceAlt) is { } hoverBg)
        {
            return normal.WithBackground(hoverBg);
        }

        return normal | TextStyle.Bold;
    }

    private static Style ResolveDefaultPressed(Theme theme, Style normal)
    {
        if ((theme.ControlFillPressed ?? theme.Selection) is { } selectionBg)
        {
            normal = normal.WithBackground(selectionBg);
        }

        return normal | TextStyle.Bold;
    }

    private static Style ResolveDefaultFocused(Theme theme, Style normal, ControlTone tone)
    {
        var style = normal;

        // Focus should not look like a pressed state. Prefer an accent foreground + underline.
        if (tone == ControlTone.Default)
        {
            if (theme.FocusBorder is { } focus)
            {
                style = style.WithForeground(focus);
            }

            style |= TextStyle.Underline;
        }

        return style;
    }
}
