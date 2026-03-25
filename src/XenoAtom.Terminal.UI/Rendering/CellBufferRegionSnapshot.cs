// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI;

namespace XenoAtom.Terminal.UI.Rendering;

internal sealed class CellBufferRegionSnapshot
{
    private int[] _scalars = [];
    private Style[] _cells = [];
    private ulong[] _hyperlinks = [];

    internal Rectangle Rect { get; private set; }

    internal bool HasRegion => Rect.Width > 0 && Rect.Height > 0;

    internal void Save(CellBuffer buffer, in Rectangle rect)
    {
        ArgumentNullException.ThrowIfNull(buffer);

        if (rect.Width <= 0 || rect.Height <= 0)
        {
            Rect = default;
            return;
        }

        var length = checked(rect.Width * rect.Height);
        EnsureCapacity(length);
        buffer.CopyRegion(rect, _scalars.AsSpan(0, length), _cells.AsSpan(0, length), _hyperlinks.AsSpan(0, length));
        Rect = rect;
    }

    internal void Restore(CellBuffer buffer)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        if (!HasRegion)
        {
            return;
        }

        var rect = Rect;
        var length = checked(rect.Width * rect.Height);
        buffer.RestoreRegion(rect, _scalars.AsSpan(0, length), _cells.AsSpan(0, length), _hyperlinks.AsSpan(0, length));
        Rect = default;
    }

    private void EnsureCapacity(int length)
    {
        if (_scalars.Length >= length)
        {
            return;
        }

        _scalars = new int[length];
        _cells = new Style[length];
        _hyperlinks = new ulong[length];
    }
}
