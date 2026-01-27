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
    private readonly record struct TextPosition(int X, int Y);

    [TestMethod]
    public void SearchReplacePopup_Repositions_Immediately_While_Dragging()
    {
        var target = new DummyTarget { SupportsReplace = false, Title = "DragMe" };
        var popup = new SearchReplacePopup(target);
        var host = new AnchorHost(popup);

        using var driver = new TerminalAppTestDriver(host, TerminalHostKind.Fullscreen, new TerminalSize(50, 12));
        driver.Tick();

        driver.App.Post(() => popup.OpenFind());
        driver.Tick();

        var initialPos = FindTextPosition(driver.Backend.GetOutText(), 50, 12, "DragMe");
        Assert.IsNotNull(initialPos);

        driver.Backend.PushEvent(new TerminalMouseEvent
        {
            Kind = TerminalMouseKind.Down,
            Button = TerminalMouseButton.Left,
            X = initialPos.Value.X,
            Y = initialPos.Value.Y,
        });
        driver.Backend.PushEvent(new TerminalMouseEvent
        {
            Kind = TerminalMouseKind.Drag,
            Button = TerminalMouseButton.Left,
            X = Math.Max(0, initialPos.Value.X - 5),
            Y = Math.Min(11, initialPos.Value.Y + 2),
        });
        driver.Backend.PushEvent(new TerminalMouseEvent
        {
            Kind = TerminalMouseKind.Up,
            Button = TerminalMouseButton.Left,
            X = Math.Max(0, initialPos.Value.X - 5),
            Y = Math.Min(11, initialPos.Value.Y + 2),
        });
        driver.Tick();

        var movedPos = FindTextPosition(driver.Backend.GetOutText(), 50, 12, "DragMe");
        Assert.IsNotNull(movedPos);
        Assert.AreNotEqual(initialPos.Value, movedPos.Value);
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
        public string Title { get; set; } = "Target";

        public bool SupportsReplace { get; set; } = true;

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

    private static TextPosition? FindTextPosition(string outText, int width, int height, string token)
    {
        var screen = new AnsiTestScreen(width, height);
        screen.Apply(outText);
        var rendered = screen.GetText();

        var lines = rendered.Split('\n');
        for (var y = 0; y < lines.Length; y++)
        {
            var line = lines[y];
            if (line.Length > 0 && line[^1] == '\r')
            {
                line = line[..^1];
            }

            var index = line.IndexOf(token, StringComparison.Ordinal);
            if (index >= 0)
            {
                return new TextPosition(index, y);
            }
        }

        return null;
    }

}
