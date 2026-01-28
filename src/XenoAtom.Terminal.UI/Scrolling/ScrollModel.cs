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
    [Bindable]
    public partial int OffsetX { get; private set; }

    /// <summary>
    /// Gets the vertical scroll offset (in rows).
    /// </summary>
    [Bindable]
    public partial int OffsetY { get; private set; }

    /// <summary>
    /// Gets the viewport width (in cells).
    /// </summary>
    [Bindable]
    public partial int ViewportWidth { get; private set; }

    /// <summary>
    /// Gets the viewport height (in rows).
    /// </summary>
    [Bindable]
    public partial int ViewportHeight { get; private set; }

    /// <summary>
    /// Gets the content extent width (in cells).
    /// </summary>
    [Bindable]
    public partial int ExtentWidth { get; private set; }

    /// <summary>
    /// Gets the content extent height (in rows).
    /// </summary>
    [Bindable]
    public partial int ExtentHeight { get; private set; }

    /// <summary>
    /// Occurs when the scroll model state changes (viewport, extent, or offset).
    /// </summary>
    public event Action? Changed;

    /// <summary>
    /// Updates the viewport size and clamps offsets if needed.
    /// </summary>
    public void SetViewport(int width, int height)
    {
        width = Math.Max(0, width);
        height = Math.Max(0, height);

        if (_viewportWidth == width && _viewportHeight == height)
        {
            return;
        }

        ViewportWidth = width;
        ViewportHeight = height;
        ClampOffsets();
        Changed?.Invoke();
    }

    /// <summary>
    /// Updates the extent size and clamps offsets if needed.
    /// </summary>
    public void SetExtent(int width, int height)
    {
        width = Math.Max(0, width);
        height = Math.Max(0, height);

        if (_extentWidth == width && _extentHeight == height)
        {
            return;
        }

        ExtentWidth = width;
        ExtentHeight = height;
        ClampOffsets();
        Changed?.Invoke();
    }

    /// <summary>
    /// Sets the scroll offsets, clamped to the valid range.
    /// </summary>
    public void SetOffset(int x, int y)
    {
        var maxX = Math.Max(0, _extentWidth - _viewportWidth);
        var maxY = Math.Max(0, _extentHeight - _viewportHeight);
        var clampedX = Math.Clamp(x, 0, maxX);
        var clampedY = Math.Clamp(y, 0, maxY);

        if (_offsetX == clampedX && _offsetY == clampedY)
        {
            return;
        }

        OffsetX = clampedX;
        OffsetY = clampedY;
        Changed?.Invoke();
    }

    /// <summary>
    /// Scrolls by the specified delta (in cells/rows).
    /// </summary>
    public void ScrollBy(int dx, int dy)
    {
        SetOffset(_offsetX + dx, _offsetY + dy);
    }

    /// <summary>
    /// Scrolls so that the specified cell position becomes visible within the viewport.
    /// </summary>
    public void ScrollToMakeVisible(int xCell, int yRow)
    {
        var targetX = _offsetX;
        var targetY = _offsetY;

        if (xCell < _offsetX)
        {
            targetX = xCell;
        }
        else if (xCell >= _offsetX + _viewportWidth)
        {
            targetX = xCell - Math.Max(0, _viewportWidth - 1);
        }

        if (yRow < _offsetY)
        {
            targetY = yRow;
        }
        else if (yRow >= _offsetY + _viewportHeight)
        {
            targetY = yRow - Math.Max(0, _viewportHeight - 1);
        }

        SetOffset(targetX, targetY);
    }

    private void ClampOffsets()
    {
        var maxX = Math.Max(0, _extentWidth - _viewportWidth);
        var maxY = Math.Max(0, _extentHeight - _viewportHeight);
        OffsetX = Math.Clamp(_offsetX, 0, maxX);
        OffsetY = Math.Clamp(_offsetY, 0, maxY);
    }
}
