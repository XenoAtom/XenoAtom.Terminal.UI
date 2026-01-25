// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI.Commands;

/// <summary>
/// Defines importance buckets for ordering commands in UI surfaces.
/// </summary>
public enum CommandImportance
{
    /// <summary>
    /// A primary command users are expected to discover quickly.
    /// </summary>
    Primary,

    /// <summary>
    /// A common command that should be discoverable without dominating the UI.
    /// </summary>
    Secondary,

    /// <summary>
    /// A command that is rarely needed or mostly contextual.
    /// </summary>
    Tertiary,
}