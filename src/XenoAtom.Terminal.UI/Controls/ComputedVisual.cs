// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.


// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Layout;

namespace XenoAtom.Terminal.UI.Controls;

public sealed class ComputedVisual : Visual, IDisposable
{
    private readonly Computed<Visual?> _computed;
    private Visual? _child;

    public ComputedVisual(Func<Visual?> build)
    {
        _computed = new Computed<Visual?>(build ?? throw new ArgumentNullException(nameof(build)));
        _computed.Invalidated += OnInvalidated;
        this.HorizontalAlignment(() => _child?.HorizontalAlignment ?? HorizontalAlignment.Stretch);
        this.VerticalAlignment(() => _child?.VerticalAlignment ?? VerticalAlignment.Stretch);
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

    protected override SizeHints MeasureCore(in LayoutConstraints constraints)
    {
        var child = EnsureChild();
        if (child is null)
        {
            return SizeHints.Fixed(default);
        }

        return child.Measure(constraints);
    }

    protected override void ArrangeCore(in Rectangle finalRect)
    {
        var child = EnsureChild();
        if (child is null)
        {
            // When there is no child, don't participate in hit-testing or rendering.
            Bounds = default;
            return;
        }

        child.Arrange(finalRect);
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
        Invalidate();
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
