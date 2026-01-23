// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Text;
using XenoAtom.Terminal.UI.Geometry;

namespace XenoAtom.Terminal.UI.Styling;

/// <summary>
/// Defines styling options for validation message presentation.
/// </summary>
public sealed record ValidationStyle : IStyle<ValidationStyle>
{
    /// <summary>
    /// Gets the default validation style.
    /// </summary>
    public static ValidationStyle Default { get; } = new();

    /// <summary>
    /// Gets the environment key for <see cref="ValidationStyle"/>.
    /// </summary>
    public static StyleKey<ValidationStyle> Key { get; } = new("ValidationStyle", Default);

    /// <summary>
    /// Gets the padding applied around the validation message content.
    /// </summary>
    public Thickness Padding { get; init; } = new(1, 0, 1, 0);

    /// <summary>
    /// Gets the spacing between the severity glyph and the message content.
    /// </summary>
    public int GlyphSpacing { get; init; } = 1;

    /// <summary>
    /// Gets the spacing between the wrapped control and the message line.
    /// </summary>
    public int Gap { get; init; } = 0;

    /// <summary>
    /// Gets the optional information glyph.
    /// </summary>
    public Rune? InfoGlyph { get; init; } = new Rune(0x2139); // ℹ

    /// <summary>
    /// Gets the optional warning glyph.
    /// </summary>
    public Rune? WarningGlyph { get; init; } = new Rune(0x26A0); // ⚠

    /// <summary>
    /// Gets the optional error glyph.
    /// </summary>
    public Rune? ErrorGlyph { get; init; } = new Rune(0x26D4); // ⛔

    /// <summary>
    /// Gets an optional style override for information messages.
    /// </summary>
    public Style? InfoStyle { get; init; }

    /// <summary>
    /// Gets an optional style override for warning messages.
    /// </summary>
    public Style? WarningStyle { get; init; }

    /// <summary>
    /// Gets an optional style override for error messages.
    /// </summary>
    public Style? ErrorStyle { get; init; }

    internal Rune? ResolveGlyph(Controls.ValidationSeverity severity)
        => severity switch
        {
            Controls.ValidationSeverity.Info => InfoGlyph,
            Controls.ValidationSeverity.Warning => WarningGlyph,
            _ => ErrorGlyph,
        };

    internal Style ResolveLineStyle(Theme theme, Controls.ValidationSeverity severity)
    {
        var style = theme.BaseTextStyle();

        var overrideStyle = severity switch
        {
            Controls.ValidationSeverity.Info => InfoStyle,
            Controls.ValidationSeverity.Warning => WarningStyle,
            _ => ErrorStyle,
        };

        if (overrideStyle is not null)
        {
            return overrideStyle.Value.MergeUnspecified(style);
        }

        var fg = severity switch
        {
            Controls.ValidationSeverity.Info => theme.Muted ?? theme.Foreground,
            Controls.ValidationSeverity.Warning => theme.Warning ?? theme.Foreground,
            _ => theme.Error ?? theme.Foreground,
        };

        if (fg is { } f)
        {
            style = style.WithForeground(f);
        }

        return style;
    }

    internal TextBlockStyle ResolveTextBlockStyle(Theme theme, Controls.ValidationSeverity severity)
    {
        var line = ResolveLineStyle(theme, severity);
        var text = TextBlockStyle.Default;

        if (line.TryGetForeground(out var fg))
        {
            text = text with { Foreground = fg };
        }

        return text;
    }

    internal string BuildPrefix(Controls.ValidationSeverity severity)
    {
        var glyph = ResolveGlyph(severity);
        if (glyph is null)
        {
            return string.Empty;
        }

        var builder = new StringBuilder();
        builder.Append(glyph.Value.ToString());
        var spacing = Math.Max(0, GlyphSpacing);
        if (spacing > 0)
        {
            builder.Append(' ', spacing);
        }

        return builder.ToString();
    }
}
