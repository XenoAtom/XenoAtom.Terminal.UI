// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI.Input;

/// <summary>
/// Provides data for selection change events.
/// </summary>
public sealed class SelectionChangedEventArgs : RoutedEventArgs
{
    /// <summary>
    /// Gets the previously selected index.
    /// </summary>
    public int OldIndex { get; init; }

    /// <summary>
    /// Gets the newly selected index.
    /// </summary>
    public int NewIndex { get; init; }
}
