// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI.Controls;

public sealed partial class VSplitter : SplitterBase
{
    public VSplitter()
    {
    }

    public VSplitter(Visual first, Visual second)
    {
        First = first;
        Second = second;
    }

    protected override Orientation SplitOrientation => Orientation.Vertical;
}

