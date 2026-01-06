// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI.Controls;

public abstract partial class Panel : Visuals.Visual
{
    private readonly List<Visuals.Visual> _children = new();

    protected Panel()
    {
    }

    protected IReadOnlyList<Visuals.Visual> Children => _children;

    public void Add(params Visuals.Visual[] children)
    {
        ArgumentNullException.ThrowIfNull(children);
        foreach (var child in children)
        {
            AttachChild(child);
            _children.Add(child);
        }

        App?.RequestRender();
    }

    protected override int ChildrenCount => _children.Count;

    protected override Visuals.Visual GetChild(int index) => _children[index];
}
