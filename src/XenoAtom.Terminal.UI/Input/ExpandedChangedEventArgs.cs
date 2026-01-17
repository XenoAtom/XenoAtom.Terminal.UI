// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI.Input;

/// <summary>
/// Provides data for expanded state change events.
/// </summary>
public sealed class ExpandedChangedEventArgs : RoutedEventArgs
{
    /// <summary>
    /// Gets the previous expanded state.
    /// </summary>
    public bool OldValue { get; init; }

    /// <summary>
    /// Gets the new expanded state.
    /// </summary>
    public bool NewValue { get; init; }
}
