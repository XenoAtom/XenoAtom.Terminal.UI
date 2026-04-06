// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Reflection;
using XenoAtom.Terminal.UI.Commands;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Hosting;
using XenoAtom.Terminal.UI.Input;
using XenoAtom.Terminal.UI.Layout;
using XenoAtom.Terminal.UI.Rendering;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class CommandShortcutTests
{
    [TestMethod]
    public void CommandSequence_Executes_On_MultiStroke_Shortcut()
    {
        var probe = new CommandProbe();
        var root = new VStack { probe };

        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Inline, new TerminalSize(40, 10));
        driver.Tick();

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Unknown, Char = TerminalChar.CtrlK, Modifiers = TerminalModifiers.Ctrl });
        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Unknown, Char = TerminalChar.CtrlP, Modifiers = TerminalModifiers.Ctrl });
        driver.Tick();

        Assert.AreEqual(1, probe.Count);
    }

    [TestMethod]
    public void CommandSequence_Is_Canceled_When_Focus_Changes()
    {
        var probe1 = new CommandProbe();
        var probe2 = new CommandProbe();

        var root = new VStack { probe1, probe2 };

        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Inline, new TerminalSize(40, 10));
        driver.Tick();

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Unknown, Char = TerminalChar.CtrlK, Modifiers = TerminalModifiers.Ctrl });
        driver.Tick();

        driver.App.Focus(probe2);
        driver.Tick();

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Unknown, Char = TerminalChar.CtrlP, Modifiers = TerminalModifiers.Ctrl });
        driver.Tick();

        Assert.AreEqual(0, probe1.Count);
        Assert.AreEqual(0, probe2.Count);
    }

    [TestMethod]
    public void CommandSequence_TimesOut_When_Not_Completed()
    {
        var probe = new CommandProbe();
        var root = new VStack { probe };

        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Inline, new TerminalSize(40, 10));
        driver.Tick();

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Unknown, Char = TerminalChar.CtrlK, Modifiers = TerminalModifiers.Ctrl });
        driver.Tick();

        // The command shortcut timeout is 1.5s, and the driver tick step is ~10ms.
        driver.Tick(200);

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Unknown, Char = TerminalChar.CtrlP, Modifiers = TerminalModifiers.Ctrl });
        driver.Tick();

        Assert.AreEqual(0, probe.Count);
    }

    [TestMethod]
    public void CommandSequence_Throws_When_Prefix_Conflicts_With_Standalone_Command()
    {
        var probe = new EmptyProbe();

        probe.AddCommand(new Command
        {
            Id = "standalone",
            LabelMarkup = "Standalone",
            Gesture = new XenoAtom.Terminal.UI.Input.KeyGesture(TerminalChar.CtrlK, TerminalModifiers.Ctrl),
            Execute = _ => { },
        });

        Assert.Throws<InvalidOperationException>(() =>
        {
            probe.AddCommand(new Command
            {
                Id = "sequence",
                LabelMarkup = "Sequence",
                Sequence = new KeySequence(
                    new XenoAtom.Terminal.UI.Input.KeyGesture(TerminalChar.CtrlK, TerminalModifiers.Ctrl),
                    new XenoAtom.Terminal.UI.Input.KeyGesture(TerminalChar.CtrlP, TerminalModifiers.Ctrl)),
                Execute = _ => { },
            });
        });
    }

    [TestMethod]
    public void Disabled_Command_Consumes_Gesture_By_Default()
    {
        var probe = new FallbackGestureProbe(allowFallthrough: false);
        var root = new VStack { probe };

        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(40, 10));
        driver.Tick();

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Escape });
        driver.Tick();

        Assert.AreEqual(0, probe.PrimaryCount);
        Assert.AreEqual(0, probe.FallbackCount);
    }

    [TestMethod]
    public void Disabled_Command_Can_Allow_Gesture_Fallthrough()
    {
        var probe = new FallbackGestureProbe(allowFallthrough: true);
        var root = new VStack { probe };

        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(40, 10));
        driver.Tick();

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Escape });
        driver.Tick();

        Assert.AreEqual(0, probe.PrimaryCount);
        Assert.AreEqual(1, probe.FallbackCount);
    }

    [TestMethod]
    public void Replacing_Default_Quit_Command_Updates_Runtime_Gesture_Handling()
    {
        var root = new EmptyProbe();
        var invoked = false;

        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(40, 10));
        driver.Tick();

        driver.App.AddGlobalCommand(new Command
        {
            Id = TerminalApp.DefaultQuitCommandId,
            LabelMarkup = "Leave",
            Gesture = new XenoAtom.Terminal.UI.Input.KeyGesture(TerminalKey.F4),
            Execute = _ => invoked = true,
        });

        driver.Tick();

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Unknown, Char = TerminalChar.CtrlQ, Modifiers = TerminalModifiers.Ctrl });
        driver.Tick();

        Assert.IsFalse(IsStopRequested(driver.App), "Replacing TerminalApp.Quit should disable the original built-in exit gesture.");
        Assert.IsFalse(invoked, "The replacement exit command should not run for the old gesture.");

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.F4 });
        driver.Tick();

        Assert.IsTrue(invoked, "The replacement exit command should run for its new gesture.");
    }

    [TestMethod]
    public void Removing_Then_Readding_Default_Quit_Command_Updates_Runtime_Gesture_Handling()
    {
        var root = new EmptyProbe();
        var invoked = false;

        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(40, 10));
        driver.Tick();

        Assert.IsTrue(driver.App.RemoveGlobalCommand(TerminalApp.DefaultQuitCommandId), "Expected the default quit command to be registered.");

        driver.App.AddGlobalCommand(new Command
        {
            Id = TerminalApp.DefaultQuitCommandId,
            LabelMarkup = "Leave",
            Gesture = new XenoAtom.Terminal.UI.Input.KeyGesture(TerminalKey.F4),
            Execute = _ => invoked = true,
        });

        driver.Tick();

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Unknown, Char = TerminalChar.CtrlQ, Modifiers = TerminalModifiers.Ctrl });
        driver.Tick();

        Assert.IsFalse(IsStopRequested(driver.App), "Removing and re-adding TerminalApp.Quit should disable the original built-in exit gesture.");
        Assert.IsFalse(invoked, "The re-registered quit command should not run for the old gesture.");

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.F4 });
        driver.Tick();

        Assert.IsTrue(invoked, "The re-registered quit command should run for its new gesture.");
    }

    private static bool IsStopRequested(TerminalApp app)
    {
        var ctsField = typeof(TerminalApp).GetField("_cts", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(ctsField, "Expected TerminalApp to expose its cancellation token source field for tests.");
        var cts = (CancellationTokenSource?)ctsField.GetValue(app);
        Assert.IsNotNull(cts, "Expected TerminalApp to initialize its cancellation token source.");
        return cts.IsCancellationRequested;
    }

    private sealed class EmptyProbe : Visual
    {
        protected override SizeHints MeasureCore(in LayoutConstraints constraints) => SizeHints.Fixed(constraints.Clamp(new Size(1, 1)));

        protected override void RenderOverride(CellBuffer buffer)
        {
        }
    }

    private sealed class CommandProbe : Visual
    {
        public int Count { get; private set; }

        public CommandProbe()
        {
            Focusable = true;

            AddCommand(new Command
            {
                Id = "probe",
                LabelMarkup = "Probe",
                Sequence = new KeySequence(
                    new XenoAtom.Terminal.UI.Input.KeyGesture(TerminalChar.CtrlK, TerminalModifiers.Ctrl),
                    new XenoAtom.Terminal.UI.Input.KeyGesture(TerminalChar.CtrlP, TerminalModifiers.Ctrl)),
                Execute = _ => Count++,
            });
        }

        protected override SizeHints MeasureCore(in LayoutConstraints constraints) => SizeHints.Fixed(constraints.Clamp(new Size(10, 1)));

        protected override void RenderOverride(CellBuffer buffer)
        {
            buffer.WriteText(Bounds.X, Bounds.Y, $"Count:{Count}".AsSpan(), Style.None);
        }
    }

    private sealed class FallbackGestureProbe : Visual
    {
        public int PrimaryCount { get; private set; }

        public int FallbackCount { get; private set; }

        public FallbackGestureProbe(bool allowFallthrough)
        {
            Focusable = true;

            AddCommand(new Command
            {
                Id = "primary",
                LabelMarkup = "Primary",
                Gesture = new XenoAtom.Terminal.UI.Input.KeyGesture(TerminalKey.Escape),
                CanExecute = _ => false,
                ConsumesGestureWhenUnavailable = allowFallthrough ? false : true,
                Execute = _ => PrimaryCount++,
            });

            AddCommand(new Command
            {
                Id = "fallback",
                LabelMarkup = "Fallback",
                Gesture = new XenoAtom.Terminal.UI.Input.KeyGesture(TerminalKey.Escape),
                Execute = _ => FallbackCount++,
            });
        }

        protected override SizeHints MeasureCore(in LayoutConstraints constraints) => SizeHints.Fixed(constraints.Clamp(new Size(10, 1)));

        protected override void RenderOverride(CellBuffer buffer)
        {
            buffer.WriteText(Bounds.X, Bounds.Y, $"P:{PrimaryCount} F:{FallbackCount}".AsSpan(), Style.None);
        }
    }
}
