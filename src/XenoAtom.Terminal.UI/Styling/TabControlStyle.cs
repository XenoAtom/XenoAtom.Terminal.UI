// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Text;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Controls;

namespace XenoAtom.Terminal.UI.Styling;

/// <summary>
/// Defines rendering and theming options for a <see cref="Controls.TabControl"/>.
/// </summary>
public sealed record TabControlStyle : IStyle<TabControlStyle>
{
    /// <summary>
    /// Gets the default tab control style.
    /// </summary>
    public static TabControlStyle Default { get; } = new()
    {
        TabContentTemplateFactory = host => new Border(host)
            .HorizontalAlignment(Align.Stretch)
            .VerticalAlignment(Align.Stretch),
    };

    /// <summary>
    /// Gets a predefined tab control style that does not apply an additional content template.
    /// </summary>
    public static TabControlStyle NoBorder { get; } = new();

    /// <summary>
    /// Gets a predefined tab control style with a rounded content border.
    /// </summary>
    public static TabControlStyle Rounded { get; } = Default with
    {
        TabContentTemplateFactory = host => new Border(host)
            .Style(BorderStyle.Rounded)
            .HorizontalAlignment(Align.Stretch)
            .VerticalAlignment(Align.Stretch),
    };

    /// <summary>
    /// Gets a predefined tab control style with a single-line content border.
    /// </summary>
    public static TabControlStyle Single { get; } = Default with
    {
        TabContentTemplateFactory = host => new Border(host)
            .Style(BorderStyle.Single)
            .HorizontalAlignment(Align.Stretch)
            .VerticalAlignment(Align.Stretch),
    };

    /// <summary>
    /// Gets a predefined tab control style with a double-line content border.
    /// </summary>
    public static TabControlStyle Double { get; } = Default with
    {
        TabContentTemplateFactory = host => new Border(host)
            .Style(BorderStyle.Double)
            .HorizontalAlignment(Align.Stretch)
            .VerticalAlignment(Align.Stretch),
    };

    /// <summary>
    /// Gets a predefined tab control style with a heavy content border.
    /// </summary>
    public static TabControlStyle Heavy { get; } = Default with
    {
        TabContentTemplateFactory = host => new Border(host)
            .Style(BorderStyle.Heavy)
            .HorizontalAlignment(Align.Stretch)
            .VerticalAlignment(Align.Stretch),
    };

    /// <summary>
    /// Gets a predefined tab control style with an ASCII content border.
    /// </summary>
    public static TabControlStyle Ascii { get; } = Default with
    {
        TabContentTemplateFactory = host => new Border(host)
            .Style(BorderStyle.Ascii)
            .HorizontalAlignment(Align.Stretch)
            .VerticalAlignment(Align.Stretch),
    };

    /// <summary>
    /// Gets a predefined tab control style with a heavy ASCII content border.
    /// </summary>
    public static TabControlStyle AsciiHeavy { get; } = Default with
    {
        TabContentTemplateFactory = host => new Border(host)
            .Style(BorderStyle.AsciiHeavy)
            .HorizontalAlignment(Align.Stretch)
            .VerticalAlignment(Align.Stretch),
    };

    /// <summary>
    /// Gets a predefined tab control style with a dashed content border.
    /// </summary>
    public static TabControlStyle Dashed { get; } = Default with
    {
        TabContentTemplateFactory = host => new Border(host)
            .Style(BorderStyle.Dashed)
            .HorizontalAlignment(Align.Stretch)
            .VerticalAlignment(Align.Stretch),
    };

    /// <summary>
    /// Gets the environment key used to resolve a <see cref="TabControlStyle"/>.
    /// </summary>
    public static StyleKey<TabControlStyle> Key { get; } = new("TabControlStyle", Default);

    /// <summary>
    /// Gets the padding applied around each tab header.
    /// </summary>
    public Thickness TabPadding { get; init; } = new(Left: 2, Top: 0, Right: 2, Bottom: 0);

    /// <summary>
    /// Gets the optional style for the tab strip background.
    /// </summary>
    public Style? StripStyle { get; init; }

    /// <summary>
    /// Gets the optional base style for a tab header.
    /// </summary>
    public Style? TabStyle { get; init; }

