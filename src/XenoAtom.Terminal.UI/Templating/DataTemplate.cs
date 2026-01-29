// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI;

namespace XenoAtom.Terminal.UI.Templating;

/// <summary>
/// Provides read access to a value being displayed by a data template.
/// </summary>
/// <typeparam name="T">The data type.</typeparam>
public readonly record struct DataTemplateValue<T>
{
    private readonly Binding<T> _binding;
    private readonly T _value;

    /// <summary>
    /// Initializes a new instance of the <see cref="DataTemplateValue{T}"/> struct from a bindable value.
    /// </summary>
    /// <param name="binding">The bindable value reference.</param>
    public DataTemplateValue(Binding<T> binding)
    {
        _binding = binding;
        _value = default!;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DataTemplateValue{T}"/> struct from a non-bindable value.
    /// </summary>
    /// <param name="value">The value.</param>
    public DataTemplateValue(T value)
    {
        _binding = default;
        _value = value;
    }

    /// <summary>
    /// Gets the current value.
    /// </summary>
    /// <remarks>
    /// When the value is backed by a bindable source, this read participates in dependency tracking.
    /// </remarks>
    public T GetValue() => _binding.IsEmpty ? _value : _binding.GetValue();
}

/// <summary>
/// A factory that creates a visual subtree for displaying a data value.
/// </summary>
/// <typeparam name="T">The data type.</typeparam>
/// <param name="value">A value reference used for display (bindable or non-bindable).</param>
/// <param name="context">Additional templating context.</param>
/// <returns>A visual representing the current value.</returns>
public delegate Visual DataTemplateDisplayFactory<T>(DataTemplateValue<T> value, in DataTemplateContext context);

/// <summary>
/// A factory that creates a visual subtree for editing a bindable data value.
/// </summary>
/// <typeparam name="T">The data type.</typeparam>
/// <param name="binding">A binding that provides read/write access to the value.</param>
/// <param name="context">Additional templating context.</param>
/// <returns>A visual representing the current value.</returns>
public delegate Visual DataTemplateEditorFactory<T>(Binding<T> binding, in DataTemplateContext context);

/// <summary>
/// Attempts to update an existing visual instance to represent the current value of the provided binding.
/// </summary>
/// <typeparam name="T">The data type.</typeparam>
/// <param name="visual">The visual to update.</param>
/// <param name="value">A value reference used for display (bindable or non-bindable).</param>
/// <param name="context">Additional templating context.</param>
/// <returns><see langword="true"/> if the visual was updated and can be reused; otherwise <see langword="false"/>.</returns>
public delegate bool DataTemplateUpdater<T>(Visual visual, DataTemplateValue<T> value, in DataTemplateContext context);

/// <summary>
/// Releases a visual that will no longer be reused by a recycling pool.
/// </summary>
/// <param name="visual">The visual being released.</param>
public delegate void DataTemplateReleaser(Visual visual);

/// <summary>
/// Describes how to create a visual for a data value, and optionally how to update/release visuals for recycling scenarios.
/// </summary>
/// <typeparam name="T">The data type.</typeparam>
public readonly record struct DataTemplate<T>(
    DataTemplateDisplayFactory<T>? Display,
    DataTemplateEditorFactory<T>? Editor,
    DataTemplateUpdater<T>? TryUpdate = null,
    DataTemplateReleaser? Release = null)
{
    /// <summary>
    /// Gets a value indicating whether this template is empty.
    /// </summary>
    public bool IsEmpty => Display is null && Editor is null;

    /// <summary>
    /// Gets a value indicating whether this template provides a display factory.
    /// </summary>
    public bool HasDisplay => Display is not null;

    /// <summary>
    /// Gets a value indicating whether this template provides an editor factory.
    /// </summary>
    public bool HasEditor => Editor is not null;
}
