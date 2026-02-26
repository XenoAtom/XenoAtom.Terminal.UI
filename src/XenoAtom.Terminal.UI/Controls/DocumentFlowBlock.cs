// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI.Controls;

/// <summary>
/// Represents a block descriptor used by <see cref="DocumentFlow"/> to create and recycle visuals.
/// </summary>
public abstract class DocumentFlowBlock
{
    /// <summary>
    /// Gets a monotonically increasing version for this block.
    /// </summary>
    public virtual int Version => 0;

    /// <summary>
    /// Gets optional top margin (in rows) before this block.
    /// </summary>
    public virtual int MarginTop => 0;

    /// <summary>
    /// Gets optional bottom margin (in rows) after this block.
    /// </summary>
    public virtual int MarginBottom => 0;

    /// <summary>
    /// Gets a reuse key for recycling visuals.
    /// </summary>
    public virtual object? ReuseKey => this;

    /// <summary>
    /// Creates a visual instance for this block.
    /// </summary>
    /// <returns>A visual instance.</returns>
    public abstract Visual CreateVisual();

    /// <summary>
    /// Tries to update a recycled visual so it can represent this block.
    /// </summary>
    /// <param name="visual">The recycled visual.</param>
    /// <returns><see langword="true"/> if the visual was updated and can be reused; otherwise <see langword="false"/>.</returns>
    public virtual bool TryUpdate(Visual visual)
    {
        _ = visual;
        return true;
    }

    /// <summary>
    /// Called when a visual is returned to a recycle pool.
    /// </summary>
    /// <param name="visual">The visual being released.</param>
    public virtual void Release(Visual visual)
    {
        _ = visual;
    }
}
