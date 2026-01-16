// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.Backends;
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
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .VerticalAlignment(VerticalAlignment.Stretch);

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
}
