// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI.Input;

/// <summary>
/// Indicates why a popup was closed.
/// </summary>
public enum PopupCloseReason
{
    /// <summary>
    /// The popup was closed programmatically.
    /// </summary>
    Programmatic,

    /// <summary>
    /// The popup was closed because the user clicked outside its content rectangle.
    /// </summary>
    OutsidePointerPress,
}

/// <summary>
/// Provides data for popup closed events.
/// </summary>
public sealed class PopupClosedEventArgs : RoutedEventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PopupClosedEventArgs"/> class.
    /// </summary>
    /// <param name="reason">The reason why the popup was closed.</param>
    /// <param name="outsidePointerX">The pointer X coordinate when closed by an outside click.</param>
    /// <param name="outsidePointerY">The pointer Y coordinate when closed by an outside click.</param>
    public PopupClosedEventArgs(PopupCloseReason reason = PopupCloseReason.Programmatic, int? outsidePointerX = null, int? outsidePointerY = null)
    {
        Reason = reason;
        OutsidePointerX = outsidePointerX;
        OutsidePointerY = outsidePointerY;
    }

    /// <summary>
    /// Gets the reason why the popup was closed.
    /// </summary>
    public PopupCloseReason Reason { get; }

    /// <summary>
    /// Gets the pointer X coordinate when <see cref="Reason"/> is <see cref="PopupCloseReason.OutsidePointerPress"/>.
    /// </summary>
    public int? OutsidePointerX { get; }

    /// <summary>
    /// Gets the pointer Y coordinate when <see cref="Reason"/> is <see cref="PopupCloseReason.OutsidePointerPress"/>.
    /// </summary>
    public int? OutsidePointerY { get; }
}
