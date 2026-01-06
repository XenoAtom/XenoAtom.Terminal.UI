// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Input;

namespace XenoAtom.Terminal.UI.Controls;

public sealed class WindowLayer : Visuals.Visual
{
    private Visuals.Visual? _content;

    public Visuals.Visual? Content
    {
        get => _content;
        set
        {
            if (ReferenceEquals(_content, value))
            {
                return;
            }

            if (_content is not null)
            {
                throw new InvalidOperationException("WindowLayer currently only supports setting Content once.");
            }

            _content = value;
            if (value is not null)
            {
                AddChild(value);
            }

            App?.RequestRender();
        }
    }

    public void AddWindow(Visuals.Visual window)
    {
        ArgumentNullException.ThrowIfNull(window);
        AddChild(window);
        App?.RequestRender();
    }

    protected override void OnPointerPressed(PointerEventArgs e)
    {
        if (e.Button != TerminalMouseButton.Left)
        {
            return;
        }

        var hit = HitTest(e.UiX, e.UiY);
        if (hit is null)
        {
            return;
        }

        var rootChild = hit;
        while (rootChild.Parent is not null && !ReferenceEquals(rootChild.Parent, this))
        {
            rootChild = rootChild.Parent;
        }

        if (rootChild.Parent is null || ReferenceEquals(rootChild, _content))
        {
            return;
        }

        BringChildToFront(rootChild);
    }
}

