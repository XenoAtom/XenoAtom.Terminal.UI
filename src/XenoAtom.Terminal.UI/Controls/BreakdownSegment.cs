// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Threading;

namespace XenoAtom.Terminal.UI.Controls;

/// <summary>
/// Represents a segment displayed by <see cref="BreakdownChart"/>.
/// </summary>
/// <remarks>
/// This type is a state container and is not itself a <see cref="Visual"/>. It participates in binding tracking once
/// it is attached to a breakdown through the segments collection.
/// </remarks>
public sealed partial class BreakdownSegment : DispatcherObject, IVisualElement
{
    private BreakdownChart? _owner;

    /// <summary>
    /// Initializes a new instance of the <see cref="BreakdownSegment"/> class.
    /// </summary>
    public BreakdownSegment()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BreakdownSegment"/> class.
    /// </summary>
    /// <param name="value">The segment value.</param>
    /// <param name="label">The segment label visual.</param>
    public BreakdownSegment(double value, Visual? label = null)
    {
        Value = value;
        Label = label;
    }

    TerminalApp? IVisualElement.App => _owner?.App;

    /// <summary>
    /// Gets or sets the segment value.
    /// </summary>
    [Bindable]
    public partial double Value { get; set; }

    /// <summary>
    /// Gets or sets the segment label visual.
    /// </summary>
    [Bindable]
    public partial Visual? Label { get; set; }

    /// <summary>
    /// Gets or sets an optional segment color.
    /// </summary>
    [Bindable]
    public partial Color? Color { get; set; }

    /// <summary>
    /// Gets or sets an optional tooltip visual displayed when the segment is hovered.
    /// </summary>
    [Bindable]
    public partial Visual? Tooltip { get; set; }

    internal void Attach(BreakdownChart owner)
    {
        _owner = owner;
    }

    internal void Detach(BreakdownChart owner)
    {
        if (ReferenceEquals(_owner, owner))
        {
            _owner = null;
        }
    }
}
