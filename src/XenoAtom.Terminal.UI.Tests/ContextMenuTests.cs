// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal;
using XenoAtom.Terminal.Backends;
using XenoAtom.Terminal.UI.Commands;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Hosting;
using XenoAtom.Terminal.UI.Input;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class ContextMenuTests
{
    [TestMethod]
    public void ContextMenu_RightClick_UsesFactoryWhenProvided()
    {
        var invoked = false;

        var target = new Button("Target");
        target.ContextMenuFactory = _ => new[]
        {
            new MenuItem("Copy", () => invoked = true),
        };

        var root = new VStack { target };
        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(50, 12));
        driver.Tick();

        var x = target.Bounds.X + 1;
        var y = target.Bounds.Y;

        driver.Backend.PushEvent(new TerminalMouseEvent
        {
            Kind = TerminalMouseKind.Down,
            Button = TerminalMouseButton.Right,
            X = x,
            Y = y,
        });
        driver.Tick();

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Enter });
        driver.TickUntil(() => invoked);
    }

    [TestMethod]
    public void ContextMenu_RightClick_FallsBackToCommandDiscovery()
    {
        Visual? executedTarget = null;

        var target = new Button("Target");
        target.AddCommand(new Command
        {
            Id = "Test.ContextMenuCommand",
            LabelMarkup = "Run",
            Presentation = CommandPresentation.ContextMenu,
            Execute = v => executedTarget = v,
        });

        var root = new VStack { target };
        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(50, 12));
        driver.Tick();

        var x = target.Bounds.X + 1;
        var y = target.Bounds.Y;

        driver.Backend.PushEvent(new TerminalMouseEvent
        {
            Kind = TerminalMouseKind.Down,
            Button = TerminalMouseButton.Right,
            X = x,
            Y = y,
        });
        driver.Tick();

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Enter });
        driver.TickUntil(() => executedTarget is not null);

        Assert.AreSame(target, executedTarget);
    }
}

