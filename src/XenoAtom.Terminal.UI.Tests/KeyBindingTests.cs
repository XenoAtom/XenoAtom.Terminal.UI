// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Hosting;
using XenoAtom.Terminal.UI.Input;
using XenoAtom.Terminal.UI.Layout;
using XenoAtom.Terminal.UI.Rendering;
using UiTerminalKeyGesture = XenoAtom.Terminal.UI.Input.TerminalKeyGesture;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class KeyBindingTests
{
    [TestMethod]
    public void KeyBinding_Executes_On_Ctrl_Gesture()
    {
        var probe = new KeyBindingProbe();
        var root = new VStack { probe };

        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Inline, new TerminalSize(40, 10));
        driver.Tick();

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Unknown, Char = TerminalChar.CtrlK, Modifiers = TerminalModifiers.Ctrl });
        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Unknown, Char = TerminalChar.CtrlK, Modifiers = TerminalModifiers.Ctrl });
        driver.Tick();

        Assert.AreEqual(2, probe.Count);
    }

    private sealed class KeyBindingProbe : Visual
    {
        public int Count { get; private set; }

        public KeyBindingProbe()
        {
            Focusable = true;
            AddKeyBinding(new UiTerminalKeyGesture(TerminalChar.CtrlK, TerminalModifiers.Ctrl), () => Count++);
        }

        protected override SizeHints MeasureCore(in LayoutConstraints constraints) => SizeHints.Fixed(constraints.Clamp(new Size(10, 1)));

        protected override void ArrangeCore(in Rectangle finalRect) => Bounds = finalRect;

        protected override void RenderOverride(CellBuffer buffer)
        {
            buffer.WriteText(Bounds.X, Bounds.Y, $"Count:{Count}".AsSpan(), CellStyle.None);
        }
    }
}
