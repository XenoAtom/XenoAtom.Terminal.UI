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
    private readonly Dictionary<Visual, Visual?> _windowOwners;

    /// <summary>
    /// Initializes a new instance of the <see cref="WindowLayer"/> class.
    /// </summary>
    public WindowLayer()
    {
        this.HorizontalAlignment(Align.Stretch);
        this.VerticalAlignment(Align.Stretch);
        _windows = new VisualList<Visual>(this, "Windows");
        _windowOwners = new Dictionary<Visual, Visual?>();
        AddHandler(PointerPressedEvent, OnPointerPressedHandledToo, handledEventsToo: true);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="WindowLayer"/> class with background content.
    /// </summary>
    /// <param name="content">The background content displayed behind windows.</param>
    public WindowLayer(Visual content) : this()
    {
        this.Content(content);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="WindowLayer"/> class with computed background content.
    /// </summary>
    /// <param name="contentFactory">A factory that provides the background content displayed behind windows.</param>
    public WindowLayer(Func<Visual> contentFactory) : this()
    {
        this.Content(contentFactory);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="WindowLayer"/> class with bound background content.
    /// </summary>
    /// <param name="content">A binding that supplies the background content displayed behind windows.</param>
    public WindowLayer(Binding<Visual?> content) : this()
    {
        this.Content(content);
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
        => AddWindow(window, owner: null);

    internal void AddWindow(Visual window, Visual? owner)
    {
        ArgumentNullException.ThrowIfNull(window);
        if (owner is not null)
        {
            if (ReferenceEquals(owner, window))
            {
                throw new ArgumentException("A window cannot own itself.", nameof(owner));
            }

            if (!_windowOwners.ContainsKey(owner))
            {
                throw new InvalidOperationException("The owner window must already be attached to the layer.");
            }
        }

        var index = owner is null ? _windows.Count : GetInsertionIndex(owner);
        _windows.Insert(index, window);
        _windowOwners[window] = owner;
    }

    /// <summary>
    /// Removes a window from the layer.
    /// </summary>
    /// <param name="window">The window visual.</param>
    /// <returns><see langword="true"/> if the window was removed.</returns>
    public bool RemoveWindow(Visual window)
    {
        ArgumentNullException.ThrowIfNull(window);

        if (!_windowOwners.ContainsKey(window))
        {
            return false;
        }

        var ownedWindows = GetOwnedWindows(window);
        for (var i = 0; i < ownedWindows.Length; i++)
        {
            RemoveWindowCore(ownedWindows[i]);
        }

        return RemoveWindowCore(window);
    }

    internal Visual[] GetOwnedWindows(Visual owner)
    {
        ArgumentNullException.ThrowIfNull(owner);

        var result = new List<Visual>();
        for (var i = _windows.Count - 1; i >= 0; i--)
        {
            var candidate = _windows[i];
            if (IsOwnedBy(candidate, owner))
            {
                result.Add(candidate);
            }
        }

        return result.ToArray();
    }

    /// <inheritdoc />
    private void OnPointerPressedHandledToo(object? sender, PointerEventArgs e)
    {
        if (!ReferenceEquals(sender, this) || e.RoutingPhase != RoutingPhase.Bubble)
        {
            return;
        }

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
        var groupRoot = GetTopLevelOwner(window);
        var index = _windows.IndexOf(groupRoot);
        if (index < 0)
        {
            return;
        }

        var groupLength = GetGroupLength(groupRoot, index);
        if (index + groupLength >= _windows.Count)
        {
            return;
        }

        for (var i = 0; i < groupLength; i++)
        {
            _windows.Move(index, _windows.Count - 1);
        }
    }

    private bool RemoveWindowCore(Visual window)
    {
        _windowOwners.Remove(window);
        return _windows.Remove(window);
    }

    private int GetInsertionIndex(Visual owner)
    {
        var ownerIndex = _windows.IndexOf(owner);
        if (ownerIndex < 0)
        {
            throw new InvalidOperationException("The owner window must already be attached to the layer.");
        }

        return ownerIndex + GetGroupLength(owner, ownerIndex);
    }

    private int GetGroupLength(Visual groupRoot, int groupRootIndex)
    {
        var end = groupRootIndex + 1;
        while (end < _windows.Count && IsOwnedBy(_windows[end], groupRoot))
        {
            end++;
        }

        return end - groupRootIndex;
    }

    private Visual GetTopLevelOwner(Visual window)
    {
        var current = window;
        while (_windowOwners.TryGetValue(current, out var owner) && owner is not null)
        {
            current = owner;
        }

        return current;
    }

    private bool IsOwnedBy(Visual window, Visual owner)
    {
        while (_windowOwners.TryGetValue(window, out var currentOwner) && currentOwner is not null)
        {
            if (ReferenceEquals(currentOwner, owner))
            {
                return true;
            }

            window = currentOwner;
        }

        return false;
    }
}
