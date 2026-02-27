// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Collections;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Input;
using XenoAtom.Terminal.UI.Layout;
using XenoAtom.Terminal.UI.Rendering;
using XenoAtom.Terminal.UI.Scrolling;
using XenoAtom.Terminal.UI.Styling;
using System.Text;

namespace XenoAtom.Terminal.UI.Controls;

/// <summary>
/// Represents a virtualized, scrollable feed of rich documents.
/// </summary>
public sealed partial class DocumentFlow : Visual, IScrollable
{
    private readonly ScrollViewer _scrollViewer;
    private readonly DocumentFlowContentVisual _content;
    private readonly BindableList<DocumentFlowItem> _items;

    private int _lastItemsVersion = -1;
    private int _lastItemCount;
    private bool _pendingFollowTailScroll;

    /// <summary>
    /// Initializes a new instance of the <see cref="DocumentFlow"/> class.
    /// </summary>
    public DocumentFlow()
    {
        Focusable = true;
        HorizontalAlignment = Align.Stretch;
        VerticalAlignment = Align.Stretch;

        _items = new BindableList<DocumentFlowItem>(this, "DocumentFlow.Items");
        _content = new DocumentFlowContentVisual(this);
        _scrollViewer = new ScrollViewer(_content, focusable: false);

        this.ItemPadding(new Thickness(1));
        this.ItemSpacing(1);
        FollowTail = true;

        AttachChild(_scrollViewer);
    }

    /// <inheritdoc />
    protected override int ChildrenCount => 1;

    /// <inheritdoc />
    protected override Visual GetChild(int index)
        => index == 0 ? _scrollViewer : throw new ArgumentOutOfRangeException(nameof(index));

    /// <summary>
    /// Gets the item collection.
    /// </summary>
    [Bindable]
    public BindableList<DocumentFlowItem> Items => _items;

    /// <inheritdoc />
    public ScrollModel Scroll => _content.Scroll;

    /// <summary>
    /// Gets or sets the maximum number of items retained in <see cref="Items"/>.
    /// </summary>
    /// <remarks>Set to 0 to disable trimming.</remarks>
    [Bindable]
    public partial int MaxCapacity { get; set; }

    /// <summary>
    /// Gets or sets the default padding applied to item bubbles.
    /// </summary>
    [Bindable]
    public partial Thickness ItemPadding { get; set; }

    /// <summary>
    /// Gets or sets the spacing, in rows, between item bubbles.
    /// </summary>
    [Bindable]
    public partial int ItemSpacing { get; set; }

    /// <summary>
    /// Gets a value indicating whether the view follows the tail when new items are appended.
    /// </summary>
    public bool FollowTail { get; private set; }

    /// <summary>
    /// Scrolls to the tail and enables follow-tail mode.
    /// </summary>
    public void ScrollToTail()
    {
        VerifyAccess();
        FollowTail = true;
        _pendingFollowTailScroll = true;
        ApplyFollowTailIfNeeded();
    }

    partial void OnMaxCapacityChanging(ref int value) => ArgumentOutOfRangeException.ThrowIfNegative(value);

    partial void OnItemSpacingChanging(ref int value) => ArgumentOutOfRangeException.ThrowIfNegative(value);

    /// <inheritdoc />
    protected override void PrepareChildren()
    {
        TrimToCapacity();

        var itemsVersion = _items.Version;
        if (itemsVersion == _lastItemsVersion)
        {
            return;
        }

        var itemCount = _items.Count;
        if (FollowTail && itemCount > _lastItemCount)
        {
            _pendingFollowTailScroll = true;
        }

        _lastItemsVersion = itemsVersion;
        _lastItemCount = itemCount;
    }

    /// <inheritdoc />
    protected override SizeHints MeasureCore(in LayoutConstraints constraints)
    {
        _scrollViewer.Measure(constraints);

        var min = constraints.Clamp(new Size(4, 1));
        var natural = constraints.Clamp(_scrollViewer.DesiredSize);
        var max = new Size(LayoutConstants.Infinite, LayoutConstants.Infinite);
        return SizeHints.Flex(
            min,
            natural,
            max,
            growX: HorizontalAlignment == Align.Stretch ? 1 : 0,
            growY: VerticalAlignment == Align.Stretch ? 1 : 0,
            shrinkX: 1,
            shrinkY: 1);
    }

