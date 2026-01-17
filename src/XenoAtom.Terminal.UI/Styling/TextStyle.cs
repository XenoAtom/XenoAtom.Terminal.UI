// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI.Styling;

/// <summary>
/// Text decorations for UI rendering (maps to ANSI SGR decorations).
/// </summary>
[Flags]
public enum TextStyle : byte
{
    /// <summary>
    /// No text decorations.
    /// </summary>
    None = 0,
    /// <summary>
    /// Bold text.
    /// </summary>
    Bold = 1 << 0,
    /// <summary>
    /// Dim text.
    /// </summary>
    Dim = 1 << 1,
    /// <summary>
    /// Italic text.
    /// </summary>
    Italic = 1 << 2,
    /// <summary>
    /// Underlined text.
    /// </summary>
    Underline = 1 << 3,
    /// <summary>
    /// Blinking text.
    /// </summary>
    Blink = 1 << 4,
    /// <summary>
    /// Inverted foreground/background.
    /// </summary>
    Invert = 1 << 5,
    /// <summary>
    /// Hidden text.
    /// </summary>
    Hidden = 1 << 6,
    /// <summary>
    /// Strikethrough text.
    /// </summary>
    Strikethrough = 1 << 7,
}
