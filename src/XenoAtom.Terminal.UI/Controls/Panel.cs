// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI.Controls;

public abstract partial class Panel : Visuals.Visual
{
    protected Panel()
    {
    }

    public void Add(params Visuals.Visual[] children)
    {
        ArgumentNullException.ThrowIfNull(children);
        foreach (var child in children)
        {
            AddChild(child);
        }
    }
}

