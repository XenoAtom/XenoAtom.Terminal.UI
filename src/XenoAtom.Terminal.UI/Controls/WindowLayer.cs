// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Input;
using XenoAtom.Terminal.UI.Collections;

namespace XenoAtom.Terminal.UI.Controls;

public sealed partial class WindowLayer : Visual
{
    private readonly VisualList<Visual> _windows;

    public WindowLayer()
    {
        this.HorizontalAlignment(HorizontalAlignment.Stretch);
        this.VerticalAlignment(VerticalAlignment.Stretch);
        _windows = new VisualList<Visual>(this, "Windows");
    }

    [Bindable]
    public partial Visual? Content { get; set; }

    public void AddWindow(Visual window)
    {
        ArgumentNullException.ThrowIfNull(window);
        _windows.Add(window);
    }

    public bool RemoveWindow(Visual window)
    {
        ArgumentNullException.ThrowIfNull(window);
        return _windows.Remove(window);
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

        BringWindowToFront(rootChild);
    }

    protected override int ChildrenCount => (_content is null ? 0 : 1) + _windows.Count;

    protected override Visual GetChild(int index)
    {
        if (_content is not null)
        {
            if (index == 0)
            {
                return _content;
            }

            index--;
        }

        if ((uint)index >= (uint)_windows.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        return _windows[index];
    }

    private void BringWindowToFront(Visual window)
    {
        var index = _windows.IndexOf(window);
        if (index < 0 || index == _windows.Count - 1)
        {
            return;
        }

        _windows.Move(index, _windows.Count - 1);
    }
}
