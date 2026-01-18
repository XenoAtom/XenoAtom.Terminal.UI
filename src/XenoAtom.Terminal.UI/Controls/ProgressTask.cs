// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Threading;

namespace XenoAtom.Terminal.UI.Controls;

/// <summary>
/// Represents a single progress task displayed by <see cref="ProgressTaskGroup"/>.
/// </summary>
/// <remarks>
/// This type is primarily a state container (value/range) and is not part of the visual tree.
/// The task can expose a visual label (for rich composition) that is hosted by a group column.
/// </remarks>
public partial class ProgressTask : DispatcherObject
{
    private readonly Visual _label;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProgressTask"/> class.
    /// </summary>
    /// <param name="label">The task label visual.</param>
    public ProgressTask(Visual label)
    {
        ArgumentNullException.ThrowIfNull(label);
        _label = label;

        Maximum = 1.0;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProgressTask"/> class.
    /// </summary>
    /// <param name="label">The task label visual.</param>
    public ProgressTask(Func<Visual> label)
    {
        ArgumentNullException.ThrowIfNull(label);
        _label = new ComputedVisual(label);

        Maximum = 1.0;
    }

    /// <summary>
    /// Gets the task label visual.
    /// </summary>
    public Visual Label => _label;

    /// <summary>
    /// Gets or sets the current progress value.
    /// </summary>
    [Bindable]
    public partial double Value { get; set; }

    /// <summary>
    /// Gets or sets the minimum value for <see cref="Value"/>.
    /// </summary>
    [Bindable]
    public partial double Minimum { get; set; }

    /// <summary>
    /// Gets or sets the maximum value for <see cref="Value"/>.
    /// </summary>
    [Bindable]
    public partial double Maximum { get; set; }

    /// <summary>
    /// Gets a normalized progress value in the range [0..1].
    /// </summary>
    public double Progress01
    {
        get
        {
            var min = Minimum;
            var max = Maximum;
            var value = Value;

            var range = max - min;
            if (range <= 0)
            {
                return 0.0;
            }

            return Math.Clamp((value - min) / range, 0.0, 1.0);
        }
    }

    /// <summary>
    /// Gets the progress percentage in the range [0..100].
    /// </summary>
    public int Percentage
    {
        get
        {
            var pct = (int)Math.Round(Progress01 * 100.0);
            return Math.Clamp(pct, 0, 100);
        }
    }

    /// <summary>
    /// Increments <see cref="Value"/> by the specified <paramref name="delta"/>.
    /// </summary>
    /// <param name="delta">The delta to add.</param>
    public void Increment(double delta) => Value += delta;
}
