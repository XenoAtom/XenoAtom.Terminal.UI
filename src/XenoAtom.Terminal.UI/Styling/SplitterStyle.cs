// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Text;
using XenoAtom.Terminal.UI.Controls;

namespace XenoAtom.Terminal.UI.Styling;

/// <summary>
/// Defines rendering and theming options for <see cref="Splitter"/> derivatives.
/// </summary>
public sealed record SplitterStyle : IStyle<SplitterStyle>
{
    /// <summary>
    /// Gets the default splitter style.
    /// </summary>
    public static SplitterStyle Default { get; } = new();

    /// <summary>
    /// Gets the environment key used to resolve a <see cref="SplitterStyle"/>.
    /// </summary>
    public static StyleKey<SplitterStyle> Key { get; } = new("SplitterStyle", Default);

    /// <summary>
    /// Gets the glyph used for horizontal split bars.
    /// </summary>
    public Rune HorizontalGlyph { get; init; } = new(0x2504); // ┄

    /// <summary>
    /// Gets the glyph used for horizontal split bars when hovered.
    /// </summary>
    public Rune HoverHorizontalGlyph { get; init; } = new(0x2501); // ━

    /// <summary>
    /// Gets the glyph used for vertical split bars.
    /// </summary>
    public Rune VerticalGlyph { get; init; } = new(0x250A); // ┊

    /// <summary>
    /// Gets the glyph used for vertical split bars when hovered.
    /// </summary>
    public Rune HoverVerticalGlyph { get; init; } = new(0x2503); // ┃

    /// <summary>
    /// Gets the optional base bar style.
    /// </summary>
    public Style? BarStyle { get; init; }

    /// <summary>
    /// Gets the optional style used when hovered.
    /// </summary>
    public Style? HoverStyle { get; init; }

    /// <summary>
    /// Gets the optional style used when focused.
    /// </summary>
    public Style? FocusStyle { get; init; }

    /// <summary>
    /// Gets the optional style used while dragging.
    /// </summary>
    public Style? DragStyle { get; init; }

    /// <summary>
    /// Gets the optional style used when disabled.
    /// </summary>
    public Style? DisabledStyle { get; init; }

    /// <summary>
    /// Resolves the splitter bar style for the provided state.
    /// </summary>
    /// <param name="theme">The current theme.</param>
    /// <param name="enabled">Whether the splitter is enabled.</param>
    /// <param name="focused">Whether the splitter is focused.</param>
    /// <param name="hovered">Whether the bar is hovered.</param>
    /// <param name="dragging">Whether the bar is being dragged.</param>
    public Style Resolve(Theme theme, bool enabled, bool focused, bool hovered, bool dragging)
    {
        if (!enabled)
        {
            if (DisabledStyle is { } d)
            {
                return d;
            }

            var dim = theme.BorderStyle(focused: false) | TextStyle.Dim;
            if (theme.Disabled is { } c)
            {
                dim = dim.WithForeground(c);
            }
            return dim;
        }

        if (dragging)
        {
            return DragStyle ?? (theme.BorderStyle(focused: true) | TextStyle.Bold);
        }

        if (hovered)
        {
            return HoverStyle ?? (theme.BorderStyle(focused: true) | TextStyle.Bold);
        }

        if (focused)
        {
            return FocusStyle ?? (theme.BorderStyle(focused: true));
        }

        // By default, make the separator line subtle so content remains the focus; hover/drag provides the strong affordance.
        return BarStyle ?? (theme.BorderStyle(focused: false) | TextStyle.Dim);
    }
}
