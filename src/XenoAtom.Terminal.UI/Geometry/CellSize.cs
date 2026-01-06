// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI.Geometry;

public readonly record struct CellSize(int Width, int Height)
{
    public static readonly CellSize Zero = new(0, 0);

    public override string ToString() => $"{Width}x{Height}";
}

