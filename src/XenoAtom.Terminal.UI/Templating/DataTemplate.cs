// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI;

namespace XenoAtom.Terminal.UI.Templating;

/// <summary>
/// A factory that creates a visual subtree for a bindable data value.
/// </summary>
/// <typeparam name="T">The data type.</typeparam>
/// <param name="binding">A binding that provides read/write access to the value.</param>
/// <param name="context">Additional templating context.</param>
/// <returns>A visual representing the current value.</returns>
public delegate Visual DataTemplateFactory<T>(Binding<T> binding, in DataTemplateContext context);

/// <summary>
/// Attempts to update an existing visual instance to represent the current value of the provided binding.
/// </summary>
/// <typeparam name="T">The data type.</typeparam>
/// <param name="visual">The visual to update.</param>
/// <param name="binding">A binding that provides read/write access to the value.</param>
/// <param name="context">Additional templating context.</param>
/// <returns><see langword="true"/> if the visual was updated and can be reused; otherwise <see langword="false"/>.</returns>
public delegate bool DataTemplateUpdater<T>(Visual visual, Binding<T> binding, in DataTemplateContext context);

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
    DataTemplateFactory<T>? Create,
    DataTemplateUpdater<T>? TryUpdate = null,
    DataTemplateReleaser? Release = null)
{
    /// <summary>
    /// Gets a value indicating whether this template is empty.
    /// </summary>
    public bool IsEmpty => Create is null;
}
