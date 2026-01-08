// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Geometry;

namespace XenoAtom.Terminal.UI.Styling;

public sealed record ButtonStyle : IStyle<ButtonStyle>
{
    public static ButtonStyle Default { get; } = new();

    public static StyleKey<ButtonStyle> Key { get; } = new("ButtonStyle", Default);

    public Thickness Padding { get; init; } = new(2, 0, 2, 0);

    public bool ShowBorder { get; init; }

    public EdgeBorderGlyphs BorderGlyphs { get; init; } = EdgeBorderGlyphs.LegacyComputing;

    public CellStyle? Normal { get; init; }
    public CellStyle? Hovered { get; init; }
    public CellStyle? Pressed { get; init; }
    public CellStyle? Focused { get; init; }
    public CellStyle? Disabled { get; init; }

    public CellStyle Resolve(Theme theme, bool enabled, bool focused, bool hovered, bool pressed, ControlTone tone)
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

    private static CellStyle ResolveDefaultNormal(Theme theme, ControlTone tone)
    {
        var (fg, bg) = tone switch
        {
            ControlTone.Primary => (theme.Background ?? theme.Foreground, theme.Primary ?? theme.Accent),
            ControlTone.Success => (theme.Background ?? theme.Foreground, theme.Success),
            ControlTone.Warning => (theme.Background ?? theme.Foreground, theme.Warning),
            ControlTone.Error => (theme.Background ?? theme.Foreground, theme.Error),
            _ => (theme.Foreground, theme.Surface ?? theme.SurfaceAlt ?? theme.Background),
        };

        var resolved = CellStyle.None;
        if (fg is { } fgc) resolved = resolved.WithForeground(fgc);
        if (bg is { } bgc) resolved = resolved.WithBackground(bgc);
        return resolved;
    }

    private static CellStyle ResolveDefaultHovered(Theme theme, CellStyle normal, ControlTone tone)
    {
        if (tone == ControlTone.Default && theme.SurfaceAlt is { } hoverBg)
        {
            return normal.WithBackground(hoverBg);
        }

        return normal | TextStyle.Bold;
    }

    private static CellStyle ResolveDefaultPressed(Theme theme, CellStyle normal)
    {
        if (theme.Selection is { } selectionBg)
        {
            normal = normal.WithBackground(selectionBg);
        }

        return normal | TextStyle.Bold;
    }

    private static CellStyle ResolveDefaultFocused(Theme theme, CellStyle normal, ControlTone tone)
    {
        var style = normal | TextStyle.Underline;

        if (tone == ControlTone.Default && theme.FocusBorder is { } focus)
        {
            style = style.WithForeground(focus);
        }

        return style;
    }
}
