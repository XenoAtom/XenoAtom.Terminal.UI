// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI.Controls;

/// <summary>
/// Specifies how items are justified along the main axis within each run of a wrapping stack.
/// </summary>
public enum WrapJustify
{
    /// <summary>
    /// Aligns items to the start of the run.
    /// </summary>
    Start,

    /// <summary>
    /// Centers items within the run.
    /// </summary>
    Center,

    /// <summary>
    /// Aligns items to the end of the run.
    /// </summary>
    End,

    /// <summary>
    /// Distributes remaining space between items. The start and end edges receive no extra space.
    /// </summary>
    SpaceBetween,

    /// <summary>
    /// Distributes remaining space around items. The start and end edges receive half of the space between items.
    /// </summary>
    SpaceAround,

    /// <summary>
    /// Distributes remaining space evenly, including the start and end edges.
    /// </summary>
    SpaceEvenly,
}

/// <summary>
/// Specifies how children are measured along the main axis of a wrapping stack.
/// </summary>
public enum WrapMeasureMode
{
    /// <summary>
    /// Measures children with the current run constraint on the main axis.
    /// </summary>
    /// <remarks>
    /// This mode is preferred for text-based visuals where height depends on the available width.
    /// </remarks>
    ConstrainToRun,

    /// <summary>
    /// Measures children with an unbounded main axis.
    /// </summary>
    /// <remarks>
    /// This mode is useful when items should keep their intrinsic width (e.g. a legend), and overflow is expected to be clipped.
    /// </remarks>
    Unconstrained,
}

