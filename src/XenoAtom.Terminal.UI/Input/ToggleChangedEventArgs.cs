// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI.Input;

/// <summary>
/// Provides data for toggle state change events.
/// </summary>
public sealed class ToggleChangedEventArgs : RoutedEventArgs
{
    /// <summary>
    /// Gets the previous value.
    /// </summary>
    public bool OldValue { get; init; }

    /// <summary>
    /// Gets the new value.
    /// </summary>
    public bool NewValue { get; init; }
}
