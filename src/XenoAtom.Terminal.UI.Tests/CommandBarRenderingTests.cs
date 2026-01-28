// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

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
}
