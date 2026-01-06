// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Geometry;

namespace XenoAtom.Terminal.UI.Visuals;

public sealed class ComputedVisual : Visual, IDisposable
{
    private readonly Computed<Visual?> _computed;
    private Visual? _child;

    public ComputedVisual(Func<Visual?> build)
    {
        _computed = new Computed<Visual?>(build ?? throw new ArgumentNullException(nameof(build)));
        _computed.Invalidated += OnInvalidated;
    }

    public void Dispose()
    {
        _computed.Invalidated -= OnInvalidated;
        _computed.Dispose();
    }

    protected override void OnAttachedToApp(TerminalApp app)
    {
        _ = app;
        EnsureChild();
    }

    protected override void OnDetachedFromApp(TerminalApp app)
    {
        _ = app;
        ClearChild();
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var child = EnsureChild();
        if (child is null)
        {
            return default;
        }

        child.Measure(availableSize);
        return child.DesiredSize;
    }

    protected override void ArrangeOverride(Rectangle finalRect)
    {
        var child = EnsureChild();
        if (child is null)
        {
            // When there is no child, don't participate in hit-testing or rendering.
            Bounds = default;
            return;
        }

        child.Arrange(finalRect);
        Bounds = finalRect;
    }

    private Visual? EnsureChild()
    {
        if (_child is not null)
        {
            return _child;
        }

        var child = _computed.Value;
        if (child is null)
        {
            return null;
        }

        _child = child;
        AttachChild(child);
        return child;
    }

    private void OnInvalidated()
    {
        ClearChild();
        EnsureChild();
        App?.RequestRender();
    }

    private void ClearChild()
    {
        if (_child is null)
        {
            return;
        }

        DetachChild(_child);
        _child = null;
    }

    protected override int ChildrenCount => _child is null ? 0 : 1;

    protected override Visual GetChild(int index)
    {
        if (index == 0 && _child is not null)
        {
            return _child;
        }

        throw new ArgumentOutOfRangeException(nameof(index));
    }
}
