// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI;

/// <summary>
/// Base type for routed event args.
/// </summary>
public abstract class RoutedEventArgs : EventArgs
{
    /// <summary>
    /// Gets or sets a value indicating whether the event has been handled.
    /// </summary>
    public bool Handled { get; set; }

    /// <summary>
    /// Gets the original source of the routed event.
    /// </summary>
    public Visual? OriginalSource { get; internal set; }

    /// <summary>
    /// Gets the current source during routing.
    /// </summary>
    public Visual? Source { get; internal set; }
}