    /// <summary>
    /// Gets the optional style for a hovered tab header.
    /// </summary>
    public Style? TabHoveredStyle { get; init; }

    /// <summary>
    /// Gets the optional style for a pressed tab header.
    /// </summary>
    public Style? TabPressedStyle { get; init; }

    /// <summary>
    /// Gets the optional style for a selected tab header.
    /// </summary>
    public Style? TabSelectedStyle { get; init; }

    /// <summary>
    /// Gets the optional style for a disabled tab header.
    /// </summary>
    public Style? TabDisabledStyle { get; init; }

    /// <summary>
    /// Gets the rune used for the tab close button.
    /// </summary>
    public Rune CloseButtonRune { get; init; } = new('×');

    /// <summary>
    /// Gets the number of cells reserved between the tab header content and the close button.
    /// </summary>
    public int CloseButtonSpacing { get; init; } = 1;

    /// <summary>
    /// Gets the optional base style for a tab close button.
    /// </summary>
    public Style? CloseButtonStyle { get; init; }

    /// <summary>
    /// Gets the optional style for a hovered tab close button.
    /// </summary>
    public Style? CloseButtonHoveredStyle { get; init; }

    /// <summary>
    /// Gets the optional style for a pressed tab close button.
    /// </summary>
    public Style? CloseButtonPressedStyle { get; init; }

    /// <summary>
    /// Gets the optional style for a disabled tab close button.
    /// </summary>
    public Style? CloseButtonDisabledStyle { get; init; }

    /// <summary>
    /// Gets the rune used by the overflow button that reveals earlier tabs.
    /// </summary>
    public Rune OverflowPreviousRune { get; init; } = new('◀');

    /// <summary>
    /// Gets the rune used by the overflow button that reveals later tabs.
    /// </summary>
    public Rune OverflowNextRune { get; init; } = new('▶');

    /// <summary>
    /// Gets the optional base style for overflow navigation buttons.
    /// </summary>
    public Style? OverflowButtonStyle { get; init; }

    /// <summary>
    /// Gets the optional style for hovered overflow navigation buttons.
    /// </summary>
    public Style? OverflowButtonHoveredStyle { get; init; }

    /// <summary>
    /// Gets the optional style for pressed overflow navigation buttons.
    /// </summary>
    public Style? OverflowButtonPressedStyle { get; init; }

    /// <summary>
    /// Gets the optional style for disabled overflow navigation buttons.
    /// </summary>
    public Style? OverflowButtonDisabledStyle { get; init; }

    /// <summary>
    /// Gets an optional template factory used to wrap the tab content host.
    /// </summary>
    /// <remarks>
    /// The provided visual is an internal host that contains the selected tab content.
    /// The returned visual should wrap that host (e.g. a <see cref="Border"/> or <see cref="Group"/>).
    /// When <see langword="null"/>, the host is rendered directly with no additional wrapper.
    /// </remarks>
    public Func<Visual, ContentVisual?>? TabContentTemplateFactory { get; init; }

    /// <summary>
    /// Resolves the strip style for the provided <paramref name="theme"/>.
    /// </summary>
    public Style ResolveStripStyle(Theme theme) => StripStyle ?? theme.BaseTextStyle();

    /// <summary>
    /// Resolves the tab style for the provided state.
    /// </summary>
    /// <param name="theme">The current theme.</param>
    /// <param name="enabled">Whether the tab is enabled.</param>
    /// <param name="focused">Whether the tab control is focused.</param>
    /// <param name="selected">Whether the tab is selected.</param>
    /// <param name="hovered">Whether the tab is hovered.</param>
    /// <param name="pressed">Whether the tab is pressed.</param>
    public Style ResolveTabStyle(Theme theme, bool enabled, bool focused, bool selected, bool hovered, bool pressed)
    {
        ArgumentNullException.ThrowIfNull(theme);

        var normal = TabStyle ?? theme.SurfaceStyle();

        if (!enabled)
        {
            var disabled = normal | TextStyle.Dim;
            if (theme.Disabled is { } c)
            {
                disabled = disabled.WithForeground(c);
            }
            return TabDisabledStyle ?? disabled;
        }

        if (pressed)
        {
            return TabPressedStyle ?? ResolveDefaultPressed(theme, normal);
        }

        if (selected)
        {
            var resolved = TabSelectedStyle ?? ResolveDefaultSelected(theme, normal);
            if (focused)
            {
                resolved = ResolveDefaultFocused(theme, resolved);
            }
            return resolved;
        }

        if (hovered)
        {
            return TabHoveredStyle ?? ResolveDefaultHovered(theme, normal);
        }

        return normal;
    }

