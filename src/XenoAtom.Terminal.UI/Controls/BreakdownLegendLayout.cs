// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI.Controls;

/// <summary>
/// Specifies how a <see cref="BreakdownChart"/> legend lays out its items.
/// </summary>
public enum BreakdownLegendLayout
{
    /// <summary>
    /// Packs legend items into as few rows as possible (wraps when needed).
    /// </summary>
    Compact,

    /// <summary>
    /// Renders one legend item per line.
    /// </summary>
    Expanded,
}

