// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class DocumentFlowTests
{
    [TestMethod]
    public void DocumentFlow_Virtualizes_Attached_Block_Visuals_While_Scrolling()
    {
        var flow = new DocumentFlow();
        for (var i = 0; i < 200; i++)
        {
            var content = new FlowDocument().Add(new ProbeBlock($"Entry {i}"));
            flow.Items.Add(new DocumentFlowItem
            {
                Content = content,
                Alignment = DocumentFlowAlignment.Left,
                MaxWidth = 30,
            });
        }

        using var driver = new TerminalAppTestDriver(flow, TerminalHostKind.Fullscreen, new TerminalSize(40, 8));
        driver.Tick();

        var attachedBefore = flow.EnumerateVisualsDepthFirst().OfType<ProbeVisual>().Count();
        Assert.IsTrue(attachedBefore <= 20, $"Expected virtualization to keep active visuals bounded, got {attachedBefore}.");

        for (var i = 0; i < 6; i++)
        {
            driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.PageDown });
            driver.Tick();
        }

        var attachedAfter = flow.EnumerateVisualsDepthFirst().OfType<ProbeVisual>().Count();
        Assert.IsTrue(attachedAfter <= 20, $"Expected virtualization to keep active visuals bounded after scrolling, got {attachedAfter}.");
    }

    [TestMethod]
    public void DocumentFlow_FollowTail_And_ScrollToTail_Work()
    {
        var flow = new DocumentFlow();

        using var driver = new TerminalAppTestDriver(flow, TerminalHostKind.Fullscreen, new TerminalSize(40, 8));
        driver.Tick();

        for (var i = 0; i < 25; i++)
        {
            flow.Items.Add(CreateItem($"Item {i}"));
        }

        driver.Tick();
        flow.ScrollToTail();
        driver.Tick();
        var tailOffset = flow.Scroll.OffsetY;
        Assert.IsTrue(tailOffset > 0);

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.PageUp });
        driver.Tick();
        Assert.IsFalse(flow.FollowTail);
        var detachedOffset = flow.Scroll.OffsetY;
        Assert.IsTrue(detachedOffset < tailOffset);

        flow.Items.Add(CreateItem("Item 25"));
        driver.Tick();
        var offsetAfterAppend = flow.Scroll.OffsetY;
        Assert.AreEqual(detachedOffset, offsetAfterAppend, "Appending while detached should keep the viewport offset.");

        flow.ScrollToTail();
        driver.Tick();
        Assert.IsTrue(flow.Scroll.OffsetY >= tailOffset);
        Assert.IsTrue(flow.FollowTail);
    }

    [TestMethod]
    public void DocumentFlow_ScrollToTail_False_Disables_FollowTail_Without_Scrolling()
    {
        var flow = new DocumentFlow();
        for (var i = 0; i < 40; i++)
        {
            flow.Items.Add(CreateItem($"Item {i}"));
        }

        using var driver = new TerminalAppTestDriver(flow, TerminalHostKind.Fullscreen, new TerminalSize(40, 8));
        driver.Tick();

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Home });
        driver.Tick();
        Assert.AreEqual(0, flow.Scroll.OffsetY);

        flow.ScrollToTail(false);
        driver.Tick();
        Assert.IsFalse(flow.FollowTail);
        Assert.AreEqual(0, flow.Scroll.OffsetY, "Disabling follow-tail should not force scrolling to the end.");

        flow.Items.Add(CreateItem("Item 40"));
        driver.Tick();
        Assert.AreEqual(0, flow.Scroll.OffsetY, "Appending while follow-tail is disabled should keep the viewport position.");
        Assert.IsFalse(flow.FollowTail);
    }

    [TestMethod]
    public void DocumentFlow_ScrollToItem_Jumps_To_Item_Top_And_Disables_FollowTail()
    {
        var flow = new DocumentFlow
        {
            ItemPadding = Thickness.Zero,
            ItemSpacing = 1,
        };

        for (var i = 0; i < 30; i++)
        {
            flow.Items.Add(new DocumentFlowItem
            {
                Content = new FlowDocument().Add(new FixedHeightBlock($"Item {i}", 1)),
                Alignment = DocumentFlowAlignment.Left,
                MaxWidth = 24,
                Padding = Thickness.Zero,
            });
        }

        using var driver = new TerminalAppTestDriver(flow, TerminalHostKind.Fullscreen, new TerminalSize(40, 8));
        driver.Tick();

        flow.ScrollToItem(5);
        driver.Tick();

        Assert.AreEqual(10, flow.Scroll.OffsetY, "Expected index 5 to align to the viewport top when each item takes two rows.");
        Assert.IsFalse(flow.FollowTail, "Programmatic item navigation should detach follow-tail.");
    }

    [TestMethod]
    public void DocumentFlow_ScrollToItem_Clamps_To_Max_Offset_Near_Tail()
    {
        var flow = new DocumentFlow
        {
            ItemPadding = Thickness.Zero,
            ItemSpacing = 1,
        };

        for (var i = 0; i < 30; i++)
        {
            flow.Items.Add(new DocumentFlowItem
            {
                Content = new FlowDocument().Add(new FixedHeightBlock($"Item {i}", 1)),
                Alignment = DocumentFlowAlignment.Left,
                MaxWidth = 24,
                Padding = Thickness.Zero,
            });
        }

        using var driver = new TerminalAppTestDriver(flow, TerminalHostKind.Fullscreen, new TerminalSize(40, 8));
        driver.Tick();

        flow.ScrollToItem(29);
        driver.Tick();

        var maxOffset = Math.Max(0, flow.Scroll.ExtentHeight - flow.Scroll.ViewportHeight);
        Assert.AreEqual(maxOffset, flow.Scroll.OffsetY, "Scrolling to the last item should clamp to the maximum offset.");
    }

    [TestMethod]
    public void DocumentFlow_TryScrollToItem_Validates_Index_Range()
    {
        var flow = new DocumentFlow();
        flow.Items.Add(CreateItem("Item 0"));

        Assert.IsFalse(flow.TryScrollToItem(-1));
        Assert.IsFalse(flow.TryScrollToItem(1));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => flow.ScrollToItem(1));
    }

    [TestMethod]
    public void DocumentFlow_MaxCapacity_Trimming_Preserves_Viewport_When_Not_Pinned()
    {
        var flow = new DocumentFlow().MaxCapacity(0);

        using var driver = new TerminalAppTestDriver(flow, TerminalHostKind.Fullscreen, new TerminalSize(50, 10));
        driver.Tick();

        for (var i = 0; i < 10; i++)
        {
            flow.Items.Add(CreateItem($"Item {i}"));
        }

        driver.Tick();

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Home });
        driver.Tick();
        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.PageDown });
        driver.Tick();
        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.PageDown });
        driver.Tick();
        Assert.IsFalse(flow.FollowTail);

        var beforeOffset = flow.Scroll.OffsetY;
        Assert.IsTrue(beforeOffset > 0);
        var screen = new AnsiTestScreen(50, 10);
        screen.Apply(driver.Backend.GetOutText());

        flow.MaxCapacity = 8;
        flow.Items.Add(CreateItem("Item 10"));
        flow.Items.Add(CreateItem("Item 11"));
        driver.Tick();

        var afterOffset = flow.Scroll.OffsetY;
        Assert.IsTrue(afterOffset < beforeOffset, "Expected viewport compensation after head trimming.");
        Assert.IsFalse(flow.FollowTail);
    }

    [TestMethod]
    public void DocumentFlow_Can_Render_Mixed_Content_Blocks()
    {
        var table = new Table()
            .Headers("Key", "Value")
            .AddRow("Mode", "Fast");

        var log = new LogControl().MaxHeight(3);
        log.AppendLine("code: Console.WriteLine(\"Hello\")");

        var document = new FlowDocument()
            .AddParagraph("Mixed content item")
            .Add(table)
            .Add(log);

        var flow = new DocumentFlow();
        flow.Items.Add(new DocumentFlowItem
        {
            Content = document,
            Alignment = DocumentFlowAlignment.Left,
            MaxWidth = 48,
        });

        using var driver = new TerminalAppTestDriver(flow, TerminalHostKind.Fullscreen, new TerminalSize(60, 12));
        driver.Tick();

        var screen = new AnsiTestScreen(60, 12);
        screen.Apply(driver.Backend.GetOutText());
        var rendered = screen.GetText();
        StringAssert.Contains(rendered, "Mixed content item");
        StringAssert.Contains(rendered, "Mode");
        StringAssert.Contains(rendered, "code:");
    }

    [TestMethod]
    public void DocumentFlow_Recomputes_Extent_On_Width_Change()
    {
        var flow = new DocumentFlow();
        flow.Items.Add(new DocumentFlowItem
        {
            Content = new FlowDocument().AddParagraph("A long paragraph that wraps more when the viewport width becomes narrower."),
            Alignment = DocumentFlowAlignment.Left,
            MaxWidth = 40,
        });

        flow.Measure(new LayoutConstraints(0, 40, 0, 6));
        flow.Arrange(new Rectangle(0, 0, 40, 6));
        var extentWide = flow.Scroll.ExtentHeight;

        flow.Measure(new LayoutConstraints(0, 20, 0, 6));
        flow.Arrange(new Rectangle(0, 0, 20, 6));
        var extentNarrow = flow.Scroll.ExtentHeight;

        Assert.IsTrue(extentNarrow >= extentWide);
    }

    [TestMethod]
    public void DocumentFlow_Rebuilds_Layouts_When_ItemSpacing_Changes()
    {
        var flow = new DocumentFlow
        {
            ItemPadding = Thickness.Zero,
            ItemSpacing = 0,
        };

        for (var i = 0; i < 3; i++)
        {
            flow.Items.Add(new DocumentFlowItem
            {
                Content = new FlowDocument().Add(new FixedHeightBlock($"Item {i}", 1)),
                Alignment = DocumentFlowAlignment.Left,
                MaxWidth = 24,
            });
        }

        using var driver = new TerminalAppTestDriver(flow, TerminalHostKind.Fullscreen, new TerminalSize(30, 8));
        driver.Tick();

        var extentWithNoSpacing = flow.Scroll.ExtentHeight;
        Assert.AreEqual(3, extentWithNoSpacing);

        flow.ItemSpacing = 2;
        driver.Tick();

        var extentWithSpacing = flow.Scroll.ExtentHeight;
        Assert.AreEqual(7, extentWithSpacing, "Expected spacing changes to invalidate cached document layouts.");
    }

    [TestMethod]
    public void DocumentFlow_Rebuilds_Layouts_When_Default_ItemPadding_Changes()
    {
        var flow = new DocumentFlow
        {
            ItemPadding = Thickness.Zero,
            ItemSpacing = 0,
        };

        for (var i = 0; i < 2; i++)
        {
            flow.Items.Add(new DocumentFlowItem
            {
                Content = new FlowDocument().Add(new FixedHeightBlock($"Item {i}", 1)),
                Alignment = DocumentFlowAlignment.Left,
                MaxWidth = 24,
            });
        }

        using var driver = new TerminalAppTestDriver(flow, TerminalHostKind.Fullscreen, new TerminalSize(30, 8));
        driver.Tick();

        var extentWithoutPadding = flow.Scroll.ExtentHeight;
        Assert.AreEqual(2, extentWithoutPadding);

        flow.ItemPadding = new Thickness(0, 1, 0, 2);
        driver.Tick();

        var extentWithPadding = flow.Scroll.ExtentHeight;
        Assert.AreEqual(8, extentWithPadding, "Expected default item padding changes to invalidate cached document layouts.");
    }

    [TestMethod]
    public void DocumentFlow_MaxWidthPercent_Limits_Bubble_Width()
    {
        var flow = new DocumentFlow();
        flow.Items.Add(new DocumentFlowItem
        {
            Content = new FlowDocument().Add(new StretchProbeBlock()),
            Alignment = DocumentFlowAlignment.Left,
            MaxWidthPercent = 50,
            Padding = Thickness.Zero,
        });

        using var driver = new TerminalAppTestDriver(flow, TerminalHostKind.Fullscreen, new TerminalSize(80, 6));
        driver.Tick();

        var width = flow.EnumerateVisualsDepthFirst().OfType<StretchProbeVisual>().Single().Bounds.Width;
        Assert.AreEqual(40, width);
    }

    [TestMethod]
    public void DocumentFlow_MaxWidth_And_MaxWidthPercent_Use_Most_Restrictive_Constraint()
    {
        var flow = new DocumentFlow();
        flow.Items.Add(new DocumentFlowItem
        {
            Content = new FlowDocument().Add(new StretchProbeBlock()),
            Alignment = DocumentFlowAlignment.Left,
            MaxWidth = 24,
            MaxWidthPercent = 80,
            Padding = Thickness.Zero,
        });

        using var driver = new TerminalAppTestDriver(flow, TerminalHostKind.Fullscreen, new TerminalSize(80, 6));
        driver.Tick();

        var width = flow.EnumerateVisualsDepthFirst().OfType<StretchProbeVisual>().Single().Bounds.Width;
        Assert.AreEqual(24, width);
    }

    [TestMethod]
    public void DocumentFlow_Keyboard_Scrolls_With_Arrow_Home_End()
    {
        var flow = new DocumentFlow();
        for (var i = 0; i < 80; i++)
        {
            flow.Items.Add(CreateItem($"Item {i}"));
        }

        using var driver = new TerminalAppTestDriver(flow, TerminalHostKind.Fullscreen, new TerminalSize(40, 8));
        driver.Tick();

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Home });
        driver.Tick();
        Assert.AreEqual(0, flow.Scroll.OffsetY);

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Down });
        driver.Tick();
        Assert.IsTrue(flow.Scroll.OffsetY > 0, "Down key should scroll document flow.");

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.End });
        driver.Tick();
        var endOffset = flow.Scroll.OffsetY;
        Assert.IsTrue(endOffset > 0, "End key should jump to the tail.");

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Home });
        driver.Tick();
        Assert.AreEqual(0, flow.Scroll.OffsetY, "Home key should jump back to top.");
    }

    [TestMethod]
    public void DocumentFlow_MouseWheel_Scrolls_On_Content()
    {
        var flow = new DocumentFlow();
        for (var i = 0; i < 80; i++)
        {
            flow.Items.Add(CreateItem($"Item {i}"));
        }

        using var driver = new TerminalAppTestDriver(flow, TerminalHostKind.Fullscreen, new TerminalSize(40, 8));
        driver.Tick();

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Home });
        driver.Tick();
        Assert.AreEqual(0, flow.Scroll.OffsetY);
        driver.Backend.PushEvent(new TerminalMouseEvent
        {
            Kind = TerminalMouseKind.Wheel,
            Button = TerminalMouseButton.Wheel,
            WheelDelta = -1,
            X = 2,
            Y = 2,
        });
        driver.Tick();

        Assert.IsTrue(flow.Scroll.OffsetY > 0, "Mouse wheel on document content should scroll.");
    }

    [TestMethod]
    public void DocumentFlow_Scroll_Updates_Block_Visual_Arrangement()
    {
        var flow = new DocumentFlow
        {
            ItemPadding = Thickness.Zero,
            ItemSpacing = 0,
        };

        flow.Items.Add(new DocumentFlowItem
        {
            Content = new FlowDocument().Add(new FixedHeightBlock("Scrolling block", 10)),
            Alignment = DocumentFlowAlignment.Left,
            MaxWidth = 30,
            Padding = Thickness.Zero,
        });

        using var driver = new TerminalAppTestDriver(flow, TerminalHostKind.Fullscreen, new TerminalSize(40, 6));
        driver.Tick();

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Home });
        driver.Tick();
        Assert.AreEqual(0, flow.Scroll.OffsetY);

        var probe = flow.EnumerateVisualsDepthFirst().OfType<ProbeVisual>().Single();
        var yBefore = probe.Bounds.Y;

        driver.Backend.PushEvent(new TerminalMouseEvent
        {
            Kind = TerminalMouseKind.Wheel,
            Button = TerminalMouseButton.Wheel,
            WheelDelta = -1,
            X = 2,
            Y = 2,
        });
        driver.Tick();

        Assert.AreEqual(1, flow.Scroll.OffsetY, "Expected scroll offset to advance.");
        var yAfter = probe.Bounds.Y;
        Assert.AreEqual(yBefore - 1, yAfter, "Expected arranged text visual to move with the scroll offset.");
    }

    [TestMethod]
    public void DocumentFlow_Updates_Extent_When_Content_Collapses()
    {
        var content = new ToggleContent(
            new FixedHeightBlock("Header", 1),
            new FixedHeightBlock("Body", 20));

        var flow = new DocumentFlow();
        flow.Items.Add(new DocumentFlowItem
        {
            Content = content,
            Alignment = DocumentFlowAlignment.Left,
            MaxWidth = 24,
        });

        flow.Measure(new LayoutConstraints(0, 24, 0, 8));
        flow.Arrange(new Rectangle(0, 0, 24, 8));
        var expandedExtent = flow.Scroll.ExtentHeight;

        content.SetCollapsed(true);
        flow.Items[0] = flow.Items[0] with { Content = content };

        flow.Measure(new LayoutConstraints(0, 24, 0, 8));
        flow.Arrange(new Rectangle(0, 0, 24, 8));
        var collapsedExtent = flow.Scroll.ExtentHeight;

        Assert.IsTrue(collapsedExtent <= expandedExtent);
    }

    [TestMethod]
    public void DocumentFlow_Selection_IsExclusive_Across_Paragraph_Blocks()
    {
        var flow = new DocumentFlow
        {
            ItemPadding = Thickness.Zero,
            ItemSpacing = 1,
        };

        flow.Items.Add(new DocumentFlowItem
        {
            Content = new FlowDocument().AddParagraph("first paragraph world"),
            Alignment = DocumentFlowAlignment.Left,
            MaxWidth = 40,
            Padding = Thickness.Zero,
        });
        flow.Items.Add(new DocumentFlowItem
        {
            Content = new FlowDocument().AddParagraph("second paragraph value"),
            Alignment = DocumentFlowAlignment.Left,
            MaxWidth = 40,
            Padding = Thickness.Zero,
        });

        using var driver = new TerminalAppTestDriver(flow, TerminalHostKind.Fullscreen, new TerminalSize(50, 8));
        driver.Tick();

        var firstParagraph = FindRenderedParagraph(flow, "first paragraph world");
        var secondParagraph = FindRenderedParagraph(flow, "second paragraph value");

        DragSelectToken(driver, firstParagraph, "world");
        Assert.IsTrue(HasSelection(firstParagraph));

        DragSelectToken(driver, secondParagraph, "value");
        var firstAfter = FindRenderedParagraph(flow, "first paragraph world");
        var secondAfter = FindRenderedParagraph(flow, "second paragraph value");
        Assert.IsFalse(HasSelection(firstAfter), "Selecting text in a different document flow paragraph should clear previous selections.");
        Assert.IsTrue(HasSelection(secondAfter));
    }

    private static DocumentFlowItem CreateItem(string text)
        => new()
        {
            Content = new FlowDocument().AddParagraph(text),
            Alignment = DocumentFlowAlignment.Left,
            MaxWidth = 34,
        };

    private static Paragraph FindRenderedParagraph(DocumentFlow flow, string text)
    {
        foreach (var paragraph in flow.EnumerateVisualsDepthFirst().OfType<Paragraph>())
        {
            if (string.Equals(paragraph.Text, text, StringComparison.Ordinal))
            {
                return paragraph;
            }
        }

        Assert.Fail($"Could not find rendered paragraph `{text}`.");
        return null!;
    }

    private static void DragSelectToken(TerminalAppTestDriver driver, Paragraph paragraph, string token)
    {
        var text = paragraph.Text ?? string.Empty;
        var start = text.IndexOf(token, StringComparison.Ordinal);
        Assert.IsTrue(start >= 0, $"Token `{token}` was not found in paragraph text.");

        var bounds = paragraph.GetAbsoluteBounds();
        var y = bounds.Y;
        var startX = bounds.X + start;
        var endX = startX + token.Length;

        driver.Backend.PushEvent(new TerminalMouseEvent
        {
            Kind = TerminalMouseKind.Down,
            Button = TerminalMouseButton.Left,
            X = startX,
            Y = y,
        });
        driver.Backend.PushEvent(new TerminalMouseEvent
        {
            Kind = TerminalMouseKind.Drag,
            Button = TerminalMouseButton.Left,
            X = endX,
            Y = y,
        });
        driver.Backend.PushEvent(new TerminalMouseEvent
        {
            Kind = TerminalMouseKind.Up,
            Button = TerminalMouseButton.Left,
            X = endX,
            Y = y,
        });
        driver.Tick();
    }

    private static bool HasSelection(Paragraph paragraph)
    {
        const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
        var type = typeof(Paragraph);
        var anchor = (int)(type.GetField("_selectionAnchor", flags)?.GetValue(paragraph) ?? -1);
        var active = (int)(type.GetField("_selectionActive", flags)?.GetValue(paragraph) ?? -1);
        return anchor >= 0 && active >= 0 && anchor != active;
    }

    private sealed class ProbeBlock : DocumentFlowBlock
    {
        private readonly string _text;

        public ProbeBlock(string text)
        {
            _text = text;
        }

        public override Visual CreateVisual() => new ProbeVisual(_text);
    }

    private sealed class ProbeVisual : Visual
    {
        private readonly string _text;
        private readonly int _height;

        public ProbeVisual(string text, int height = 1)
        {
            _text = text;
            _height = Math.Max(1, height);
            HorizontalAlignment = Align.Stretch;
        }

        protected override SizeHints MeasureCore(in LayoutConstraints constraints)
        {
            var width = Math.Max(1, Math.Min(constraints.MaxWidth, TerminalTextUtility.GetWidth(_text.AsSpan())));
            return SizeHints.Fixed(new Size(width, _height));
        }

        protected override void RenderOverride(CellBuffer buffer)
        {
            var rect = Bounds;
            if (rect.Width <= 0 || rect.Height <= 0)
            {
                return;
            }

            for (var y = rect.Y; y < rect.Bottom; y++)
            {
                if (_text.Length == 0)
                {
                    continue;
                }

                buffer.WriteText(rect.X, y, _text.AsSpan(), Style.None);
            }
        }
    }

    private sealed class FixedHeightBlock : DocumentFlowBlock
    {
        private readonly string _text;
        private readonly int _height;

        public FixedHeightBlock(string text, int height)
        {
            _text = text;
            _height = height;
        }

        public override Visual CreateVisual() => new ProbeVisual(_text, _height);
    }

    private sealed class StretchProbeBlock : DocumentFlowBlock
    {
        public override Visual CreateVisual() => new StretchProbeVisual();
    }

    private sealed class StretchProbeVisual : Visual
    {
        protected override SizeHints MeasureCore(in LayoutConstraints constraints)
        {
            var width = constraints.MaxWidth == LayoutConstants.Infinite ? 1 : Math.Max(1, constraints.MaxWidth);
            return SizeHints.Fixed(new Size(width, 1));
        }
    }

    private sealed class ToggleContent : IDocumentFlowContent
    {
        private readonly DocumentFlowBlock _header;
        private readonly DocumentFlowBlock _body;
        private bool _collapsed;
        private int _version;

        public ToggleContent(DocumentFlowBlock header, DocumentFlowBlock body)
        {
            _header = header;
            _body = body;
        }

        public int Version => _version;

        public int BlockCount => _collapsed ? 1 : 2;

        public DocumentFlowBlock GetBlock(int index)
        {
            if (index == 0)
            {
                return _header;
            }

            if (!_collapsed && index == 1)
            {
                return _body;
            }

            throw new ArgumentOutOfRangeException(nameof(index));
        }

        public void SetCollapsed(bool collapsed)
        {
            if (_collapsed == collapsed)
            {
                return;
            }

            _collapsed = collapsed;
            _version++;
        }
    }
}
