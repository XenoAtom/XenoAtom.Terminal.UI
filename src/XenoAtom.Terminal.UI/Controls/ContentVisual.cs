// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI.Controls;

/// <summary>
/// Base class for controls that expose a single visual content.
/// </summary>
public abstract partial class ContentVisual : Visual
{
    [Bindable]
    public partial Visual? Content { get; set; }

    protected override int ChildrenCount => _content is null ? 0 : 1;

    protected override Visual GetChild(int index)
        => index == 0 && _content is not null ? _content : throw new ArgumentOutOfRangeException(nameof(index));
}