    /// <inheritdoc />
    protected override void ArrangeCore(in Rectangle finalRect)
    {
        _scrollViewer.Arrange(finalRect);
        if (ApplyFollowTailIfNeeded())
        {
            _scrollViewer.Arrange(finalRect);
        }

        UpdateFollowTailState();
    }

    /// <inheritdoc />
    protected override void OnKeyDown(KeyEventArgs e)
    {
        var viewportHeight = Math.Max(1, _scrollViewer.ViewportHeight);
        var maxVerticalOffset = Math.Max(0, _content.ExtentHeight - viewportHeight);
        var page = Math.Max(1, viewportHeight - 1);

        switch (e.Key)
        {
            case TerminalKey.Up:
                _scrollViewer.VerticalOffset = Math.Max(0, _scrollViewer.VerticalOffset - 1);
                FollowTail = false;
                e.Handled = true;
                return;
            case TerminalKey.Down:
                _scrollViewer.VerticalOffset = Math.Min(maxVerticalOffset, _scrollViewer.VerticalOffset + 1);
                FollowTail = _scrollViewer.VerticalOffset >= maxVerticalOffset;
                e.Handled = true;
                return;
            case TerminalKey.PageUp:
                _scrollViewer.VerticalOffset = Math.Max(0, _scrollViewer.VerticalOffset - page);
                FollowTail = false;
                e.Handled = true;
                return;
            case TerminalKey.PageDown:
                _scrollViewer.VerticalOffset = Math.Min(maxVerticalOffset, _scrollViewer.VerticalOffset + page);
                FollowTail = _scrollViewer.VerticalOffset >= maxVerticalOffset;
                e.Handled = true;
                return;
            case TerminalKey.Home:
                _scrollViewer.VerticalOffset = 0;
                FollowTail = false;
                e.Handled = true;
                return;
            case TerminalKey.End:
                _scrollViewer.VerticalOffset = maxVerticalOffset;
                FollowTail = true;
                e.Handled = true;
                return;
        }
    }

    /// <inheritdoc />
    protected override void OnPointerWheel(PointerEventArgs e)
    {
        if (e.RoutingPhase != RoutingPhase.Bubble || e.Kind != TerminalMouseKind.Wheel || e.WheelDelta == 0)
        {
            return;
        }

        var viewportHeight = Math.Max(1, _scrollViewer.ViewportHeight);
        var maxVerticalOffset = Math.Max(0, _content.ExtentHeight - viewportHeight);
        if (maxVerticalOffset == 0)
        {
            return;
        }

        var step = Math.Max(1, Math.Abs(e.WheelDelta));
        var nextOffset = e.WheelDelta > 0
            ? Math.Max(0, _scrollViewer.VerticalOffset - step)
            : Math.Min(maxVerticalOffset, _scrollViewer.VerticalOffset + step);

        if (nextOffset == _scrollViewer.VerticalOffset)
        {
            return;
        }

        _scrollViewer.VerticalOffset = nextOffset;
        FollowTail = nextOffset >= maxVerticalOffset;
        e.Handled = true;
    }

    private bool ApplyFollowTailIfNeeded()
    {
        if (!_pendingFollowTailScroll)
        {
            return false;
        }

        var viewportHeight = _scrollViewer.ViewportHeight;
        if (viewportHeight <= 0)
        {
            return false;
        }

        var target = Math.Max(0, _content.ExtentHeight - viewportHeight);
        _scrollViewer.VerticalOffset = target;
        _pendingFollowTailScroll = false;
        return true;
    }

    private void UpdateFollowTailState()
    {
        var viewportHeight = Math.Max(1, _scrollViewer.ViewportHeight);
        var maxVerticalOffset = Math.Max(0, _content.ExtentHeight - viewportHeight);
        FollowTail = _scrollViewer.VerticalOffset >= maxVerticalOffset;
    }

