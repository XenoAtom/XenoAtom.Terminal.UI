// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI;

/// <summary>
/// Specifies how a visual is aligned horizontally / vertically within its parent.
/// </summary>
public enum Align
{
    /// <summary>
    /// Align to the left / top.
    /// </summary>
    Start = 0,
    /// <summary>
    /// Center horizontally / vertically.
    /// </summary>
    Center = 1,
    /// <summary>
    /// Align to the right / bottom.
    /// </summary>
    End = 2,
    /// <summary>
    /// Stretch to fill the available width / height.
    /// </summary>
    Stretch = 3,
}
