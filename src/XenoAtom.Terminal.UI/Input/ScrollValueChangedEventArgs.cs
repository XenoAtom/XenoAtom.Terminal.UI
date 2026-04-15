// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI.Input;

/// <summary>
/// Provides data for scroll value change events.
/// </summary>
public sealed class ScrollValueChangedEventArgs : RoutedEventArgs
{
    /// <summary>
    /// Gets the scroll orientation whose value changed.
    /// </summary>
    public Orientation Orientation { get; init; }

    /// <summary>
    /// Gets the previous value.
    /// </summary>
    public int OldValue { get; init; }

    /// <summary>
    /// Gets the new value.
    /// </summary>
    public int NewValue { get; init; }

    /// <summary>
    /// Gets a value indicating whether the change was caused by user interaction.
    /// </summary>
    public bool IsUserInitiated { get; init; }
}
