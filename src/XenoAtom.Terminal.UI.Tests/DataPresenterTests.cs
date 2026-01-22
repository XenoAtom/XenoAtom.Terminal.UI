// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Hosting;
using XenoAtom.Terminal.UI.Templating;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class DataPresenterTests
{
    private static void ReplaceAllText(TextBox textBox, string text)
    {
        ArgumentNullException.ThrowIfNull(textBox);
        var length = textBox.TextDocument.CurrentSnapshot.Length;
        textBox.TextDocument.Replace(0, length, text);
    }

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
    public void DataPresenter_Default_Bool_Display_Is_Lowercase()
    {
        var value = new State<bool>(true);

        var presenter = value.PresentAs(DataTemplateRole.Display);

        using var driver = new TerminalAppTestDriver(new VStack { presenter }, TerminalHostKind.Fullscreen, new TerminalSize(20, 5));
        driver.Tick();

        Assert.IsInstanceOfType<TextBlock>(presenter.GetChildUnsafe(0));
        var textBlock = (TextBlock)presenter.GetChildUnsafe(0);
        Assert.AreEqual("true", textBlock.Text);

        value.Value = false;
        driver.Tick();
        Assert.AreEqual("false", textBlock.Text);
    }

    [TestMethod]
    public void DataPresenter_Uses_Default_Editor_Template_For_Guid()
    {
        var value = new State<Guid>(Guid.Empty);
        var presenter = value.PresentAs(DataTemplateRole.Editor);
        var focusSink = new Button("Sink");

        using var driver = new TerminalAppTestDriver(new VStack { focusSink, presenter }, TerminalHostKind.Fullscreen, new TerminalSize(50, 6));
        driver.Tick();

        Assert.IsInstanceOfType<TextBox>(presenter.GetChildUnsafe(0));
        var textBox = (TextBox)presenter.GetChildUnsafe(0);

        var guid = Guid.NewGuid();
        ReplaceAllText(textBox, guid.ToString("D"));
        Assert.AreEqual(guid, value.Value);

        driver.App.Focus(focusSink);
        driver.Tick();

        guid = Guid.NewGuid();
        value.Value = guid;
        driver.Tick();
        Assert.AreEqual(guid.ToString("D"), textBox.Text);
    }

    [TestMethod]
    public void DataPresenter_Uses_Default_Editor_Template_For_DateOnly()
    {
        var value = new State<DateOnly>(new DateOnly(2024, 1, 1));
        var presenter = value.PresentAs(DataTemplateRole.Editor);

        using var driver = new TerminalAppTestDriver(new VStack { presenter }, TerminalHostKind.Fullscreen, new TerminalSize(50, 6));
        driver.Tick();

        Assert.IsInstanceOfType<TextBox>(presenter.GetChildUnsafe(0));
        var textBox = (TextBox)presenter.GetChildUnsafe(0);

        ReplaceAllText(textBox, "2024-12-31");
        Assert.AreEqual(new DateOnly(2024, 12, 31), value.Value);
    }

    [TestMethod]
    public void DataPresenter_Uses_Default_Editor_Template_For_Enum_When_Not_Registered()
    {
        var value = new State<TestEnum>(TestEnum.Beta);
        var presenter = value.PresentAs(DataTemplateRole.Editor);
        var focusSink = new Button("Sink");

        using var driver = new TerminalAppTestDriver(new VStack { focusSink, presenter }, TerminalHostKind.Fullscreen, new TerminalSize(50, 6));
        driver.Tick();

        Assert.IsInstanceOfType<TextBox>(presenter.GetChildUnsafe(0));
        var textBox = (TextBox)presenter.GetChildUnsafe(0);

        ReplaceAllText(textBox, "Alpha");
        Assert.AreEqual(TestEnum.Alpha, value.Value);

        driver.App.Focus(focusSink);
        driver.Tick();

        value.Value = TestEnum.Gamma;
        driver.Tick();
        Assert.AreEqual("Gamma", textBox.Text);
    }

    [TestMethod]
    public void DataPresenter_Uses_Select_When_Enum_Is_Registered()
    {
        var value = new State<TestEnum>(TestEnum.Beta);
        var presenter = value.PresentAs(DataTemplateRole.Editor);

        var templates = DataTemplates.Default.Derive(b => b.RegisterEnum<TestEnum>());

        using var driver = new TerminalAppTestDriver(new VStack { presenter }.Style(templates), TerminalHostKind.Fullscreen, new TerminalSize(60, 10));
        driver.Tick();

        Assert.IsInstanceOfType<EnumSelect<TestEnum>>(presenter.GetChildUnsafe(0));
        var select = (EnumSelect<TestEnum>)presenter.GetChildUnsafe(0);
        select.SelectedIndex = 0;
        driver.Tick();
        Assert.AreEqual(TestEnum.Alpha, value.Value);
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

    private enum TestEnum
    {
        Alpha,
        Beta,
        Gamma,
    }
}

