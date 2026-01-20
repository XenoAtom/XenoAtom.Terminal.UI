// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI.Templating;

/// <summary>
/// Describes the interaction state of a templated item.
/// </summary>
[Flags]
public enum DataTemplateItemState
{
    /// <summary>
    /// No specific state.
    /// </summary>
    None = 0,

    /// <summary>
    /// The item is selected.
    /// </summary>
    Selected = 1 << 0,

    /// <summary>
    /// The pointer is over the item.
    /// </summary>
    Hovered = 1 << 1,

    /// <summary>
    /// The item (or its container) has focus.
    /// </summary>
    Focused = 1 << 2,

    /// <summary>
    /// The item is disabled.
    /// </summary>
    Disabled = 1 << 3,
}

