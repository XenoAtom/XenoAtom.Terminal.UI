// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI.Controls;

/// <summary>
/// Specifies the sizing mode for a <see cref="GridLength"/>.
/// </summary>
public enum GridUnitType
{
    /// <summary>
    /// Size to content.
    /// </summary>
    Auto = 0,
    /// <summary>
    /// Fixed size in cells.
    /// </summary>
    Fixed = 1,
    /// <summary>
    /// Star sizing (proportional distribution).
    /// </summary>
    Star = 2,
}

/// <summary>
/// Represents a grid length (Auto/Fixed/Star).
/// </summary>
/// <param name="Type">The unit type.</param>
/// <param name="Value">The unit value (cells for Fixed, weight for Star).</param>
public readonly record struct GridLength(GridUnitType Type, double Value)
{
    /// <summary>
    /// Gets an Auto grid length.
    /// </summary>
    public static GridLength Auto => new(GridUnitType.Auto, 0);

    /// <summary>
    /// Creates a fixed grid length in cells.
    /// </summary>
    public static GridLength Fixed(int cells) => new(GridUnitType.Fixed, Math.Max(0, cells));

    /// <summary>
    /// Creates a star grid length with a weight.
    /// </summary>
    public static GridLength Star(double weight = 1) => new(GridUnitType.Star, weight <= 0 ? 1 : weight);

    /// <inheritdoc />
    public override string ToString()
        => Type switch
        {
            GridUnitType.Auto => "Auto",
            GridUnitType.Fixed => Value.ToString(System.Globalization.CultureInfo.InvariantCulture),
            GridUnitType.Star => Value.ToString(System.Globalization.CultureInfo.InvariantCulture) + "*",
            _ => base.ToString() ?? string.Empty,
        };
}

/// <summary>
/// Defines a row in a <see cref="Grid"/>.
/// </summary>
public sealed partial class RowDefinition
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RowDefinition"/> class with default values.
    /// </summary>
    public RowDefinition()
    {
        Height = GridLength.Star(1);
        MaxHeight = int.MaxValue;
    }

    /// <summary>
    /// Gets or sets the row height.
    /// </summary>
    [Bindable]
    public partial GridLength Height { get; set; }

    /// <summary>
    /// Gets or sets the minimum row height.
    /// </summary>
    [Bindable]
    public partial int MinHeight { get; set; }

    /// <summary>
    /// Gets or sets the maximum row height.
    /// </summary>
    [Bindable]
    public partial int MaxHeight { get; set; }
}

/// <summary>
/// Defines a column in a <see cref="Grid"/>.
/// </summary>
public sealed partial class ColumnDefinition
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ColumnDefinition"/> class with default values.
    /// </summary>
    public ColumnDefinition()
    {
        Width = GridLength.Star(1);
        MaxWidth = int.MaxValue;
    }

    /// <summary>
    /// Gets or sets the column width.
    /// </summary>
    [Bindable]
    public partial GridLength Width { get; set; }

    /// <summary>
    /// Gets or sets the minimum column width.
    /// </summary>
    [Bindable]
    public partial int MinWidth { get; set; }

    /// <summary>
    /// Gets or sets the maximum column width.
    /// </summary>
    [Bindable]
    public partial int MaxWidth { get; set; }
}
