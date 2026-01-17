// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI.Controls;

/// <summary>
/// Provides fluent helpers for <see cref="LineChart"/>.
/// </summary>
public static partial class LineChartExtensions
{
    /// <summary>
    /// Sets the chart values.
    /// </summary>
    /// <param name="lineChart">The chart.</param>
    /// <param name="values">The values to set.</param>
    /// <returns>The chart instance.</returns>
    public static LineChart Values(this LineChart lineChart, IEnumerable<double> values)
    {
        ArgumentNullException.ThrowIfNull(lineChart);
        ArgumentNullException.ThrowIfNull(values);
        lineChart.Values.Clear();
        lineChart.Values?.AddRange(values);
        return lineChart;
    }
}
