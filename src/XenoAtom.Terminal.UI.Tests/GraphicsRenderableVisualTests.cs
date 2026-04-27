// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class GraphicsRenderableVisualTests
{
    [TestMethod]
    public void GraphicsPass_CollectsCommandsInVisualTreeOrder()
    {
        var first = new TestGraphicsVisual("first", 3, 2);
        var second = new TestGraphicsVisual("second", 2, 1);
        var root = new HStack { Spacing = 1 };
        root.Add(first);
        root.Add(second);

        var presenter = new RecordingGraphicsPresenter();
        using var driver = new TerminalAppTestDriver(
            root,
            TerminalHostKind.Fullscreen,
            new TerminalSize(20, 8),
            new TerminalAppOptions { GraphicsPresenter = presenter });

        driver.Tick();

        Assert.AreEqual(1, presenter.PresentCalls);
        Assert.AreEqual(1, first.RenderGraphicsCalls);
        Assert.AreEqual(1, second.RenderGraphicsCalls);

        var commands = presenter.Frames[^1];
        Assert.AreEqual(2, commands.Length);
        Assert.AreEqual(new Rectangle(0, 0, 3, 2), commands[0].CellBounds);
        Assert.AreEqual(new Rectangle(4, 0, 2, 1), commands[1].CellBounds);
        Assert.AreEqual(0, commands[0].PaintOrder);
        Assert.AreEqual(1, commands[1].PaintOrder);
        Assert.AreNotEqual(0ul, commands[0].VisualRenderId);
        Assert.AreNotEqual(commands[0].VisualRenderId, commands[1].VisualRenderId);
        Assert.AreEqual("first", commands[0].AccessibilityText);
        Assert.AreEqual("second", commands[1].AccessibilityText);

        var context = presenter.Contexts[^1];
        Assert.AreEqual(TerminalHostKind.Fullscreen, context.HostKind);
        Assert.AreEqual(new Rectangle(0, 0, 20, 8), context.ViewportBounds);
        Assert.AreEqual(TerminalGraphicsTextFrameKind.Full, context.TextFrameKind);
    }

    [TestMethod]
    public void GraphicsPass_RunsWhenTextSceneDoesNotRender()
    {
        var visual = new TestGraphicsVisual("image", 4, 2);
        var presenter = new RecordingGraphicsPresenter();
        using var driver = new TerminalAppTestDriver(
            visual,
            TerminalHostKind.Fullscreen,
            new TerminalSize(20, 8),
            new TerminalAppOptions { GraphicsPresenter = presenter });

        driver.Tick();
        presenter.ClearFrames();

        driver.App.RequestGraphicsRender();
        driver.Tick();

        Assert.AreEqual(1, presenter.PresentCalls);
        Assert.AreEqual(2, visual.RenderGraphicsCalls);
        Assert.AreEqual(1, presenter.Frames[^1].Length);
        Assert.AreEqual(TerminalGraphicsTextFrameKind.None, presenter.Contexts[^1].TextFrameKind);
    }

    [TestMethod]
    public void GraphicsPass_UpdatesRegistrationWhenChildDetaches()
    {
        var child = new TestGraphicsVisual("child", 4, 2);
        var host = new SingleChildHostVisual(child);
        var presenter = new RecordingGraphicsPresenter();
        using var driver = new TerminalAppTestDriver(
            host,
            TerminalHostKind.Fullscreen,
            new TerminalSize(20, 8),
            new TerminalAppOptions { GraphicsPresenter = presenter });

        driver.Tick();
        Assert.AreEqual(1, presenter.Frames[^1].Length);
        Assert.AreEqual(1, driver.App.GraphicsRenderableVisualCount);

        presenter.ClearFrames();
        host.SetChild(null);
        driver.App.RequestGraphicsRender();
        driver.Tick();

        Assert.AreEqual(1, presenter.PresentCalls);
        Assert.AreEqual(0, presenter.Frames[^1].Length);
        Assert.AreEqual(0, driver.App.GraphicsRenderableVisualCount);
        Assert.AreEqual(1, child.RenderGraphicsCalls);
    }

    [TestMethod]
    public void GraphicsPass_UpdatesSubtreeMarkersWhenChildIsReplaced()
    {
        var first = new TestGraphicsVisual("first", 4, 2);
        var second = new TestGraphicsVisual("second", 3, 1);
        var host = new SingleChildHostVisual(first);
        var presenter = new RecordingGraphicsPresenter();
        using var driver = new TerminalAppTestDriver(
            host,
            TerminalHostKind.Fullscreen,
            new TerminalSize(20, 8),
            new TerminalAppOptions { GraphicsPresenter = presenter });

        driver.Tick();
        Assert.AreEqual(1, driver.App.GraphicsRenderableVisualCount);
        Assert.AreEqual(1, host.GraphicsRenderableSubtreeCount);
        Assert.AreEqual("first", presenter.Frames[^1].Single().AccessibilityText);

        presenter.ClearFrames();
        host.SetChild(second);
        driver.Backend.SetSize(new TerminalSize(21, 8));
        driver.Tick();

        Assert.AreEqual(1, driver.App.GraphicsRenderableVisualCount);
        Assert.AreEqual(1, host.GraphicsRenderableSubtreeCount);
        Assert.AreEqual(1, first.RenderGraphicsCalls);
        Assert.AreEqual(1, second.RenderGraphicsCalls);
        Assert.AreEqual("second", presenter.Frames[^1].Single().AccessibilityText);
    }

    [TestMethod]
    public void GraphicsPass_TracksBindingDependencies()
    {
        var version = new State<int>(1);
        var visual = new BindingGraphicsVisual(version, 4, 2);
        var presenter = new RecordingGraphicsPresenter();
        using var driver = new TerminalAppTestDriver(
            visual,
            TerminalHostKind.Fullscreen,
            new TerminalSize(20, 8),
            new TerminalAppOptions { GraphicsPresenter = presenter });

        driver.Tick();
        presenter.ClearFrames();

        version.Value = 2;
        driver.Tick();

        Assert.AreEqual(1, presenter.PresentCalls);
        Assert.AreEqual(2, visual.RenderGraphicsCalls);
        Assert.AreEqual(2, presenter.Frames[^1].Single().Content.Version);
        Assert.AreEqual(TerminalGraphicsTextFrameKind.None, presenter.Contexts[^1].TextFrameKind);
    }

    [TestMethod]
    public void GraphicsPresenter_ResetOnBeginAndEndRun()
    {
        var presenter = new RecordingGraphicsPresenter();
        var driver = new TerminalAppTestDriver(
            new TestGraphicsVisual("image", 4, 2),
            TerminalHostKind.Fullscreen,
            new TerminalSize(20, 8),
            new TerminalAppOptions { GraphicsPresenter = presenter });

        Assert.AreEqual(1, presenter.ResetCalls);

        driver.Dispose();

        Assert.AreEqual(2, presenter.ResetCalls);
    }

    private sealed class TestGraphicsVisual : Visual, IGraphicsRenderableVisual
    {
        private readonly string _name;
        private readonly int _width;
        private readonly int _height;
        private readonly TerminalGraphicContent _content;

        public TestGraphicsVisual(string name, int width, int height)
        {
            _name = name;
            _width = width;
            _height = height;
            _content = TerminalGraphicContent.FromBytes(new byte[] { 1, 2, 3, 4 }, mediaType: "application/octet-stream", cacheKey: name);
        }

        public int RenderGraphicsCalls { get; private set; }

        protected override SizeHints MeasureCore(in LayoutConstraints constraints)
        {
            _ = constraints;
            return SizeHints.Fixed(new Size(_width, _height));
        }

        public void RenderGraphics(GraphicsRenderContext context)
        {
            RenderGraphicsCalls++;
            context.Add(Bounds, _content, ImageScaleMode.Fit, preserveAspectRatio: true, reserveCells: true, accessibilityText: _name);
        }
    }

    private sealed class BindingGraphicsVisual : Visual, IGraphicsRenderableVisual
    {
        private readonly State<int> _version;
        private readonly int _width;
        private readonly int _height;

        public BindingGraphicsVisual(State<int> version, int width, int height)
        {
            _version = version;
            _width = width;
            _height = height;
        }

        public int RenderGraphicsCalls { get; private set; }

        protected override SizeHints MeasureCore(in LayoutConstraints constraints)
        {
            _ = constraints;
            return SizeHints.Fixed(new Size(_width, _height));
        }

        public void RenderGraphics(GraphicsRenderContext context)
        {
            RenderGraphicsCalls++;
            var version = _version.Value;
            context.Add(
                Bounds,
                TerminalGraphicContent.FromBytes(new[] { (byte)version }, mediaType: "application/octet-stream", cacheKey: "binding", version),
                ImageScaleMode.Fit,
                preserveAspectRatio: true,
                reserveCells: true,
                accessibilityText: "binding");
        }
    }

    private sealed class SingleChildHostVisual : Visual
    {
        private Visual? _child;

        public SingleChildHostVisual(Visual? child)
        {
            SetChild(child);
        }

        protected override int ChildrenCount => _child is null ? 0 : 1;

        protected override Visual GetChild(int index)
            => index == 0 && _child is not null ? _child : throw new ArgumentOutOfRangeException(nameof(index));

        public void SetChild(Visual? child)
        {
            if (_child is not null)
            {
                DetachChild(_child);
            }

            _child = child;
            if (_child is not null)
            {
                AttachChild(_child);
            }
        }

        protected override SizeHints MeasureCore(in LayoutConstraints constraints)
            => _child is null ? SizeHints.Fixed(Size.Zero) : _child.Measure(constraints);

        protected override void ArrangeCore(in Rectangle finalRect)
        {
            _child?.Arrange(finalRect);
        }
    }

    private sealed class RecordingGraphicsPresenter : ITerminalGraphicsPresenter
    {
        public int PresentCalls { get; private set; }

        public int ResetCalls { get; private set; }

        public List<GraphicsCommand[]> Frames { get; } = new();

        public List<ContextSnapshot> Contexts { get; } = new();

        public TerminalGraphicsCapabilities Capabilities => TerminalGraphicsCapabilities.None;

        public ValueTask PresentAsync(GraphicsCommandBuffer current, TerminalGraphicsPresentContext context, CancellationToken cancellationToken = default)
        {
            _ = cancellationToken;
            PresentCalls++;
            Frames.Add(current.AsSpan().ToArray());
            Contexts.Add(new ContextSnapshot(context.HostKind, context.ViewportBounds, context.FrameIndex, context.TextFrameKind));
            return ValueTask.CompletedTask;
        }

        public void ClearFrames()
        {
            PresentCalls = 0;
            Frames.Clear();
            Contexts.Clear();
        }

        public void Reset()
        {
            ResetCalls++;
        }

        public void Dispose()
        {
        }
    }

    private readonly record struct ContextSnapshot(
        TerminalHostKind HostKind,
        Rectangle ViewportBounds,
        int FrameIndex,
        TerminalGraphicsTextFrameKind TextFrameKind);
}
