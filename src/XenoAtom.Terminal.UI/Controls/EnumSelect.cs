// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Templating;

namespace XenoAtom.Terminal.UI.Controls;

/// <summary>
/// A specialized <see cref="Select{T}"/> for enum values.
/// </summary>
/// <remarks>
/// <para>
/// This control populates <see cref="Select{T}.Items"/> from <see cref="Enum.GetValues{TEnum}"/> and exposes a
/// bindable <see cref="Value"/> property that maps to <see cref="Select{T}.SelectedIndex"/>.
/// </para>
/// <para>
/// The default item presentation uses the same template resolution as <see cref="Select{T}"/>:
/// <see cref="Select{T}.ItemTemplate"/> when provided, otherwise an environment-scoped <see cref="DataTemplates"/>
/// display template, otherwise a <see cref="TextBlock"/> displaying <c>value.ToString()</c>.
/// </para>
/// </remarks>
/// <typeparam name="TEnum">The enum type.</typeparam>
public sealed partial class EnumSelect<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] TEnum> : Select<TEnum>
    where TEnum : struct, Enum
{
    private readonly TEnum[] _values;
    private bool _syncingFromValue;

    /// <summary>
    /// Initializes a new instance of the <see cref="EnumSelect{TEnum}"/> class.
    /// </summary>
    public EnumSelect()
    {
        _values = Enum.GetValues<TEnum>();
        for (var i = 0; i < _values.Length; i++)
        {
            Items.Add(_values[i]);
        }

        if (_values.Length > 0)
        {
            // Initialize to a valid enum value (first declared value), rather than default(TEnum) which can be undefined.
            Value = _values[0];
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EnumSelect{TEnum}"/> class with an initial value.
    /// </summary>
    /// <param name="value">The initial enum value.</param>
    public EnumSelect(TEnum value) : this()
    {
        this.Value(value);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EnumSelect{TEnum}"/> class bound to a value binding.
    /// </summary>
    /// <param name="value">A binding that supplies the selected enum value.</param>
    public EnumSelect(Binding<TEnum> value) : this()
    {
        this.BindValue(value);
    }

    /// <inheritdoc />
    protected override Layout.SizeHints MeasureCore(in Layout.LayoutConstraints constraints)
    {
        return base.MeasureCore(constraints);
    }

    /// <inheritdoc />
    protected override void ArrangeCore(in Geometry.Rectangle finalRect)
    {
        base.ArrangeCore(finalRect);
    }

    /// <summary>
    /// Gets or sets the selected enum value.
    /// </summary>
    [Bindable]
    public partial TEnum Value { get; set; }

    partial void OnValueChanging(ref TEnum value)
    {
        // Ensure the value is valid for this enum (as exposed by Enum.GetValues). If not, fallback to the first value.
        if (_values.Length == 0)
        {
            return;
        }

        for (var i = 0; i < _values.Length; i++)
        {
            if (EqualityComparer<TEnum>.Default.Equals(_values[i], value))
            {
                return;
            }
        }

        value = _values[0];
    }

    partial void OnValueChanged(TEnum value)
    {
        SyncFromValue();
    }

    /// <inheritdoc />
    protected override void OnSelectionChanged(SelectSelectionChangedEventArgs e)
    {
        base.OnSelectionChanged(e);

        if (_syncingFromValue || _values.Length == 0)
        {
            return;
        }

        var index = e.NewIndex;
        if ((uint)index >= (uint)_values.Length)
        {
            return;
        }

        Value = _values[index];
    }

    /// <inheritdoc />
    protected override void PrepareChildren()
    {
        SyncFromValue();
    }

    private void SyncFromValue()
    {
        if (_syncingFromValue) return;

        if (_values.Length == 0)
        {
            return;
        }

        var value = Value;
        var index = FindIndex(value);
        if (index < 0)
        {
            index = 0;
        }

        _syncingFromValue = true;
        try
        {
            SelectedIndex = index;
        }
        finally
        {
            _syncingFromValue = false;
        }
    }

    private int FindIndex(TEnum value)
    {
        for (var i = 0; i < _values.Length; i++)
        {
            if (EqualityComparer<TEnum>.Default.Equals(_values[i], value))
            {
                return i;
            }
        }

        return -1;
    }
}
