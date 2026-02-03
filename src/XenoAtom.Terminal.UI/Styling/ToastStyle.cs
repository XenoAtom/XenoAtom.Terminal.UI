// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Text;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Geometry;

namespace XenoAtom.Terminal.UI.Styling;

/// <summary>
/// Defines rendering and theming options for <see cref="Toast"/> notifications.
/// </summary>
public sealed record ToastStyle : IStyle<ToastStyle>
{
    /// <summary>
    /// Gets the default toast style.
    /// </summary>
    public static ToastStyle Default { get; } = new();

    /// <summary>
    /// Gets the environment key used to resolve a <see cref="ToastStyle"/>.
    /// </summary>
    public static StyleKey<ToastStyle> Key { get; } = new("ToastStyle", Default);

    /// <summary>
    /// Gets the minimum toast width.
    /// </summary>
    public int MinWidth { get; init; } = 30;

    /// <summary>
    /// Gets the maximum toast width.
    /// </summary>
    public int MaxWidth { get; init; } = 60;

    /// <summary>
    /// Gets the padding applied inside the toast container.
    /// </summary>
    public Thickness Padding { get; init; } = new(1);

    /// <summary>
    /// Gets the spacing between the icon and the title/content.
    /// </summary>
    public int IconSpacing { get; init; } = 1;

    /// <summary>
    /// Gets the border style applied to the toast container.
    /// </summary>
    public BorderStyle BorderStyle { get; init; } = BorderStyle.Rounded;

    /// <summary>
    /// Gets the icon for informational toasts.
    /// </summary>
    public string InfoIcon { get; init; } = "ℹ️";

    /// <summary>
    /// Gets the icon for success toasts.
    /// </summary>
    public string SuccessIcon { get; init; } = "✅";

    /// <summary>
    /// Gets the icon for warning toasts.
    /// </summary>
    public string WarningIcon { get; init; } = "⚠️";

    /// <summary>
    /// Gets the icon for error toasts.
    /// </summary>
    public string ErrorIcon { get; init; } = "❌";

    /// <summary>
    /// Gets the icon used for the close button.
    /// </summary>
    public string CloseIcon { get; init; } = "\u2715";

    /// <summary>
    /// Gets the progress bar style used for countdowns.
    /// </summary>
    public ProgressBarStyle ProgressStyle { get; init; } = ProgressBarStyle.Thin;

    /// <summary>
    /// Resolves the base container style for the given <paramref name="severity"/>.
    /// </summary>
    /// <param name="theme">The current theme.</param>
    /// <param name="severity">The toast severity.</param>
    public Style ResolveStyle(Theme theme, ToastSeverity severity)
    {
        ArgumentNullException.ThrowIfNull(theme);

        var style = Style.None;
        if (theme.Foreground is { } fg)
        {
            style = style.WithForeground(fg);
        }

        var background = theme.PopupSurface ?? theme.Surface ?? theme.Background;
        if (background is { } bg)
        {
            style = style.WithBackground(bg);
        }

        return style;
    }

    /// <summary>
    /// Resolves the title text style for the given <paramref name="severity"/>.
    /// </summary>
    /// <param name="theme">The current theme.</param>
    /// <param name="severity">The toast severity.</param>
    public Style ResolveTitleStyle(Theme theme, ToastSeverity severity)
        => ResolveStyle(theme, severity).AddTextStyle(TextStyle.Bold);

    /// <summary>
    /// Resolves the icon style for the given <paramref name="severity"/>.
    /// </summary>
    /// <param name="theme">The current theme.</param>
    /// <param name="severity">The toast severity.</param>
    public Style ResolveIconStyle(Theme theme, ToastSeverity severity)
    {
        ArgumentNullException.ThrowIfNull(theme);

        var color = severity switch
        {
            ToastSeverity.Success => theme.Success ?? theme.Accent,
            ToastSeverity.Warning => theme.Warning ?? theme.Accent,
            ToastSeverity.Error => theme.Error ?? theme.Accent,
            _ => theme.Accent ?? theme.Foreground,
        };

        var style = Style.None;
        if (color is { } fg)
        {
            style = style.WithForeground(fg);
        }

        return style;
    }

    /// <summary>
    /// Resolves the border style for the given <paramref name="severity"/>.
    /// </summary>
    /// <param name="theme">The current theme.</param>
    /// <param name="severity">The toast severity.</param>
    public Style ResolveBorderStyle(Theme theme, ToastSeverity severity)
    {
        ArgumentNullException.ThrowIfNull(theme);

        var color = severity switch
        {
            ToastSeverity.Success => theme.Success ?? theme.Border,
            ToastSeverity.Warning => theme.Warning ?? theme.Border,
            ToastSeverity.Error => theme.Error ?? theme.Border,
            _ => theme.Border ?? theme.Accent,
        };

        var style = Style.None;
        if (color is { } fg)
        {
            style = style.WithForeground(fg);
        }

        return style;
    }
}
