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

    [TestMethod]
    public void RoutedEventArgs_Provides_Preview_Then_Bubble_Phase()
    {
        var leaf = new LeafVisual();
        var root = new RoutingPhaseProbe { Content = leaf };

        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(20, 10));
        driver.Tick();

        var x = leaf.Bounds.X;
        var y = leaf.Bounds.Y;
        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Down, Button = TerminalMouseButton.Left, X = x, Y = y });
        driver.Tick();

        Assert.AreEqual(2, root.SeenPhases.Count, "Expected two invocations on root (preview and bubble).");
        Assert.AreEqual(RoutingPhase.Preview, root.SeenPhases[0], "Expected preview phase first.");
        Assert.AreEqual(RoutingPhase.Bubble, root.SeenPhases[1], "Expected bubble phase second.");
    }

    [TestMethod]
    public void RoutedEventArgs_HandledEventsToo_Still_Runs_When_Handled()
    {
        var leaf = new HandlingLeafVisual();
        var root = new HandledEventsTooProbe { Content = leaf };

        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(20, 10));
        driver.Tick();

        var x = leaf.Bounds.X;
        var y = leaf.Bounds.Y;
        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Down, Button = TerminalMouseButton.Left, X = x, Y = y });
        driver.Tick();

        Assert.AreEqual(0, root.RegularBubbleCount, "Expected regular bubble handler to be skipped when handled.");
        Assert.AreEqual(1, root.HandledTooBubbleCount, "Expected handled-events-too bubble handler to run when handled.");
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
            _content?.Arrange(finalRect);
        }
    }

    private sealed class RoutingPhaseProbe : Visual
    {
        private Visual? _content;

        public List<RoutingPhase> SeenPhases { get; } = new();

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

        public RoutingPhaseProbe()
        {
            AddHandler(PointerPressedEvent, (_, e) =>
            {
                if (e.Source == this)
                {
                    SeenPhases.Add(e.RoutingPhase);
                }
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
            _content?.Arrange(finalRect);
        }
    }

    private sealed class HandledEventsTooProbe : Visual
    {
        private Visual? _content;

        public int RegularBubbleCount { get; private set; }

        public int HandledTooBubbleCount { get; private set; }

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

        public HandledEventsTooProbe()
        {
            AddHandler(PointerPressedEvent, (_, e) =>
            {
                if (e.RoutingPhase == RoutingPhase.Bubble && e.Source == this)
                {
                    RegularBubbleCount++;
                }
            });

            AddHandler(PointerPressedEvent, (_, e) =>
            {
                if (e.RoutingPhase == RoutingPhase.Bubble && e.Source == this)
                {
                    HandledTooBubbleCount++;
                }
            }, handledEventsToo: true);
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
            _content?.Arrange(finalRect);
        }
    }

    private sealed class LeafVisual : Visual
    {
        protected override SizeHints MeasureCore(in LayoutConstraints constraints)
            => SizeHints.Fixed(constraints.Clamp(new Size(4, 1)));
    }

    private sealed class HandlingLeafVisual : Visual
    {
        protected override SizeHints MeasureCore(in LayoutConstraints constraints)
            => SizeHints.Fixed(constraints.Clamp(new Size(4, 1)));

        protected override void OnPointerPressed(PointerEventArgs e)
        {
            if (e.RoutingPhase == RoutingPhase.Preview)
            {
                e.Handled = true;
            }
        }
    }
}
