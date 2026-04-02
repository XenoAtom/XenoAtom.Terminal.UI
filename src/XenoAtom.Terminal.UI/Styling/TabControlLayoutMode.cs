// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI.Styling;

/// <summary>
/// Selects how a <see cref="Controls.TabControl"/> renders its header strip and content chrome.
/// </summary>
public enum TabControlLayoutMode
{
    /// <summary>
    /// Renders a compact, flat header strip above the content region.
    /// </summary>
    Compact,

    /// <summary>
    /// Renders outlined tabs attached to the content frame.
    /// </summary>
    Attached,
}
