// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI.Input;

/// <summary>
/// Provides data for numeric value change events.
/// </summary>
public sealed class ValueChangedEventArgs : RoutedEventArgs
{
    /// <summary>
    /// Gets the previous value.
    /// </summary>
    public double OldValue { get; init; }

    /// <summary>
    /// Gets the new value.
    /// </summary>
    public double NewValue { get; init; }
}
