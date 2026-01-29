// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI;

namespace XenoAtom.Terminal.UI.Scrolling;

/// <summary>
/// Represents scroll state for a scrollable surface (offset, viewport, and extent).
/// </summary>
/// <remarks>
/// This type is used by <see cref="XenoAtom.Terminal.UI.Controls.ScrollViewer"/> and by scrollable controls
/// (e.g. <c>TextArea</c>) that expose their own scroll model via <see cref="IScrollable"/>.
/// </remarks>
public sealed partial class ScrollModel : IVisualElement
{
    /// <summary>
    /// Initializes a new instance of the ScrollModel class with the specified visual owner.
    /// </summary>
    /// <param name="owner">The Visual instance that owns this ScrollModel. Cannot be null.</param>
    public ScrollModel(Visual owner)
    {
        ArgumentNullException.ThrowIfNull(owner);
        Owner = owner;
    }
    
    /// <summary>
    /// Gets the owner of this scroll model.
    /// </summary>
    public Visual Owner { get; }
    
    TerminalApp? IVisualElement.App => Owner.App;

    /// <summary>
    /// Gets the horizontal scroll offset (in cells).
    /// </summary>
    public int OffsetX { get; private set; }

    /// <summary>
    /// Gets the vertical scroll offset (in rows).
    /// </summary>
    public int OffsetY { get; private set; }

    /// <summary>
    /// Gets the viewport width (in cells).
    /// </summary>
    public int ViewportWidth { get; private set; }

    /// <summary>
    /// Gets the viewport height (in rows).
    /// </summary>
    public int ViewportHeight { get; private set; }

    /// <summary>
    /// Gets the content extent width (in cells).
    /// </summary>
    public int ExtentWidth { get; private set; }

    /// <summary>
    /// Gets the content extent height (in rows).
    /// </summary>
    public int ExtentHeight { get; private set; }

    /// <summary>
    /// Occurs when the scroll model state changes (viewport, extent, or offset).
    /// </summary>
    public event Action? Changed;

    /// <summary>
    /// Gets the current version number of the object.
    /// </summary>
    [Bindable]
    public partial int Version { get; private set; }

    /// <summary>
    /// Updates the viewport size and clamps offsets if needed.
    /// </summary>
    public void SetViewport(int width, int height)
    {
        width = Math.Max(0, width);
        height = Math.Max(0, height);

        if (ViewportWidth == width && ViewportHeight == height)
        {
            return;
        }

        ViewportWidth = width;
        ViewportHeight = height;
        ClampOffsets();
        OnChanged();
    }

    /// <summary>
    /// Updates the extent size and clamps offsets if needed.
    /// </summary>
    public void SetExtent(int width, int height)
    {
        width = Math.Max(0, width);
        height = Math.Max(0, height);

        if (ExtentWidth == width && ExtentHeight == height)
        {
            return;
        }

        ExtentWidth = width;
        ExtentHeight = height;
        ClampOffsets();
        OnChanged();
    }

    /// <summary>
    /// Sets the scroll offsets, clamped to the valid range.
    /// </summary>
    public void SetOffset(int x, int y)
    {
        var maxX = Math.Max(0, ExtentWidth - ViewportWidth);
        var maxY = Math.Max(0, ExtentHeight - ViewportHeight);
        var clampedX = Math.Clamp(x, 0, maxX);
        var clampedY = Math.Clamp(y, 0, maxY);

        if (OffsetX == clampedX && OffsetY == clampedY)
        {
            return;
        }

        OffsetX = clampedX;
        OffsetY = clampedY;
        OnChanged();
    }

    /// <summary>
    /// Scrolls by the specified delta (in cells/rows).
    /// </summary>
    public void ScrollBy(int dx, int dy)
    {
        SetOffset(OffsetX + dx, OffsetY + dy);
    }

    /// <summary>
    /// Scrolls so that the specified cell position becomes visible within the viewport.
    /// </summary>
    public void ScrollToMakeVisible(int xCell, int yRow)
    {
        var targetX = OffsetX;
        var targetY = OffsetY;

        if (xCell < OffsetX)
        {
            targetX = xCell;
        }
        else if (xCell >= OffsetX + ViewportWidth)
        {
            targetX = xCell - Math.Max(0, ViewportWidth - 1);
        }

        if (yRow < OffsetY)
        {
            targetY = yRow;
        }
        else if (yRow >= OffsetY + ViewportHeight)
        {
            targetY = yRow - Math.Max(0, ViewportHeight - 1);
        }

        SetOffset(targetX, targetY);
    }

    private void OnChanged()
    {
        UpdateVersion();
        Changed?.Invoke();
    }

    private void ClampOffsets()
    {
        var maxX = Math.Max(0, ExtentWidth - ViewportWidth);
        var maxY = Math.Max(0, ExtentHeight - ViewportHeight);
        OffsetX = Math.Clamp(OffsetX, 0, maxX);
        OffsetY = Math.Clamp(OffsetY, 0, maxY);
    }
    private void UpdateVersion()
    {
        var hash = new HashCode();
        // ReSharper disable NonReadonlyMemberInGetHashCode
        hash.Add(OffsetX);
        hash.Add(OffsetY);
        hash.Add(ViewportWidth);
        hash.Add(ViewportHeight);
        hash.Add(ExtentWidth);
        hash.Add(ExtentHeight);
        // ReSharper restore NonReadonlyMemberInGetHashCode
        Version = hash.ToHashCode();
    }
}