    private void TrimToCapacity()
    {
        var capacity = MaxCapacity;
        if (capacity <= 0 || _items.Count <= capacity)
        {
            return;
        }

        var removeCount = _items.Count - capacity;
        var preserveViewport = !FollowTail;
        var removedHeight = preserveViewport ? _content.GetHeadHeight(removeCount) : 0;

        for (var i = 0; i < removeCount; i++)
        {
            _items.RemoveAt(0);
        }

        if (preserveViewport && removedHeight > 0)
        {
            var target = Math.Max(0, _content.Scroll.OffsetY - removedHeight);
            _content.Scroll.SetOffset(_content.Scroll.OffsetX, target);
        }
    }

    private sealed class DocumentFlowContentVisual : Visual, IScrollable
    {
        private static readonly object DefaultReuseKey = new();

        private readonly DocumentFlow _owner;
        private readonly ScrollModel _scroll;
        private readonly BindableList<Visual> _activeChildren;
        private readonly Dictionary<long, ActiveBlockVisual> _activeBlocks;
        private readonly Dictionary<object, Stack<Visual>> _recyclePool;
        private readonly List<long> _staleKeys;
        private readonly List<DocumentLayout> _documentLayouts;

        private int[] _documentOffsets = Array.Empty<int>();
        private int _layoutWidth = -1;
        private int _layoutItemsVersion = -1;
        private bool _layoutCacheValid;
        private int _arrangeGeneration;
        private int _extentHeight;

        public DocumentFlowContentVisual(DocumentFlow owner)
        {
            ArgumentNullException.ThrowIfNull(owner);
            _owner = owner;
            _scroll = new ScrollModel(this);
            _activeChildren = new BindableList<Visual>(
                this,
                "DocumentFlow.ActiveChildren",
                onAdding: AttachCollectionChild,
                onRemoving: DetachCollectionChild);
            _activeBlocks = new Dictionary<long, ActiveBlockVisual>();
            _recyclePool = new Dictionary<object, Stack<Visual>>();
            _staleKeys = new List<long>();
            _documentLayouts = new List<DocumentLayout>();
        }

        public ScrollModel Scroll => _scroll;

        public int ExtentHeight => _extentHeight;

        protected override int ChildrenCount => _activeChildren.Count;

        protected override Visual GetChild(int index) => _activeChildren[index];

        public int GetHeadHeight(int itemCount)
        {
            if (itemCount <= 0 || _documentLayouts.Count == 0)
            {
                return 0;
            }

            var maxCount = Math.Min(itemCount, _documentLayouts.Count);
            var height = 0;
            for (var i = 0; i < maxCount; i++)
            {
                height += _documentLayouts[i].TotalHeight;
                if (i + 1 < _documentLayouts.Count)
                {
                    height += Math.Max(0, _owner.ItemSpacing);
                }
            }

            return Math.Max(0, height);
        }

        protected override SizeHints MeasureCore(in LayoutConstraints constraints)
        {
            var width = constraints.MaxWidth == LayoutConstants.Infinite ? LayoutConstants.MaxFinite : Math.Max(1, constraints.MaxWidth);
            EnsureLayouts(width);

            var desired = constraints.Clamp(new Size(
                Math.Max(1, width),
                Math.Max(1, _extentHeight)));

            return SizeHints.Flex(
                min: new Size(1, 1),
                natural: desired,
                max: new Size(LayoutConstants.Infinite, LayoutConstants.Infinite),
                growX: 1,
                growY: 1,
                shrinkX: 1,
                shrinkY: 1);
        }

        protected override void ArrangeCore(in Rectangle finalRect)
        {
            if (finalRect.Width <= 0 || finalRect.Height <= 0)
            {
                _scroll.SetViewport(0, 0);
                _scroll.SetExtent(0, 0);
                RecycleAllActiveBlocks();
                return;
            }

            EnsureLayouts(Math.Max(1, finalRect.Width));

            _scroll.SetViewport(finalRect.Width, finalRect.Height);
            _scroll.SetExtent(finalRect.Width, _extentHeight);

            RealizeVisibleBlocks(finalRect);
        }

