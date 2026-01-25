// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI.Commands;

/// <summary>
/// Flags describing where a command should be presented.
/// </summary>
[Flags]
public enum CommandPresentation
{
    /// <summary>
    /// The command should not be automatically presented.
    /// </summary>
    None = 0,

    /// <summary>
    /// Present the command in a command bar UI surface.
    /// </summary>
    CommandBar = 1,

    /// <summary>
    /// Present the command in a command palette.
    /// </summary>
    CommandPalette = 2,

    /// <summary>
    /// Present the command in menus.
    /// </summary>
    Menu = 4,

    /// <summary>
    /// Present the command in context menus.
    /// </summary>
    ContextMenu = 8,
}