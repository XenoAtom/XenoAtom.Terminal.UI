// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI.Controls;

public enum GridUnitType
{
    Auto = 0,
    Fixed = 1,
    Star = 2,
}

public readonly record struct GridLength(GridUnitType Type, double Value)
{
    public static GridLength Auto => new(GridUnitType.Auto, 0);

    public static GridLength Fixed(int cells) => new(GridUnitType.Fixed, Math.Max(0, cells));

    public static GridLength Star(double weight = 1) => new(GridUnitType.Star, weight <= 0 ? 1 : weight);

    public override string ToString()
        => Type switch
        {
            GridUnitType.Auto => "Auto",
            GridUnitType.Fixed => Value.ToString(System.Globalization.CultureInfo.InvariantCulture),
            GridUnitType.Star => Value.ToString(System.Globalization.CultureInfo.InvariantCulture) + "*",
            _ => base.ToString() ?? string.Empty,
        };
}

public sealed record RowDefinition
{
    public GridLength Height { get; init; } = GridLength.Star(1);

    public int MinHeight { get; init; }

    public int MaxHeight { get; init; } = int.MaxValue;
}

public sealed record ColumnDefinition
{
    public GridLength Width { get; init; } = GridLength.Star(1);

    public int MinWidth { get; init; }

    public int MaxWidth { get; init; } = int.MaxValue;
}