        protected override void RenderOverride(CellBuffer buffer)
        {
            var rect = Bounds;
            if (rect.Width <= 0 || rect.Height <= 0)
            {
                return;
            }

            EnsureLayouts(Math.Max(1, rect.Width));
            if (_documentLayouts.Count == 0)
            {
                return;
            }

            var viewportTop = _scroll.OffsetY;
            var viewportBottom = viewportTop + rect.Height;
            var firstDocument = FindDocumentForRow(viewportTop);
            var lastDocument = FindDocumentForRow(Math.Max(viewportTop, viewportBottom - 1));
            if (firstDocument < 0 || lastDocument < firstDocument)
            {
                return;
            }

            var theme = GetTheme();
            var borderGlyphs = theme.Lines;

            for (var docIndex = firstDocument; docIndex <= lastDocument; docIndex++)
            {
                var layout = _documentLayouts[docIndex];
                var item = _owner._items[docIndex];

                var docTop = _documentOffsets[docIndex];
                var docY = rect.Y + (docTop - _scroll.OffsetY);
                var bubbleRect = ResolveBubbleRect(rect, item, layout.BubbleWidth, layout.TotalHeight, docY);
                if (!buffer.ClipIntersects(bubbleRect))
                {
                    continue;
                }

                if (item.BackgroundStyle is { } backgroundStyle)
                {
                    FillRect(buffer, bubbleRect, backgroundStyle);
                }

                if (item.BorderStyle is { } borderStyle)
                {
                    DrawBorder(buffer, bubbleRect, borderGlyphs, borderStyle);
                }
            }
        }

        private void EnsureLayouts(int viewportWidth)
        {
            viewportWidth = Math.Max(1, viewportWidth);
            var items = _owner._items;
            var itemsVersion = items.Version;

            if (_layoutCacheValid &&
                _layoutWidth == viewportWidth &&
                _layoutItemsVersion == itemsVersion &&
                _documentLayouts.Count == items.Count &&
                !HasDocumentVersionChanges(items, viewportWidth))
            {
                return;
            }

            _layoutWidth = viewportWidth;
            _layoutItemsVersion = itemsVersion;
            RebuildLayouts(items, viewportWidth);
            _layoutCacheValid = true;
        }

