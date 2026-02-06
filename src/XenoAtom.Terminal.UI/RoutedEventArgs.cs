// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI;

/// <summary>
/// Identifies the current routing phase for a routed event.
/// </summary>
public enum RoutingPhase
{
    /// <summary>
    /// The phase is not set.
    /// </summary>
    None = 0,

    /// <summary>
    /// Direct routing; the event is dispatched only on the target.
    /// </summary>
    Direct,

    /// <summary>
    /// Preview routing from root to target.
    /// </summary>
    Preview,

    /// <summary>
    /// Bubble routing from target to root.
    /// </summary>
    Bubble,
}

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

    /// <summary>
    /// Gets the current routing phase while the event is being dispatched.
    /// </summary>
    public RoutingPhase RoutingPhase { get; internal set; }
}
