// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI.Input;

/// <summary>
/// Provides data for breakdown segment click events.
/// </summary>
public sealed class BreakdownSegmentClickedEventArgs : RoutedEventArgs
{
    /// <summary>
    /// Gets the clicked segment index.
    /// </summary>
    public required int Index { get; init; }

    /// <summary>
    /// Gets the clicked segment.
    /// </summary>
    public required Controls.BreakdownSegment Segment { get; init; }
}

