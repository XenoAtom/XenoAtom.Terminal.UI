// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Collections;
using System.ComponentModel;
using XenoAtom.Terminal.UI.Collections;

namespace XenoAtom.Terminal.UI.Controls;

public abstract partial class Panel : Visual, IEnumerable<Visual>
{
    private readonly VisualList<Visual> _children;

    protected Panel()
    {
        this.HorizontalAlignment = HorizontalAlignment.Stretch;
        this.VerticalAlignment = VerticalAlignment.Stretch;
        _children = new VisualList<Visual>(this, "Children");
    }

    public VisualList<Visual> Children => _children;

    internal void AddRange(params Visual[] children)
    {
        ArgumentNullException.ThrowIfNull(children);
        _children.AddRange(children);
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public IEnumerator<Visual> GetEnumerator() => _children.GetEnumerator();

    IEnumerator<Visual> IEnumerable<Visual>.GetEnumerator() => _children.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => _children.GetEnumerator();

    protected override int ChildrenCount => _children.Count;

    protected override Visual GetChild(int index) => _children[index];
}
