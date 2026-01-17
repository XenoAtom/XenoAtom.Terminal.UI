// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI.Scrolling;

/// <summary>
/// Exposes scroll state for scrollable visuals.
/// </summary>
public interface IScrollable
{
    /// <summary>
    /// Gets the scroll model for the visual.
    /// </summary>
    ScrollModel Scroll { get; }
}
