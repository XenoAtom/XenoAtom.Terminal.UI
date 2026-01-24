// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Collections;
using System.ComponentModel;
using XenoAtom.Terminal.UI.Collections;

namespace XenoAtom.Terminal.UI.Controls;

/// <summary>
/// Base class for controls that manage a collection of child visuals.
/// </summary>
public abstract partial class Panel : Visual, IEnumerable<Visual>
{
    private readonly VisualList<Visual> _children;

    /// <summary>
    /// Initializes a new instance of the <see cref="Panel"/> class.
    /// </summary>
    protected Panel()
    {
        this.HorizontalAlignment = Align.Stretch;
        this.VerticalAlignment = Align.Stretch;
        _children = new VisualList<Visual>(this, "Children");
    }

    /// <summary>
    /// Gets the collection of child visuals.
    /// </summary>
    [Bindable]
    public VisualList<Visual> Children => _children;

    internal void AddRange(params Visual[] children)
    {
        ArgumentNullException.ThrowIfNull(children);
        _children.AddRange(children);
    }

    /// <inheritdoc />
    [EditorBrowsable(EditorBrowsableState.Never)]
    public IEnumerator<Visual> GetEnumerator() => _children.GetEnumerator();

    IEnumerator<Visual> IEnumerable<Visual>.GetEnumerator() => _children.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => _children.GetEnumerator();

    /// <inheritdoc />
    protected override int ChildrenCount => _children.Count;

    /// <inheritdoc />
    protected override Visual GetChild(int index) => _children[index];
}