        private bool HasDocumentVersionChanges(BindableList<DocumentFlowItem> items, int viewportWidth)
        {
            if (_documentLayouts.Count != items.Count)
            {
                return true;
            }

            for (var index = 0; index < items.Count; index++)
            {
                var item = items[index];
                var content = item.Content;
                if (content is null)
                {
                    return true;
                }

                var layout = _documentLayouts[index];
                var bubbleWidth = ResolveBubbleWidth(item, viewportWidth);
                if (layout.ContentVersion != content.Version || layout.BubbleWidth != bubbleWidth || layout.Blocks.Length != content.BlockCount)
                {
                    return true;
                }

                for (var blockIndex = 0; blockIndex < layout.Blocks.Length; blockIndex++)
                {
                    var block = content.GetBlock(blockIndex);
                    var blockLayout = layout.Blocks[blockIndex];
                    if (!ReferenceEquals(blockLayout.Block, block) ||
                        blockLayout.Version != block.Version ||
                        blockLayout.MarginTop != Math.Max(0, block.MarginTop) ||
                        blockLayout.MarginBottom != Math.Max(0, block.MarginBottom))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private void RebuildLayouts(BindableList<DocumentFlowItem> items, int viewportWidth)
        {
            _documentLayouts.Clear();
            if (items.Count == 0)
            {
                _documentOffsets = Array.Empty<int>();
                _extentHeight = 0;
                RecycleAllActiveBlocks();
                return;
            }

            if (_documentOffsets.Length != items.Count + 1)
            {
                _documentOffsets = new int[items.Count + 1];
            }

            var spacing = Math.Max(0, _owner.ItemSpacing);
            _documentOffsets[0] = 0;

            var runningOffset = 0;
            for (var docIndex = 0; docIndex < items.Count; docIndex++)
            {
                var item = items[docIndex];
                if (item.Content is null)
                {
                    throw new InvalidOperationException("DocumentFlowItem.Content cannot be null.");
                }

                var layout = BuildDocumentLayout(item, docIndex, viewportWidth);
                _documentLayouts.Add(layout);

                runningOffset += layout.TotalHeight;
                if (docIndex + 1 < items.Count)
                {
                    runningOffset += spacing;
                }

                _documentOffsets[docIndex + 1] = runningOffset;
            }

            _extentHeight = Math.Max(0, runningOffset);
        }

        private DocumentLayout BuildDocumentLayout(DocumentFlowItem item, int docIndex, int viewportWidth)
        {
            var content = item.Content;
            var bubbleWidth = ResolveBubbleWidth(item, viewportWidth);
            var padding = item.Padding ?? _owner.ItemPadding;
            var innerWidth = Math.Max(1, bubbleWidth - padding.Horizontal);
            var blockCount = content.BlockCount;
            var blocks = blockCount == 0 ? Array.Empty<BlockLayout>() : new BlockLayout[blockCount];

            var currentY = Math.Max(0, padding.Top);
            for (var blockIndex = 0; blockIndex < blockCount; blockIndex++)
            {
                var block = content.GetBlock(blockIndex);
                var marginTop = Math.Max(0, block.MarginTop);
                var marginBottom = Math.Max(0, block.MarginBottom);
                currentY += marginTop;

                var blockHeight = MeasureBlockHeight(docIndex, blockIndex, block, innerWidth);
                var blockTop = currentY;
                currentY += blockHeight;
                var blockBottom = currentY;
                currentY += marginBottom;

                blocks[blockIndex] = new BlockLayout(
                    block,
                    block.Version,
                    Math.Max(1, blockHeight),
                    blockTop,
                    blockBottom,
                    marginTop,
                    marginBottom);
            }

            currentY += Math.Max(0, padding.Bottom);

            return new DocumentLayout
            {
                ContentVersion = content.Version,
                BubbleWidth = bubbleWidth,
                TotalHeight = Math.Max(1, currentY),
                Blocks = blocks,
            };
        }

        private int MeasureBlockHeight(int docIndex, int blockIndex, DocumentFlowBlock block, int width)
        {
            var key = MakeBlockKey(docIndex, blockIndex);
            if (_activeBlocks.TryGetValue(key, out var active) && ReferenceEquals(active.Block, block))
            {
                active.Visual.Measure(new LayoutConstraints(0, width, 0, LayoutConstants.Infinite));
                return Math.Max(1, active.Visual.DesiredSize.Height);
            }

            var reuseKey = NormalizeReuseKey(block.ReuseKey);
            var visual = AcquireRecycledOrCreate(block, reuseKey);
            visual.Measure(new LayoutConstraints(0, width, 0, LayoutConstants.Infinite));
            var measuredHeight = Math.Max(1, visual.DesiredSize.Height);
            block.Release(visual);
            StoreRecycledVisual(reuseKey, visual);
            return measuredHeight;
        }

        private void RealizeVisibleBlocks(in Rectangle rect)
        {
            if (_documentLayouts.Count == 0)
            {
                RecycleAllActiveBlocks();
                return;
            }

            _arrangeGeneration++;

            var viewportTop = _scroll.OffsetY;
            var viewportBottom = viewportTop + rect.Height;
            var firstDocument = FindDocumentForRow(viewportTop);
            var lastDocument = FindDocumentForRow(Math.Max(viewportTop, viewportBottom - 1));
            if (firstDocument < 0 || lastDocument < firstDocument)
            {
                RecycleStaleActiveBlocks();
                return;
            }

            for (var docIndex = firstDocument; docIndex <= lastDocument; docIndex++)
            {
                var layout = _documentLayouts[docIndex];
                var item = _owner._items[docIndex];
                var padding = item.Padding ?? _owner.ItemPadding;

                var docTop = _documentOffsets[docIndex];
                var docY = rect.Y + (docTop - _scroll.OffsetY);
                var bubbleRect = ResolveBubbleRect(rect, item, layout.BubbleWidth, layout.TotalHeight, docY);

                var innerX = bubbleRect.X + padding.Left;
                var innerWidth = Math.Max(1, bubbleRect.Width - padding.Horizontal);
                var localViewportTop = viewportTop - docTop;
                var localViewportBottom = viewportBottom - docTop;

                var firstBlock = FindFirstVisibleBlock(layout.Blocks, localViewportTop);
                var lastBlock = FindLastVisibleBlock(layout.Blocks, localViewportBottom);
                if (firstBlock < 0 || lastBlock < firstBlock)
                {
                    continue;
                }

                for (var blockIndex = firstBlock; blockIndex <= lastBlock; blockIndex++)
                {
                    var blockLayout = layout.Blocks[blockIndex];
                    var blockRect = new Rectangle(
                        innerX,
                        docY + blockLayout.Top,
                        innerWidth,
                        blockLayout.Height);

                    if (!IntersectsVertically(blockRect, rect))
                    {
                        continue;
                    }

                    var requiresMeasure = false;
                    var key = MakeBlockKey(docIndex, blockIndex);
                    if (!_activeBlocks.TryGetValue(key, out var active) || !ReferenceEquals(active.Block, blockLayout.Block))
                    {
                        if (active is not null)
                        {
                            RecycleActiveBlock(key, active);
                        }

                        var reuseKey = NormalizeReuseKey(blockLayout.Block.ReuseKey);
                        var visual = AcquireRecycledOrCreate(blockLayout.Block, reuseKey);
                        active = new ActiveBlockVisual(visual, blockLayout.Block, reuseKey, blockLayout.Version);
                        _activeBlocks[key] = active;
                        _activeChildren.Add(visual);
                        requiresMeasure = true;
                    }
                    else if (active.BlockVersion != blockLayout.Version)
                    {
                        if (!blockLayout.Block.TryUpdate(active.Visual))
                        {
                            RecycleActiveBlock(key, active);

                            var reuseKey = NormalizeReuseKey(blockLayout.Block.ReuseKey);
                            var visual = AcquireRecycledOrCreate(blockLayout.Block, reuseKey);
                            active = new ActiveBlockVisual(visual, blockLayout.Block, reuseKey, blockLayout.Version);
                            _activeBlocks[key] = active;
                            _activeChildren.Add(visual);
                            requiresMeasure = true;
                        }
                        else
                        {
                            active.BlockVersion = blockLayout.Version;
                            requiresMeasure = true;
                        }
                    }

                    if (requiresMeasure)
                    {
                        active.Visual.Measure(new LayoutConstraints(0, blockRect.Width, 0, LayoutConstants.Infinite));
                    }

                    active.Generation = _arrangeGeneration;
                    active.Visual.Arrange(blockRect);
                }
            }

            RecycleStaleActiveBlocks();
        }

        private static bool IntersectsVertically(in Rectangle a, in Rectangle b)
        {
            var top = Math.Max(a.Y, b.Y);
            var bottom = Math.Min(a.Bottom, b.Bottom);
            return top < bottom;
        }

        private void RecycleStaleActiveBlocks()
        {
            _staleKeys.Clear();
            foreach (var pair in _activeBlocks)
            {
                if (pair.Value.Generation != _arrangeGeneration)
                {
                    _staleKeys.Add(pair.Key);
                }
            }

            for (var i = 0; i < _staleKeys.Count; i++)
            {
                var key = _staleKeys[i];
                if (_activeBlocks.TryGetValue(key, out var active))
                {
                    RecycleActiveBlock(key, active);
                }
            }
        }

        private void RecycleAllActiveBlocks()
        {
            _staleKeys.Clear();
            foreach (var key in _activeBlocks.Keys)
            {
                _staleKeys.Add(key);
            }

            for (var i = 0; i < _staleKeys.Count; i++)
            {
                var key = _staleKeys[i];
                if (_activeBlocks.TryGetValue(key, out var active))
                {
                    RecycleActiveBlock(key, active);
                }
            }
        }

        private void RecycleActiveBlock(long key, ActiveBlockVisual active)
        {
            active.Block.Release(active.Visual);
            StoreRecycledVisual(active.ReuseKey, active.Visual);
            _activeChildren.Remove(active.Visual);
            _activeBlocks.Remove(key);
        }

        private Visual AcquireRecycledOrCreate(DocumentFlowBlock block, object reuseKey)
        {
            while (TryTakeRecycledVisual(reuseKey, out var recycled))
            {
                if (block.TryUpdate(recycled))
                {
                    return recycled;
                }
            }

            return block.CreateVisual();
        }

        private bool TryTakeRecycledVisual(object reuseKey, out Visual visual)
        {
            if (_recyclePool.TryGetValue(reuseKey, out var stack) && stack.Count > 0)
            {
                visual = stack.Pop();
                if (stack.Count == 0)
                {
                    _recyclePool.Remove(reuseKey);
                }

                return true;
            }

            visual = null!;
            return false;
        }

        private void StoreRecycledVisual(object reuseKey, Visual visual)
        {
            if (!_recyclePool.TryGetValue(reuseKey, out var stack))
            {
                stack = new Stack<Visual>();
                _recyclePool.Add(reuseKey, stack);
            }

            stack.Push(visual);
        }

        private static object NormalizeReuseKey(object? reuseKey) => reuseKey ?? DefaultReuseKey;

        private static long MakeBlockKey(int documentIndex, int blockIndex)
            => ((long)documentIndex << 32) | (uint)blockIndex;

        private int ResolveBubbleWidth(DocumentFlowItem item, int viewportWidth)
        {
            viewportWidth = Math.Max(1, viewportWidth);
            if (item.Alignment == DocumentFlowAlignment.Stretch)
            {
                return viewportWidth;
            }

            var maxWidth = viewportWidth;
            if (TryResolveWidthFromPercent(item.MaxWidthPercent, viewportWidth, out var percentWidth))
            {
                maxWidth = Math.Min(maxWidth, percentWidth);
            }

            var absoluteMaxWidth = item.MaxWidth.GetValueOrDefault(viewportWidth);
            if (absoluteMaxWidth > 0)
            {
                maxWidth = Math.Min(maxWidth, absoluteMaxWidth);
            }

            return Math.Clamp(maxWidth, 1, viewportWidth);
        }

        private static bool TryResolveWidthFromPercent(double? maxWidthPercent, int viewportWidth, out int width)
        {
            width = viewportWidth;
            if (!maxWidthPercent.HasValue)
            {
                return false;
            }

            var percent = maxWidthPercent.Value;
            if (double.IsNaN(percent) || double.IsInfinity(percent) || percent <= 0d)
            {
                return false;
            }

            var clampedPercent = Math.Min(100d, percent);
            width = (int)Math.Floor(viewportWidth * (clampedPercent / 100d));
            width = Math.Clamp(width, 1, viewportWidth);
            return true;
        }

        private static Rectangle ResolveBubbleRect(in Rectangle viewportRect, DocumentFlowItem item, int bubbleWidth, int bubbleHeight, int y)
        {
            bubbleWidth = Math.Clamp(bubbleWidth, 1, Math.Max(1, viewportRect.Width));
            bubbleHeight = Math.Max(1, bubbleHeight);

            var x = viewportRect.X;
            switch (item.Alignment)
            {
                case DocumentFlowAlignment.Right:
                    x = viewportRect.Right - bubbleWidth;
                    break;
                case DocumentFlowAlignment.Center:
                    x = viewportRect.X + ((viewportRect.Width - bubbleWidth) / 2);
                    break;
                case DocumentFlowAlignment.Stretch:
                    x = viewportRect.X;
                    bubbleWidth = viewportRect.Width;
                    break;
            }

            return new Rectangle(x, y, bubbleWidth, bubbleHeight);
        }

        private int FindDocumentForRow(int row)
        {
            var count = _documentLayouts.Count;
            if (count == 0)
            {
                return -1;
            }

            if (row <= 0)
            {
                return 0;
            }

            if (row >= _documentOffsets[count])
            {
                return count - 1;
            }

            var lo = 0;
            var hi = count;
            while (lo < hi)
            {
                var mid = (lo + hi) >> 1;
                if (_documentOffsets[mid] <= row)
                {
                    lo = mid + 1;
                }
                else
                {
                    hi = mid;
                }
            }

            return Math.Max(0, lo - 1);
        }

        private static int FindFirstVisibleBlock(BlockLayout[] blocks, int localViewportTop)
        {
            if (blocks.Length == 0)
            {
                return -1;
            }

            var lo = 0;
            var hi = blocks.Length;
            while (lo < hi)
            {
                var mid = (lo + hi) >> 1;
                if (blocks[mid].Bottom <= localViewportTop)
                {
                    lo = mid + 1;
                }
                else
                {
                    hi = mid;
                }
            }

            return lo < blocks.Length ? lo : -1;
        }

        private static int FindLastVisibleBlock(BlockLayout[] blocks, int localViewportBottom)
        {
            if (blocks.Length == 0)
            {
                return -1;
            }

            var lo = 0;
            var hi = blocks.Length;
            while (lo < hi)
            {
                var mid = (lo + hi) >> 1;
                if (blocks[mid].Top < localViewportBottom)
                {
                    lo = mid + 1;
                }
                else
                {
                    hi = mid;
                }
            }

            var index = lo - 1;
            return index >= 0 ? index : -1;
        }

        private static void FillRect(CellBuffer buffer, in Rectangle rect, Style style)
        {
            if (!buffer.ClipIntersects(rect))
            {
                return;
            }

            for (var y = rect.Y; y < rect.Bottom; y++)
            {
                for (var x = rect.X; x < rect.Right; x++)
                {
                    buffer.SetCell(x, y, new Rune(' '), style);
                }
            }
        }

        private static void DrawBorder(CellBuffer buffer, in Rectangle rect, LineGlyphs glyphs, Style style)
        {
            if (rect.Width <= 0 || rect.Height <= 0 || !buffer.ClipIntersects(rect))
            {
                return;
            }

            var left = rect.X;
            var right = rect.Right - 1;
            var top = rect.Y;
            var bottom = rect.Bottom - 1;

            if (rect.Width == 1)
            {
                for (var y = top; y <= bottom; y++)
                {
                    buffer.SetCell(left, y, glyphs.Vertical, style);
                }

                return;
            }

            if (rect.Height == 1)
            {
                for (var x = left; x <= right; x++)
                {
                    buffer.SetCell(x, top, glyphs.Horizontal, style);
                }

                return;
            }

            buffer.SetCell(left, top, glyphs.TopLeft, style);
            buffer.SetCell(right, top, glyphs.TopRight, style);
            buffer.SetCell(left, bottom, glyphs.BottomLeft, style);
            buffer.SetCell(right, bottom, glyphs.BottomRight, style);

            for (var x = left + 1; x < right; x++)
            {
                buffer.SetCell(x, top, glyphs.Horizontal, style);
                buffer.SetCell(x, bottom, glyphs.Horizontal, style);
            }

            for (var y = top + 1; y < bottom; y++)
            {
                buffer.SetCell(left, y, glyphs.Vertical, style);
                buffer.SetCell(right, y, glyphs.Vertical, style);
            }
        }

        private sealed class ActiveBlockVisual
        {
            public ActiveBlockVisual(Visual visual, DocumentFlowBlock block, object reuseKey, int blockVersion)
            {
                Visual = visual;
                Block = block;
                ReuseKey = reuseKey;
                BlockVersion = blockVersion;
            }

            public Visual Visual { get; }

            public DocumentFlowBlock Block { get; }

            public object ReuseKey { get; }

            public int BlockVersion { get; set; }

            public int Generation { get; set; }
        }

        private sealed class DocumentLayout
        {
            public int ContentVersion { get; init; }

            public int BubbleWidth { get; init; }

            public int TotalHeight { get; init; }

            public BlockLayout[] Blocks { get; init; } = Array.Empty<BlockLayout>();
        }

        private readonly record struct BlockLayout(
            DocumentFlowBlock Block,
            int Version,
            int Height,
            int Top,
            int Bottom,
            int MarginTop,
            int MarginBottom);
    }
}
