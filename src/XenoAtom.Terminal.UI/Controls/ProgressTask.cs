// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Collections;
using XenoAtom.Terminal.UI.Threading;

namespace XenoAtom.Terminal.UI.Controls;

/// <summary>
/// Represents a single progress task displayed by <see cref="ProgressTaskGroup"/>.
/// </summary>
/// <remarks>
/// This type is primarily a state container (value/range) and is not part of the visual tree.
/// The task can expose a visual label (for rich composition) that is hosted by a group column.
/// </remarks>
public partial class ProgressTask : DispatcherObject, IVisualElement
{
    private readonly Visual _label;
    private readonly BindableList<ProgressTaskCellCustomization> _cellCustomizations;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProgressTask"/> class.
    /// </summary>
    /// <param name="label">The task label visual.</param>
    public ProgressTask(Visual label)
    {
        ArgumentNullException.ThrowIfNull(label);
        _label = label;
        _cellCustomizations = new BindableList<ProgressTaskCellCustomization>(this, $"{nameof(ProgressTask)}.CellCustomizations");

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
        _cellCustomizations = new BindableList<ProgressTaskCellCustomization>(this, $"{nameof(ProgressTask)}.CellCustomizations");

        Maximum = 1.0;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProgressTask"/> class.
    /// </summary>
    /// <param name="label">A binding that supplies the task label visual.</param>
    public ProgressTask(Binding<Visual> label)
    {
        _label = new ComputedVisual(() => label.GetValue());
        _cellCustomizations = new BindableList<ProgressTaskCellCustomization>(this, $"{nameof(ProgressTask)}.CellCustomizations");

        Maximum = 1.0;
    }

    /// <summary>
    /// Gets the task label visual.
    /// </summary>
    public Visual Label => _label;

    /// <summary>
    /// Required to make sure that bindings in this class don't participate during the creation of this task, but only when it is added to a visual tree.
    /// </summary>
    TerminalApp? IVisualElement.App => _label.App;

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

    /// <summary>
    /// Adds a customization applied to the cell visual of a specific column.
    /// </summary>
    /// <param name="columnId">The target column identifier (see <see cref="ProgressTaskColumns"/> for built-in column ids).</param>
    /// <param name="customize">The customization to apply to the created cell visual.</param>
    /// <returns>The same instance for chaining.</returns>
    public ProgressTask CustomizeCell(string columnId, Action<Visual> customize)
    {
        ArgumentException.ThrowIfNullOrEmpty(columnId);
        ArgumentNullException.ThrowIfNull(customize);

        _cellCustomizations.Add(new ProgressTaskCellCustomization(columnId, Key: null, customize));
        return this;
    }

    /// <summary>
    /// Sets a keyed customization applied to the cell visual of a specific column.
    /// </summary>
    /// <param name="columnId">The target column identifier (see <see cref="ProgressTaskColumns"/> for built-in column ids).</param>
    /// <param name="key">A stable key used to replace a previous customization for the same column.</param>
    /// <param name="customize">The customization to apply to the created cell visual.</param>
    /// <returns>The same instance for chaining.</returns>
    public ProgressTask SetCellCustomization(string columnId, string key, Action<Visual> customize)
    {
        ArgumentException.ThrowIfNullOrEmpty(columnId);
        ArgumentException.ThrowIfNullOrEmpty(key);
        ArgumentNullException.ThrowIfNull(customize);

        var customizations = _cellCustomizations;
        for (var i = 0; i < customizations.Count; i++)
        {
            var existing = customizations[i];
            if (existing.ColumnId == columnId && existing.Key == key)
            {
                customizations[i] = new ProgressTaskCellCustomization(columnId, key, customize);
                return this;
            }
        }

        customizations.Add(new ProgressTaskCellCustomization(columnId, key, customize));
        return this;
    }

    /// <summary>
    /// Clears all customizations associated with a specific column.
    /// </summary>
    /// <param name="columnId">The target column identifier.</param>
    /// <returns>The same instance for chaining.</returns>
    public ProgressTask ClearCellCustomizations(string columnId)
    {
        ArgumentException.ThrowIfNullOrEmpty(columnId);

        var customizations = _cellCustomizations;
        for (var i = customizations.Count - 1; i >= 0; i--)
        {
            if (customizations[i].ColumnId == columnId)
            {
                customizations.RemoveAt(i);
            }
        }

        return this;
    }

    /// <summary>
    /// Called by <see cref="ProgressTaskGroup"/> when a cell visual is created for this task.
    /// </summary>
    /// <param name="columnId">The column identifier.</param>
    /// <param name="cell">The created cell visual.</param>
    /// <remarks>
    /// Override this method to apply per-task customization without modifying the group column definitions.
    /// The default implementation applies registered entries added via <see cref="CustomizeCell(string,Action{Visual})"/>
    /// and <see cref="SetCellCustomization(string,string,Action{Visual})"/>.
    /// </remarks>
    protected internal virtual void OnCellCreated(string columnId, Visual cell)
    {
        ArgumentException.ThrowIfNullOrEmpty(columnId);
        ArgumentNullException.ThrowIfNull(cell);

        var customizations = _cellCustomizations;
        for (var i = 0; i < customizations.Count; i++)
        {
            var customization = customizations[i];
            if (customization.ColumnId == columnId)
            {
                customization.Apply(cell);
            }
        }
    }

    /// <summary>
    /// Stores per-task customizations applied to a specific column cell visual.
    /// </summary>
    /// <remarks>
    /// The <see cref="ProgressTaskGroup"/> creates cell visuals from columns, then applies matching customizations from
    /// the task before the visuals are attached to the visual tree.
    /// </remarks>
    private sealed record ProgressTaskCellCustomization(string ColumnId, string? Key, Action<Visual> Apply);
}
