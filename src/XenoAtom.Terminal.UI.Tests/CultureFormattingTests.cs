// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Globalization;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Hosting;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class CultureFormattingTests
{
    [TestMethod]
    public void DataPresenter_Uses_AppCulture_For_NumberFormatting()
    {
        var culture = CultureInfo.GetCultureInfo("fr-FR");
        var presenter = new DataPresenter<double>().Value(1.5);

        using var driver = new TerminalAppTestDriver(
            presenter,
            TerminalHostKind.Fullscreen,
            new TerminalSize(20, 5),
            new TerminalAppOptions { Culture = culture });

        driver.Tick();

        var screen = new AnsiTestScreen(20, 5);
        screen.Apply(driver.Backend.GetOutText());
        var rendered = screen.GetText();

        StringAssert.Contains(rendered, "1,5");
        Assert.IsFalse(rendered.Contains("1.5", StringComparison.Ordinal), "Formatting should respect the configured culture.");
    }

    [TestMethod]
    public void CultureStyle_Can_Override_AppCulture_For_Subtree()
    {
        var appCulture = CultureInfo.InvariantCulture;
        var subtreeCulture = CultureInfo.GetCultureInfo("fr-FR");

        var presenter = new DataPresenter<double>().Value(1.5);
        presenter.Style(CultureStyle.Default with { Culture = subtreeCulture });

        using var driver = new TerminalAppTestDriver(
            presenter,
            TerminalHostKind.Fullscreen,
            new TerminalSize(20, 5),
            new TerminalAppOptions { Culture = appCulture });

        driver.Tick();

        var screen = new AnsiTestScreen(20, 5);
        screen.Apply(driver.Backend.GetOutText());
        var rendered = screen.GetText();

        StringAssert.Contains(rendered, "1,5");
    }
}
