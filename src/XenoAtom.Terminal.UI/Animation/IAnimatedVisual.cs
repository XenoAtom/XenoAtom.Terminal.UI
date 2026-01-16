// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI.Animation;

/// <summary>
/// Represents a visual that can advance an animation over time.
/// </summary>
public interface IAnimatedVisual
{
    /// <summary>
    /// Gets the timestamp (in ticks) when the next animation update is desired.
    /// </summary>
    long NextAnimationTick { get; }

    /// <summary>
    /// Advances the animation to the specified timestamp.
    /// </summary>
    /// <param name="timestamp">The current timestamp, in ticks.</param>
    /// <returns><c>true</c> if the visual state changed and needs to be re-rendered; otherwise <c>false</c>.</returns>
    bool AdvanceAnimation(long timestamp);
}
