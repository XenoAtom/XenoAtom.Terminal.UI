// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Hosting;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class ValidationPresenterTests
{
    [TestMethod]
    public void ValidationPresenter_Hides_Message_When_Null()
    {
        var editor = new TextBox("Hello");
        var presenter = new ValidationPresenter().Content(editor);
        var root = new VStack(presenter);

        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(30, 10));
        driver.Tick();

        Assert.AreEqual(editor.DesiredSize.Height, presenter.DesiredSize.Height);
    }

    [TestMethod]
    public void ValidationPresenter_Shows_Message_Below_By_Default()
    {
        var editor = new TextBox("Hello");
        var presenter = new ValidationPresenter()
            .Content(editor)
            .Message(new ValidationMessage(ValidationSeverity.Error, "Invalid value"));

        var root = new VStack(presenter);

        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(30, 10));
        driver.Tick();

        Assert.IsGreaterThan(editor.DesiredSize.Height, presenter.DesiredSize.Height);
        Assert.AreEqual(0, editor.Bounds.Y);

        var screen = new AnsiTestScreen(30, 10);
        screen.Apply(driver.Backend.GetOutText());
        StringAssert.Contains(screen.GetText(), "Invalid value");
    }

    [TestMethod]
    public void ValidationPresenter_Can_Show_Message_Above()
    {
        var editor = new TextBox("Hello");
        var presenter = new ValidationPresenter()
            .Content(editor)
            .Placement(ValidationPlacement.Above)
            .Message(new ValidationMessage(ValidationSeverity.Warning, "Warning text"));

        var root = new VStack(presenter);

        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(30, 10));
        driver.Tick();

        Assert.IsGreaterThan(0, editor.Bounds.Y);

        var screen = new AnsiTestScreen(30, 10);
        screen.Apply(driver.Backend.GetOutText());
        StringAssert.Contains(screen.GetText(), "Warning text");
    }
}