    /// <summary>
    /// Resolves the close button style for the provided state.
    /// </summary>
    /// <param name="theme">The current theme.</param>
    /// <param name="tabStyle">The resolved tab header style.</param>
    /// <param name="enabled">Whether the parent tab is enabled.</param>
    /// <param name="hovered">Whether the close button is hovered.</param>
    /// <param name="pressed">Whether the close button is pressed.</param>
    public Style ResolveCloseButtonStyle(Theme theme, Style tabStyle, bool enabled, bool hovered, bool pressed)
    {
        ArgumentNullException.ThrowIfNull(theme);

        var normal = CloseButtonStyle ?? tabStyle;
        if (!enabled)
        {
            return CloseButtonDisabledStyle ?? ResolveDefaultDisabled(theme, normal);
        }

        if (pressed)
        {
            return CloseButtonPressedStyle ?? ResolveDefaultClosePressed(theme, normal);
        }

        if (hovered)
        {
            return CloseButtonHoveredStyle ?? ResolveDefaultCloseHovered(theme, normal);
        }

        return normal;
    }

    /// <summary>
    /// Resolves the overflow button style for the provided state.
    /// </summary>
    /// <param name="theme">The current theme.</param>
    /// <param name="enabled">Whether the button is enabled.</param>
    /// <param name="hovered">Whether the button is hovered.</param>
    /// <param name="pressed">Whether the button is pressed.</param>
    public Style ResolveOverflowButtonStyle(Theme theme, bool enabled, bool hovered, bool pressed)
    {
        ArgumentNullException.ThrowIfNull(theme);

        var normal = OverflowButtonStyle ?? TabStyle ?? theme.SurfaceStyle();
        if (!enabled)
        {
            return OverflowButtonDisabledStyle ?? ResolveDefaultDisabled(theme, normal);
        }

        if (pressed)
        {
            return OverflowButtonPressedStyle ?? ResolveDefaultPressed(theme, normal);
        }

        if (hovered)
        {
            return OverflowButtonHoveredStyle ?? ResolveDefaultHovered(theme, normal);
        }

        return normal;
    }

    private static Style ResolveDefaultHovered(Theme theme, Style normal)
    {
        if ((theme.ControlFillHover ?? theme.SurfaceAlt) is { } hoverBg)
        {
            normal = normal.WithBackground(hoverBg);
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

    private static Style ResolveDefaultSelected(Theme theme, Style normal)
    {
        var style = normal | TextStyle.Bold;
        if (theme.Accent is { } accent)
        {
            style = style.WithForeground(accent);
        }
        return style;
    }

    private static Style ResolveDefaultFocused(Theme theme, Style style)
    {
        if (theme.FocusBorder is { } focus)
        {
            style = style.WithForeground(focus);
        }

        return style | TextStyle.Underline;
    }

    private static Style ResolveDefaultDisabled(Theme theme, Style normal)
    {
        if (theme.Disabled is { } disabled)
        {
            normal = normal.WithForeground(disabled);
        }

        return normal | TextStyle.Dim;
    }

    private static Style ResolveDefaultCloseHovered(Theme theme, Style normal)
    {
        if ((theme.Error ?? theme.ControlFillHover ?? theme.SurfaceAlt) is { } bg)
        {
            normal = normal.WithBackground(bg);
        }

        if ((theme.Background ?? theme.Foreground) is { } fg)
        {
            normal = normal.WithForeground(fg);
        }

        return normal | TextStyle.Bold;
    }

    private static Style ResolveDefaultClosePressed(Theme theme, Style normal)
    {
        if ((theme.Error ?? theme.ControlFillPressed ?? theme.Selection) is { } bg)
        {
            normal = normal.WithBackground(bg);
        }

        if ((theme.Background ?? theme.Foreground) is { } fg)
        {
            normal = normal.WithForeground(fg);
        }

        return normal | TextStyle.Bold;
    }
}
