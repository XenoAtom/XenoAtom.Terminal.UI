// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI.Hosting;

/// <summary>
/// Specifies the hosting mode for a terminal UI.
/// </summary>
public enum TerminalHostKind
{
    /// <summary>
    /// Inline rendering within the terminal output stream.
    /// </summary>
    Inline,
    /// <summary>
    /// Fullscreen rendering that owns the terminal viewport.
    /// </summary>
    Fullscreen,
}
