// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Hosting;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class TextEditorWordSelectionTests
{
    [TestMethod]
    public void TextBox_DoubleClick_Selects_Word()
    {
        var textBox = new TextBox();
        var root = new VStack { textBox };

        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(40, 5));
        driver.Tick();

        driver.Backend.PushEvent(new TerminalTextEvent { Text = "hello world" });
        driver.TickUntil(() => textBox.Text == "hello world");

        var x = textBox.Bounds.X + 1 + 6; // padding-left + start of "world"
        var y = textBox.Bounds.Y;
        driver.Backend.PushEvent(new TerminalMouseEvent
        {
            Kind = TerminalMouseKind.DoubleClick,
            Button = TerminalMouseButton.Left,
            X = x,
            Y = y,
        });

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Unknown, Char = TerminalChar.CtrlC, Modifiers = TerminalModifiers.Ctrl });
        driver.Tick();

        Assert.AreEqual("world", driver.Terminal.Clipboard.Text);
    }
}

