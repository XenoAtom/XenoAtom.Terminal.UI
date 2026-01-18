// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.Controls;

/// <summary>
/// Provides predefined columns for <see cref="ProgressTaskGroup"/>.
/// </summary>
public static class ProgressTaskColumns
{
    /// <summary>
    /// Creates a column that displays <see cref="ProgressTask.Label"/>.
    /// </summary>
    public static ProgressTaskColumn Label(HorizontalAlignment alignment = HorizontalAlignment.Right)
        => new(task => task.Label.HorizontalAlignment(alignment)) { Id = "Label", Width = GridLength.Auto };

    /// <summary>
    /// Creates a column that displays a <see cref="Controls.ProgressBar"/> bound to <see cref="ProgressTask.Progress01"/>.
    /// </summary>
    /// <param name="style">The progress bar style to use.</param>
    /// <param name="minWidth">Minimum bar width in cells.</param>
    public static ProgressTaskColumn Bar(ProgressBarStyle? style = null, int minWidth = 10)
        => new(task => new ProgressBar()
            .Value(() => task.Progress01)
            .Style(style ?? ProgressBarStyle.Default)
            .HorizontalAlignment(HorizontalAlignment.Stretch))
        {
            Id = "Bar",
            Width = GridLength.Star(1),
            MinWidth = Math.Max(0, minWidth),
        };

    /// <summary>
    /// Creates a column that displays the progress percentage formatted as <c>100%</c>.
    /// </summary>
    public static ProgressTaskColumn Percentage()
        => new(static task => new TextBlock(() => $"{task.Percentage,3}%"))
        {
            Id = "Percentage",
            Width = GridLength.Auto,
            MinWidth = 4,
        };

    /// <summary>
    /// Creates a column that displays a spinner.
    /// </summary>
    /// <param name="style">The spinner style to use.</param>
    public static ProgressTaskColumn Spinner(SpinnerStyle? style = null)
        => new(_ => new Spinner().Style(style ?? SpinnerStyle.Default))
        {
            Id = "Spinner",
            Width = GridLength.Auto,
            MinWidth = 2,
        };
}
