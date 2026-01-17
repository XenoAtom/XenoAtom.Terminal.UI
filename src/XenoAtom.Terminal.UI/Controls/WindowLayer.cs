// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Input;
using XenoAtom.Terminal.UI.Collections;

namespace XenoAtom.Terminal.UI.Controls;

/// <summary>
/// Hosts a stack of windows in front of an optional background content.
/// </summary>
public sealed partial class WindowLayer : Visual
{
    private readonly VisualList<Visual> _windows;

    /// <summary>
    /// Initializes a new instance of the <see cref="WindowLayer"/> class.
    /// </summary>
    public WindowLayer()
    {
        this.HorizontalAlignment(HorizontalAlignment.Stretch);
        this.VerticalAlignment(VerticalAlignment.Stretch);
        _windows = new VisualList<Visual>(this, "Windows");
    }

    /// <summary>
    /// Gets or sets the background content behind all windows.
    /// </summary>
    [Bindable]
    public partial Visual? Content { get; set; }

    /// <summary>
    /// Adds a window to the layer.
    /// </summary>
    /// <param name="window">The window visual.</param>
    public void AddWindow(Visual window)
    {
        ArgumentNullException.ThrowIfNull(window);
        _windows.Add(window);
    }

    /// <summary>
    /// Removes a window from the layer.
    /// </summary>
    /// <param name="window">The window visual.</param>
    /// <returns><see langword="true"/> if the window was removed.</returns>
    public bool RemoveWindow(Visual window)
    {
        ArgumentNullException.ThrowIfNull(window);
        return _windows.Remove(window);
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
    protected override int ChildrenCount => (_content is null ? 0 : 1) + _windows.Count;

    /// <inheritdoc />
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
