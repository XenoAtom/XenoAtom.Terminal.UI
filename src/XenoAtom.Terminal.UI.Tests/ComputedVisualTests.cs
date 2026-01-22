// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Hosting;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class ComputedVisualTests
{
    [TestMethod]
    public void ComputedVisual_Rebuilds_On_BindingChange()
    {
        var model = new TextBox("A");
        var computed = new ComputedVisual(() => new TextBlock($"Computed:{model.Text}"));

        var root = new VStack { Spacing = 1 };
        root.Add(model);
        root.Add(computed);

        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Inline, new TerminalSize(40, 10));
        driver.Tick();

        model.Text = "B";
        driver.Tick();

        var outText = driver.Backend.GetOutText();
        StringAssert.Contains(outText, "Computed:A");

        var screen = new AnsiTestScreen(40, 10);
        screen.Apply(outText);
        var rendered = screen.GetText();
        StringAssert.Contains(rendered, "Computed:B");
    }

    [TestMethod]
    public void EnvironmentValue_Invalidates_ComputedVisual()
    {
        var key = new StyleKey<string>("Title", "A");

        ComputedVisual? view = null;
        view = new ComputedVisual(() => new TextBlock($"Env:{view!.Get(key)}"));

        var root = new VStack();
        root.Add(view);

        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Inline, new TerminalSize(40, 10));
        driver.Tick();

        root.SetStyle(key, "B");
        driver.Tick();

        var outText = driver.Backend.GetOutText();
        StringAssert.Contains(outText, "Env:A");

        var screen = new AnsiTestScreen(40, 10);
        screen.Apply(outText);
        var rendered = screen.GetText();
        StringAssert.Contains(rendered, "Env:B");
    }
}

