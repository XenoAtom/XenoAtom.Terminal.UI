// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Rendering;

namespace XenoAtom.Terminal.UI.Styling;

public sealed class ButtonStyle
{
    public static ButtonStyle Default { get; } = new();

    public static EnvironmentKey<ButtonStyle> Key { get; } = new("ButtonStyle", Default);

    public Thickness Padding { get; init; } = new(2, 0, 2, 0);

    public CellStyle? Normal { get; init; }
    public CellStyle? Hovered { get; init; }
    public CellStyle? Pressed { get; init; }
    public CellStyle? Focused { get; init; }
    public CellStyle? Disabled { get; init; }

    public CellStyle Resolve(Theme theme, bool enabled, bool focused, bool hovered, bool pressed, ControlTone tone)
    {
        if (!enabled)
        {
            return Disabled ?? CellStyle.Dim;
        }

        if (pressed)
        {
            return Pressed ?? theme.SelectionStyle();
        }

        if (focused)
        {
            return Focused ?? theme.SelectionStyle();
        }

        if (hovered)
        {
            if (Hovered is { } h)
            {
                return h;
            }

            var style = CellStyle.None;
            if (theme.Selection is { } selection)
            {
                style = style.WithBackground(selection);
            }
            return style;
        }

        if (Normal is { } n)
        {
            return n;
        }

        var bg = tone switch
        {
            ControlTone.Primary => theme.Primary ?? theme.Accent,
            ControlTone.Success => theme.Success,
            ControlTone.Warning => theme.Warning,
            ControlTone.Error => theme.Error,
            _ => theme.SurfaceAlt ?? theme.Surface ?? theme.Background,
        };

        var fg = tone is ControlTone.Default ? theme.Foreground : (theme.Background ?? theme.Foreground);

        var resolved = CellStyle.None;
        if (fg is { } fgc) resolved = resolved.WithForeground(fgc);
        if (bg is { } bgc) resolved = resolved.WithBackground(bgc);
        return resolved;
    }
}
