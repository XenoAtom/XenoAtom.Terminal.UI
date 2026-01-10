// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Linq;
using System.Diagnostics.CodeAnalysis;
using XenoAtom.Terminal.UI.Geometry;

namespace XenoAtom.Terminal.UI.Controls;

/// <summary>
/// Displays a single child from <see cref="Panel.Children"/> based on <see cref="SelectedIndex"/>.
/// </summary>
public sealed partial class ContentSwitcher : Panel
{
    private Visual? _active;

    public ContentSwitcher()
    {
        this.SelectedIndex(0);
    }

    [Bindable]
    public partial int SelectedIndex { get; set; }

    protected override int ChildrenCount
        => TryGetActiveChild(out _) ? 1 : 0;

    protected override Visual GetChild(int index)
        => index == 0 && TryGetActiveChild(out var child) ? child : throw new ArgumentOutOfRangeException(nameof(index));

    protected override Size MeasureOverride(Size availableSize)
    {
        if (!TryGetActiveChild(out var child))
        {
            return default;
        }

        child.Measure(availableSize);
        return child.DesiredSize;
    }

    protected override void ArrangeOverride(Rectangle finalRect)
    {
        if (!TryGetActiveChild(out var child))
        {
            return;
        }

        child.Arrange(finalRect);
    }

    private bool TryGetActiveChild([NotNullWhen(true)] out Visual? child)
    {
        var count = Children.Count;
        if (count == 0)
        {
            child = null;
            return false;
        }

        var index = Math.Clamp(SelectedIndex, 0, count - 1);
        child = Children[index];
        return true;
    }

    partial void OnSelectedIndexChanged(int value)
    {
        var count = Children.Count;
        var app = App;

        var newActive = count == 0 ? null : Children[Math.Clamp(value, 0, count - 1)];
        if (ReferenceEquals(_active, newActive))
        {
            return;
        }

        var oldActive = _active;
        _active = newActive;

        if (app is null)
        {
            return;
        }

        if (oldActive is not null && oldActive.App is not null)
        {
            oldActive.DetachFromApp();
        }

        if (newActive is not null && newActive.App is null)
        {
            newActive.AttachToApp(app);
        }

        if (app.FocusedElement is { } focused && oldActive is not null && IsDescendantOf(focused, oldActive))
        {
            var focusCandidate = newActive?.Focusable == true
                ? newActive
                : newActive?.EnumerateVisualsDepthFirst().FirstOrDefault(v => v.Focusable && v.IsVisible && v.IsEnabled);

            app.Focus(focusCandidate);
        }
    }

    private static bool IsDescendantOf(Visual visual, Visual ancestor)
    {
        for (var v = visual; v is not null; v = v.Parent)
        {
            if (ReferenceEquals(v, ancestor))
            {
                return true;
            }
        }

        return false;
    }
}
