// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI.Scrolling;

public sealed class ScrollModel
{
    public int OffsetX { get; private set; }
    public int OffsetY { get; private set; }

    public int ViewportWidth { get; private set; }
    public int ViewportHeight { get; private set; }

    public int ExtentWidth { get; private set; }
    public int ExtentHeight { get; private set; }

    public event Action? Changed;

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
        Changed?.Invoke();
    }

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
        Changed?.Invoke();
    }

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
        Changed?.Invoke();
    }

    public void ScrollBy(int dx, int dy)
    {
        SetOffset(OffsetX + dx, OffsetY + dy);
    }

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

    private void ClampOffsets()
    {
        var maxX = Math.Max(0, ExtentWidth - ViewportWidth);
        var maxY = Math.Max(0, ExtentHeight - ViewportHeight);
        OffsetX = Math.Clamp(OffsetX, 0, maxX);
        OffsetY = Math.Clamp(OffsetY, 0, maxY);
    }
}
