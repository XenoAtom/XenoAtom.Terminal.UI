// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI.Controls;

/// <summary>
/// Provides data for <see cref="Select{T}"/> selection changed events.
/// </summary>
public sealed class SelectSelectionChangedEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SelectSelectionChangedEventArgs"/> class.
    /// </summary>
    /// <param name="oldIndex">The previous selected index.</param>
    /// <param name="newIndex">The current selected index.</param>
    public SelectSelectionChangedEventArgs(int oldIndex, int newIndex)
    {
        OldIndex = oldIndex;
        NewIndex = newIndex;
    }

    /// <summary>
    /// Gets the previous selected index.
    /// </summary>
    public int OldIndex { get; }

    /// <summary>
    /// Gets the current selected index.
    /// </summary>
    public int NewIndex { get; }
}

