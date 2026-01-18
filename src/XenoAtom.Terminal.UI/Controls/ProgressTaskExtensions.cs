// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.Controls;

/// <summary>
/// Provides convenience APIs for configuring <see cref="ProgressTask"/> instances.
/// </summary>
public static partial class ProgressTaskExtensions
{
    private const string BarStyleCustomizationKey = "BarStyle";
    private const string SpinnerStyleCustomizationKey = "SpinnerStyle";

    /// <summary>
    /// Applies the specified <paramref name="style"/> to the bar cell created by <see cref="ProgressTaskColumns.Bar(ProgressBarStyle?, int)"/>.
    /// </summary>
    /// <param name="task">The task to configure.</param>
    /// <param name="style">The progress bar style to apply.</param>
    /// <returns>The same task for chaining.</returns>
    public static ProgressTask StyleBar(this ProgressTask task, ProgressBarStyle style)
    {
        ArgumentNullException.ThrowIfNull(task);
        ArgumentNullException.ThrowIfNull(style);

        return task.SetCellCustomization(
            ProgressTaskColumns.BarColumnId,
            BarStyleCustomizationKey,
            cell =>
            {
                if (cell is ProgressBar bar)
                {
                    bar.Style(style);
                }
            });
    }

    /// <summary>
    /// Applies the specified <paramref name="style"/> to the spinner cell created by <see cref="ProgressTaskColumns.Spinner(SpinnerStyle?)"/>.
    /// </summary>
    /// <param name="task">The task to configure.</param>
    /// <param name="style">The spinner style to apply.</param>
    /// <returns>The same task for chaining.</returns>
    public static ProgressTask StyleSpinner(this ProgressTask task, SpinnerStyle style)
    {
        ArgumentNullException.ThrowIfNull(task);
        ArgumentNullException.ThrowIfNull(style);

        return task.SetCellCustomization(
            ProgressTaskColumns.SpinnerColumnId,
            SpinnerStyleCustomizationKey,
            cell =>
            {
                if (cell is Spinner spinner)
                {
                    spinner.Style(style);
                }
            });
    }
}

