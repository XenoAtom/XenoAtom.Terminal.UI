// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI;

public sealed class ComputedVisual : Visual, IDisposable
{
    private readonly Computed<Visual?> _computed;

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
        ClearChildren();
    }

    protected override CellSize MeasureOverride(CellSize availableSize)
    {
        var child = EnsureChild();
        if (child is null)
        {
            return default;
        }

        child.Measure(availableSize);
        return child.DesiredSize;
    }

    protected override void ArrangeOverride(CellRect finalRect)
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
        if (Children.Count > 0)
        {
            return Children[0];
        }

        var child = _computed.Value;
        if (child is null)
        {
            return null;
        }

        AddChild(child);
        return child;
    }

    private void OnInvalidated()
    {
        ClearChildren();
        EnsureChild();
        App?.RequestRender();
    }
}
