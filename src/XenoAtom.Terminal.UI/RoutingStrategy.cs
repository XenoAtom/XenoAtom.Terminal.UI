// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI;

/// <summary>
/// Specifies how a routed event travels through the visual tree.
/// </summary>
[Flags]
public enum RoutingStrategy
{
    /// <summary>
    /// Routes the event directly to the source.
    /// </summary>
    Direct = 0,
    /// <summary>
    /// Routes the event from the source up to the root.
    /// </summary>
    Bubble = 1,
    /// <summary>
    /// Routes the event from the root down to the source.
    /// </summary>
    Preview = 2,
}
