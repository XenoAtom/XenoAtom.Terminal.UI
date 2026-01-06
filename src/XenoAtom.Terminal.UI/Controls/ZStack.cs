// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI.Controls;

/// <summary>
/// A panel that overlays children on the same bounds (Z-order follows child order).
/// </summary>
public sealed partial class ZStack : Panel
{
    public ZStack()
    {
    }

    public ZStack(params Visual[] children)
    {
        AddRange(children);
    }
}
