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
    None = 0,
    Bold = 1 << 0,
    Dim = 1 << 1,
    Italic = 1 << 2,
    Underline = 1 << 3,
    Blink = 1 << 4,
    Invert = 1 << 5,
    Hidden = 1 << 6,
    Strikethrough = 1 << 7,
}

