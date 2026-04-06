// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Linq;
using XenoAtom.Terminal.UI.Commands;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Hosting;
using XenoAtom.Terminal.UI.Input;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class CommandBarRenderingTests
{
    [TestMethod]
    public void CommandBar_Renders_Local_And_Global_Commands()
    {
        var probe = new CommandProbe();
        var bar = new CommandBar();
        var layout = new DockLayout { Content = probe, Bottom = bar };

        using var driver = new TerminalAppTestDriver(layout, TerminalHostKind.Fullscreen, new TerminalSize(60, 6));
        driver.App.Focus(probe);

        driver.App.AddGlobalCommand(new Command
        {
            Id = "quit",
            LabelMarkup = "Quit",
            Gesture = new XenoAtom.Terminal.UI.Input.KeyGesture(TerminalChar.CtrlQ, TerminalModifiers.Ctrl),
            Execute = _ => { },
        });

        driver.Tick();

        var outText = driver.Backend.GetOutText();
        StringAssert.Contains(outText, "Ctrl+Q");
        StringAssert.Contains(outText, "Quit");
        StringAssert.Contains(outText, "Ctrl+K Ctrl+P");
        StringAssert.Contains(outText, "Probe");
    }

    [TestMethod]
    public void CommandBar_Default_Mode_Remains_Single_Row_When_Commands_Do_Not_Fit()
    {
        var probe = new WrappingProbe();
        var bar = new CommandBar();
        var layout = new DockLayout { Content = probe, Bottom = bar };

        using var driver = new TerminalAppTestDriver(layout, TerminalHostKind.Fullscreen, new TerminalSize(20, 6));
        driver.App.Focus(probe);
        driver.Tick();

        Assert.AreEqual(1, bar.Bounds.Height);

        var lines = GetScreenLines(driver, 20, 6);
        var renderedCommandRows = lines.Count(static line => line.Contains("Alpha", StringComparison.Ordinal) || line.Contains("Beta", StringComparison.Ordinal) || line.Contains("Gamma", StringComparison.Ordinal));
        Assert.AreEqual(1, renderedCommandRows);
    }

    [TestMethod]
    public void CommandBar_MultiLine_Wraps_Commands_To_Additional_Rows()
    {
        var probe = new WrappingProbe();
        var bar = new CommandBar().MultiLine(true);
        var layout = new DockLayout { Content = probe, Bottom = bar };

        using var driver = new TerminalAppTestDriver(layout, TerminalHostKind.Fullscreen, new TerminalSize(20, 6));
        driver.App.Focus(probe);
        driver.Tick();

        Assert.IsTrue(bar.Bounds.Height > 1, $"Expected wrapped command bar to request multiple rows, actual height={bar.Bounds.Height}.");

        var lines = GetScreenLines(driver, 20, 6);
        var alphaRow = FindLine(lines, "Alpha");
        var betaRow = FindLine(lines, "Beta");
        var gammaRow = FindLine(lines, "Gamma");

        Assert.IsTrue(alphaRow >= 0, "Expected Alpha to render.");
        Assert.IsTrue(betaRow >= 0, "Expected Beta to render.");
        Assert.IsTrue(gammaRow >= 0, "Expected Gamma to render.");
        Assert.IsTrue(alphaRow != betaRow || betaRow != gammaRow, "Expected wrapped commands to span multiple rows.");
    }

    [TestMethod]
    public void CommandBar_Refreshes_When_Global_Command_Is_Replaced_After_Run_Starts()
    {
        var probe = new CommandProbe();
        var bar = new CommandBar();
        var layout = new DockLayout { Content = probe, Bottom = bar };

        using var driver = new TerminalAppTestDriver(layout, TerminalHostKind.Fullscreen, new TerminalSize(60, 6));
        driver.App.Focus(probe);
        driver.Tick();

        driver.App.AddGlobalCommand(new Command
        {
            Id = TerminalApp.DefaultQuitCommandId,
            LabelMarkup = "Leave",
            Gesture = new XenoAtom.Terminal.UI.Input.KeyGesture(TerminalKey.F4),
            Execute = _ => { },
        });

        driver.Tick();

        var rendered = string.Join('\n', GetScreenLines(driver, 60, 6));
        StringAssert.Contains(rendered, "F4");
        StringAssert.Contains(rendered, "Leave");
        Assert.IsFalse(rendered.Contains("Ctrl+Q", StringComparison.Ordinal), "The command bar should stop showing the original quit gesture after replacement.");
        Assert.IsFalse(rendered.Contains("Quit", StringComparison.Ordinal), "The command bar should stop showing the original quit label after replacement.");
    }

    private static string[] GetScreenLines(TerminalAppTestDriver driver, int width, int height)
    {
        var screen = new AnsiTestScreen(width, height);
        screen.Apply(driver.Backend.GetOutText());
        return screen.GetText().Split('\n');
    }

    private static int FindLine(string[] lines, string text)
    {
        for (var i = 0; i < lines.Length; i++)
        {
            if (lines[i].Contains(text, StringComparison.Ordinal))
            {
                return i;
            }
        }

        return -1;
    }

    private sealed class CommandProbe : Visual
    {
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
                Execute = _ => { },
            });
        }

        protected override SizeHints MeasureCore(in LayoutConstraints constraints) => SizeHints.Fixed(constraints.Clamp(new Geometry.Size(10, 1)));

        protected override void RenderOverride(Rendering.CellBuffer buffer)
        {
        }
    }

    private sealed class WrappingProbe : Visual
    {
        public WrappingProbe()
        {
            Focusable = true;
            AddCommand(new Command
            {
                Id = "alpha",
                LabelMarkup = "Alpha",
                Gesture = new KeyGesture(TerminalChar.CtrlA, TerminalModifiers.Ctrl),
                Execute = _ => { },
            });
            AddCommand(new Command
            {
                Id = "beta",
                LabelMarkup = "Beta",
                Gesture = new KeyGesture(TerminalChar.CtrlB, TerminalModifiers.Ctrl),
                Execute = _ => { },
            });
            AddCommand(new Command
            {
                Id = "gamma",
                LabelMarkup = "Gamma",
                Gesture = new KeyGesture(TerminalChar.CtrlG, TerminalModifiers.Ctrl),
                Execute = _ => { },
            });
        }

        protected override SizeHints MeasureCore(in LayoutConstraints constraints) => SizeHints.Fixed(constraints.Clamp(new Geometry.Size(10, 1)));

        protected override void RenderOverride(Rendering.CellBuffer buffer)
        {
        }
    }
}
