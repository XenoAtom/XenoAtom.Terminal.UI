// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal;
using XenoAtom.Terminal.UI.Controls;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class ComputedVisualHitTestTests
{
    [TestMethod]
    public void ComputedVisual_DoesNotBlockHitTesting_When_ChildIsNull()
    {
        var showOverlay = new State<bool>(false);
        var clicked = new State<bool>(false);

        var underButton = new Button("Under").Click(() => clicked.Value = true);

        var closeButton = new Button("Close").Click(() => showOverlay.Value = false);
        var overlay = new ZStack(
            new Backdrop().IsEnabled(false),
            new Center().Content(closeButton));

        var root = new ZStack(
                underButton,
                new ComputedVisual(() => showOverlay.Value ? overlay : null))
            .HorizontalAlignment(Align.Stretch)
            .VerticalAlignment(Align.Stretch);

        using var driver = new TerminalAppTestDriver(root, size: new TerminalSize(60, 15));
        driver.Tick(2);

        // Show the overlay and click Close (focus & capture path).
        showOverlay.Value = true;
        driver.Tick(2);

        driver.Backend.PushEvent(new TerminalMouseEvent
        {
            Kind = TerminalMouseKind.Down,
            Button = TerminalMouseButton.Left,
            X = closeButton.Bounds.X + 1,
            Y = closeButton.Bounds.Y,
        });
        driver.Backend.PushEvent(new TerminalMouseEvent
        {
            Kind = TerminalMouseKind.Up,
            Button = TerminalMouseButton.Left,
            X = closeButton.Bounds.X + 1,
            Y = closeButton.Bounds.Y,
        });
        driver.TickUntil(() => !showOverlay.Value);
        driver.Tick(2);

        // Now the overlay computed visual returns null; clicking the underlying button must work.
        driver.Backend.PushEvent(new TerminalMouseEvent
        {
            Kind = TerminalMouseKind.Down,
            Button = TerminalMouseButton.Left,
            X = underButton.Bounds.X + 1,
            Y = underButton.Bounds.Y,
        });
        driver.Backend.PushEvent(new TerminalMouseEvent
        {
            Kind = TerminalMouseKind.Up,
            Button = TerminalMouseButton.Left,
            X = underButton.Bounds.X + 1,
            Y = underButton.Bounds.Y,
        });
        driver.TickUntil(() => clicked.Value);
    }
}

