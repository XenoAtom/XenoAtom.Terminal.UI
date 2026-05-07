// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.Backends;
using XenoAtom.Terminal;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Hosting;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class MarkupRenderingTests
{
    [TestMethod]
    public void Markup_Respects_NewLines()
    {
        var root = new Markup("[bold]Markup[/] supports inline styling:\n- [green]success[/]\n- [yellow]warning[/]")
            .HorizontalAlignment(Align.Stretch)
            .VerticalAlignment(Align.Stretch);

        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(60, 8));
        driver.Tick();

        var screen = new AnsiTestScreen(60, 8);
        screen.Apply(driver.Backend.GetOutText());
        var rows = screen.GetText().Split(Environment.NewLine);

        Assert.IsTrue(rows[0].Contains("Markup supports inline styling:", StringComparison.Ordinal));
        Assert.IsFalse(rows[0].Contains("success", StringComparison.Ordinal));
        Assert.IsTrue(rows[1].Contains("- success", StringComparison.Ordinal));
        Assert.IsTrue(rows[2].Contains("- warning", StringComparison.Ordinal));
    }

    [TestMethod]
    public void Markup_Computed_Text_Can_Clear_Selection()
    {
        var text = new State<string>("hello world");
        var markup = new Markup(() => text.Value).HorizontalAlignment(Align.Stretch);

        using var driver = new TerminalAppTestDriver(markup, TerminalHostKind.Fullscreen, new TerminalSize(30, 4));
        driver.Tick();

        var y = markup.Bounds.Y;
        driver.Backend.PushEvent(new TerminalMouseEvent
        {
            Kind = TerminalMouseKind.Down,
            Button = TerminalMouseButton.Left,
            X = markup.Bounds.X,
            Y = y,
        });
        driver.Backend.PushEvent(new TerminalMouseEvent
        {
            Kind = TerminalMouseKind.Drag,
            Button = TerminalMouseButton.Left,
            X = markup.Bounds.X + 5,
            Y = y,
        });
        driver.Backend.PushEvent(new TerminalMouseEvent
        {
            Kind = TerminalMouseKind.Up,
            Button = TerminalMouseButton.Left,
            X = markup.Bounds.X + 5,
            Y = y,
        });
        driver.Tick();

        text.Value = "updated";
        driver.Tick();

        driver.Terminal.Clipboard.Text = "seed";
        driver.Backend.PushEvent(new TerminalKeyEvent
        {
            Key = TerminalKey.Unknown,
            Char = TerminalChar.CtrlC,
            Modifiers = TerminalModifiers.Ctrl,
        });
        driver.Tick();

        Assert.AreEqual("seed", driver.Terminal.Clipboard.Text);
    }
}
