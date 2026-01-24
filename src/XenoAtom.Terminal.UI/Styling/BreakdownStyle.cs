// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Collections.Generic;
using System.Text;

namespace XenoAtom.Terminal.UI.Styling;

/// <summary>
/// Defines styling options for the <see cref="Controls.BreakdownChart"/> control.
/// </summary>
public sealed record BreakdownStyle : IStyle<BreakdownStyle>
{
    /// <summary>
    /// Gets the default breakdown style.
    /// </summary>
    public static BreakdownStyle Default { get; } = new()
    {
        FillRune = new Rune(' '),
        SegmentGap = 1,
    };

    /// <summary>
    /// Gets the environment key for <see cref="BreakdownStyle"/>.
    /// </summary>
    public static StyleKey<BreakdownStyle> Key { get; } = new("BreakdownStyle", Default);

    /// <summary>
    /// Gets the rune used to fill segment cells.
    /// </summary>
    /// <remarks>
    /// This defaults to a space so segments are rendered via background colors. When creating a new
    /// <see cref="BreakdownStyle"/> instance, prefer overriding only the properties you need.
    /// </remarks>
    public Rune FillRune { get; init; } = new(' ');

    /// <summary>
    /// Gets the number of empty cells inserted between segments.
    /// </summary>
    public int SegmentGap { get; init; } = 1;

    /// <summary>
    /// Gets how legend items are laid out.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The compact layout packs legend items into as few lines as possible, similar to Spectre.Console/Rich.
    /// </para>
    /// <para>
    /// The expanded layout renders one item per line.
    /// </para>
    /// </remarks>
    public Controls.BreakdownLegendLayout LegendLayout { get; init; } = Controls.BreakdownLegendLayout.Compact;

    /// <summary>
    /// Gets the spacing (in cells) between legend items when using the compact layout.
    /// </summary>
    public int LegendItemSpacing { get; init; } = 4;

    /// <summary>
    /// Gets how leftover space is distributed along the legend line when using the compact layout.
    /// </summary>
    public Controls.WrapJustify LegendJustify { get; init; } = Controls.WrapJustify.Start;

    /// <summary>
    /// Gets an optional base style applied to all bar cells.
    /// </summary>
    public Style? BarStyle { get; init; }

    /// <summary>
    /// Gets an optional base style applied to legend content.
    /// </summary>
    public Style? LegendStyle { get; init; }

    /// <summary>
    /// Gets an optional muted style applied to secondary legend content (percentages/values).
    /// </summary>
    public Style? LegendMutedStyle { get; init; }

    /// <summary>
    /// Gets an optional list of default segment colors.
    /// </summary>
    /// <remarks>
    /// When a segment does not specify a color, the breakdown cycles through this list. When not provided, the control
    /// falls back to theme tones (Primary/Success/Warning/Error).
    /// </remarks>
    public IReadOnlyList<Color?>? DefaultSegmentColors { get; init; }
}
