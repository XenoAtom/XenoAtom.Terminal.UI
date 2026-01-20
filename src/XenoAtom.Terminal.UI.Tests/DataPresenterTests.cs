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
    public void DataPresenter_Uses_Default_Display_Template_For_State_String()
    {
        var name = new State<string>("Alex");

        var presenter = new DataPresenter<State<string>>
        {
            Role = DataTemplateRole.Display,
            Value = name
        };

        using var driver = new TerminalAppTestDriver(new VStack { presenter }, TerminalHostKind.Fullscreen, new TerminalSize(40, 10));
        driver.Tick();

        Assert.IsInstanceOfType<TextBlock>(presenter.Content);
        Assert.AreEqual("Alex", ((TextBlock)presenter.Content!).Text);
    }

    [TestMethod]
    public void DataPresenter_Uses_Default_Editor_Template_For_State_String()
    {
        var name = new State<string?>("Alex");

        var presenter = new DataPresenter<State<string?>>
        {
            Role = DataTemplateRole.Editor,
            Value = name
        };

        using var driver = new TerminalAppTestDriver(new VStack { presenter }, TerminalHostKind.Fullscreen, new TerminalSize(40, 10));
        driver.Tick();

        Assert.IsInstanceOfType<TextBox>(presenter.Content);
        var textBox = (TextBox)presenter.Content!;
        textBox.Text = "Bob";
        Assert.AreEqual("Bob", name.Value);
    }

    [TestMethod]
    public void DataPresenter_Uses_Default_Editor_Template_For_State_Int32()
    {
        var value = new State<int>(10);

        var presenter = new DataPresenter<State<int>>
        {
            Role = DataTemplateRole.Editor,
            Value = value
        };

        using var driver = new TerminalAppTestDriver(new VStack { presenter }, TerminalHostKind.Fullscreen, new TerminalSize(40, 10));
        driver.Tick();

        Assert.IsInstanceOfType<NumberBox<int>>(presenter.Content);
        var numberBox = (NumberBox<int>)presenter.Content!;
        numberBox.Value = 42;
        Assert.AreEqual(42, value.Value);
    }
}
