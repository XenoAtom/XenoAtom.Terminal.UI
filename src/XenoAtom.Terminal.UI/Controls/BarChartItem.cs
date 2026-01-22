// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Threading;

namespace XenoAtom.Terminal.UI.Controls;

/// <summary>
/// Represents a single row item displayed by <see cref="BarChart"/>.
/// </summary>
/// <remarks>
/// This type is primarily a state container and is not itself a <see cref="Visual"/>.
/// </remarks>
public sealed partial class BarChartItem : DispatcherObject, IVisualElement
{
    private BarChart? _owner;

    /// <summary>
    /// Initializes a new instance of the <see cref="BarChartItem"/> class.
    /// </summary>
    public BarChartItem()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BarChartItem"/> class.
    /// </summary>
    /// <param name="label">The item label visual.</param>
    /// <param name="value">The item numeric value.</param>
    public BarChartItem(Visual? label, double value)
    {
        Label = label;
        Value = value;
    }

    TerminalApp? IVisualElement.App => _owner?.App;

    /// <summary>
    /// Gets or sets the label visual displayed for the row.
    /// </summary>
    [Bindable]
    public partial Visual? Label { get; set; }

    /// <summary>
    /// Gets or sets the numeric value displayed by the row.
    /// </summary>
    [Bindable]
    public partial double Value { get; set; }

    /// <summary>
    /// Gets or sets an optional explicit value label visual.
    /// </summary>
    [Bindable]
    public partial Visual? ValueLabel { get; set; }

    /// <summary>
    /// Gets or sets an optional per-item bar color.
    /// </summary>
    [Bindable]
    public partial Color? BarColor { get; set; }

    internal void Attach(BarChart owner) => _owner = owner;

    internal void Detach(BarChart owner)
    {
        if (ReferenceEquals(_owner, owner))
        {
            _owner = null;
        }
    }
}

