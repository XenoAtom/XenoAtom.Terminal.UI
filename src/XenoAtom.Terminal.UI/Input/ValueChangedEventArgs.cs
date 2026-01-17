// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI.Input;

/// <summary>
/// Provides data for value change events.
/// </summary>
public sealed class ValueChangedEventArgs<T> : RoutedEventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ValueChangedEventArgs{T}"/> class.
    /// </summary>
    /// <param name="oldValue">The previous value.</param>
    /// <param name="newValue">The new value.</param>
    public ValueChangedEventArgs(T oldValue, T newValue)
    {
        OldValue = oldValue;
        NewValue = newValue;
    }

    /// <summary>
    /// Gets the previous value.
    /// </summary>
    public T OldValue { get; }

    /// <summary>
    /// Gets the new value.
    /// </summary>
    public T NewValue { get; }
}
