// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI;

/// <summary>
/// Specifies how text is aligned within an available width.
/// </summary>
public enum TextAlignment
{
    /// <summary>Left aligned.</summary>
    Left = 0,
    /// <summary>Centered.</summary>
    Center = 1,
    /// <summary>Right aligned.</summary>
    Right = 2,
    /// <summary>Justified (when supported by the renderer).</summary>
    Justify = 3,
}
