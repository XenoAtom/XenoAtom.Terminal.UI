// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Hosting;
using XenoAtom.Terminal.UI.Templating;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class DataPresenterTests
{
    [TestMethod]
    public void DataPresenter_Uses_Default_Display_Template_When_Value_Is_Bound()
    {
        var name = new State<string>("Alex");

        var presenter = name.PresentAs(DataTemplateRole.Display);

        using var driver = new TerminalAppTestDriver(new VStack { presenter }, TerminalHostKind.Fullscreen, new TerminalSize(40, 10));
        driver.Tick();

        Assert.AreEqual(1, presenter.GetChildrenCount());
        Assert.IsInstanceOfType<TextBlock>(presenter.GetChildUnsafe(0));

        var textBlock = (TextBlock)presenter.GetChildUnsafe(0);
        Assert.AreEqual("Alex", textBlock.Text);

        name.Value = "Bob";
        driver.Tick();

        Assert.AreEqual("Bob", textBlock.Text);
    }

    [TestMethod]
    public void DataPresenter_Uses_Default_Editor_Template_When_Value_Is_Bound()
    {
        var name = new State<string?>("Alex");

        var presenter = name.PresentAs(DataTemplateRole.Editor);

        using var driver = new TerminalAppTestDriver(new VStack { presenter }, TerminalHostKind.Fullscreen, new TerminalSize(40, 10));
        driver.Tick();

        Assert.AreEqual(1, presenter.GetChildrenCount());
        Assert.IsInstanceOfType<TextBox>(presenter.GetChildUnsafe(0));

        var textBox = (TextBox)presenter.GetChildUnsafe(0);
        textBox.Text = "Bob";

        Assert.AreEqual("Bob", name.Value);
    }

    [TestMethod]
    public void DataPresenter_Uses_Default_Editor_Template_For_Int32()
    {
        var value = new State<int>(10);

        var presenter = value.PresentAs(DataTemplateRole.Editor);

        using var driver = new TerminalAppTestDriver(new VStack { presenter }, TerminalHostKind.Fullscreen, new TerminalSize(40, 10));
        driver.Tick();

        Assert.AreEqual(1, presenter.GetChildrenCount());
        Assert.IsInstanceOfType<NumberBox<int>>(presenter.GetChildUnsafe(0));

        var numberBox = (NumberBox<int>)presenter.GetChildUnsafe(0);
        numberBox.Value = 42;

        Assert.AreEqual(42, value.Value);
    }

    [TestMethod]
    public void DataPresenter_Uses_Environment_Overrides()
    {
        var name = new State<string>("Alex");

        var templates = DataTemplates.Default.Derive(builder => builder
            .Register<string>(DataTemplateRole.Display, new((Binding<string> binding, in DataTemplateContext _) => new TextBlock(() => $"> {binding.GetValue()}")))
        );

        var presenter = name.PresentAs(DataTemplateRole.Display);

        using var driver = new TerminalAppTestDriver(new VStack { presenter }.Style(templates), TerminalHostKind.Fullscreen, new TerminalSize(40, 10));
        driver.Tick();

        var textBlock = (TextBlock)presenter.GetChildUnsafe(0);
        Assert.AreEqual("> Alex", textBlock.Text);
    }
}

