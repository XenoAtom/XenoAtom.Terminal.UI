// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Hosting;
using XenoAtom.Terminal.UI.Layout;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class RoutedEventArgsTests
{
    [TestMethod]
    public void RoutedEventArgs_Sets_Source_And_OriginalSource()
    {
        var button = new Button("OK");
        var root = new PointerProbe { Content = button };

        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(20, 10));
        driver.Tick();

        var x = button.Bounds.X + Math.Min(1, Math.Max(0, button.Bounds.Width - 1));
        var y = button.Bounds.Y;
        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Down, Button = TerminalMouseButton.Left, X = x, Y = y });
        driver.TickUntil(() => root.SeenOriginal is not null);

        Assert.IsNotNull(root.SeenOriginal);
        Assert.IsTrue(IsInSubtree(root.SeenOriginal, button), "Expected OriginalSource to be the button or a descendant visual.");
        Assert.AreSame(root, root.SeenSource);
    }

    private static bool IsInSubtree(Visual visual, Visual ancestor)
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

    private sealed class PointerProbe : Visual
    {
        private Visual? _content;

        public Visual? SeenOriginal { get; private set; }

        public Visual? SeenSource { get; private set; }

        public Visual? Content
        {
            get => _content;
            init
            {
                if (value is null)
                {
                    return;
                }

                _content = value;
                AttachChild(value);
            }
        }

        public PointerProbe()
        {
            AddHandler(PointerPressedEvent, (_, e) =>
            {
                SeenOriginal = e.OriginalSource;
                SeenSource = e.Source;
            });
        }

        protected override int ChildrenCount => _content is null ? 0 : 1;

        protected override Visual GetChild(int index)
            => index == 0 && _content is not null ? _content : throw new ArgumentOutOfRangeException(nameof(index));

        protected override SizeHints MeasureCore(in LayoutConstraints constraints)
        {
            var size = constraints.Clamp(new Size(10, 1));
            _content?.Measure(new LayoutConstraints(0, size.Width, 0, size.Height));
            return SizeHints.Fixed(size);
        }

        protected override void ArrangeCore(in Rectangle finalRect)
        {
            Bounds = finalRect;
            _content?.Arrange(finalRect);
        }
    }
}
