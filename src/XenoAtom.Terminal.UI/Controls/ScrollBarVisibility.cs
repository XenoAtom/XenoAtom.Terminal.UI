// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI.Controls;

/// <summary>
/// Specifies when a scroll bar is displayed by a <see cref="ScrollViewer"/>.
/// </summary>
public enum ScrollBarVisibility
{
    /// <summary>
    /// Displays the scroll bar only when the content exceeds the viewport.
    /// </summary>
    Auto,

    /// <summary>
    /// Hides the scroll bar while preserving scrolling on the corresponding axis.
    /// </summary>
    Hidden,

    /// <summary>
    /// Always displays the scroll bar and reserves its space in the content viewport.
    /// </summary>
    Always,
}
