// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Hosting;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class SearchReplacePopupDragTests
{
    [TestMethod]
    public void SearchReplacePopup_CanBeRepositioned_By_Dragging()
    {
        var target = new DummyTarget();
        var popup = new SearchReplacePopup(target);
        var host = new AnchorHost(popup);

        using var driver = new TerminalAppTestDriver(host, TerminalHostKind.Fullscreen, new TerminalSize(50, 20));
        driver.Tick();

        // Default anchor: right edge, top row.
        Assert.AreEqual(50, popup.Bounds.X);
        Assert.AreEqual(0, popup.Bounds.Y);

        popup.BeginDrag(uiX: 50, uiY: 0);
        popup.UpdateDrag(uiX: 45, uiY: 2);
        popup.EndDrag();

        driver.Tick();

        Assert.AreEqual(45, popup.Bounds.X);
        Assert.AreEqual(2, popup.Bounds.Y);
    }

    private sealed class AnchorHost : Visual
    {
        private readonly SearchReplacePopup _popup;

        public AnchorHost(SearchReplacePopup popup)
        {
            Focusable = false;
            _popup = popup;
            AttachChild(popup);
        }

        protected override int ChildrenCount => 1;

        protected override Visual GetChild(int index) => index == 0 ? _popup : throw new ArgumentOutOfRangeException(nameof(index));

        protected override SizeHints MeasureCore(in LayoutConstraints constraints)
            => SizeHints.Fixed(constraints.Clamp(new Size(50, 20)));

        protected override void ArrangeCore(in Rectangle finalRect)
        {
            Bounds = finalRect;
            _popup.ArrangeWithin(finalRect);
        }
    }

    private sealed class DummyTarget : ISearchReplaceTarget
    {
        public string Title => "Target";

        public bool SupportsReplace => true;

        public void SetQuery(in SearchQuery query)
        {
        }

        public void NextMatch()
        {
        }

        public void PreviousMatch()
        {
        }

        public int ReplaceCurrent(string replacement) => 0;

        public int ReplaceAll(string replacement) => 0;

        public string GetStatusText() => string.Empty;

        public string? GetErrorText() => null;
    }
}

