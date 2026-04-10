// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Hosting;
using XenoAtom.Terminal;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class ValidationPresenterTests
{
    private static string RenderText(TerminalAppTestDriver driver, int width = 60, int height = 10)
    {
        var screen = new AnsiTestScreen(width, height);
        screen.Apply(driver.Backend.GetOutText());
        return screen.GetText();
    }

    private static ValidationMessage? ValidatePort(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return new(ValidationSeverity.Error, "Port is required.");
        }

        if (!int.TryParse(text, out var port) || port is < 1 or > 65535)
        {
            return new(ValidationSeverity.Error, "Port must be in [1..65535].");
        }

        return null;
    }

    [TestMethod]
    public void ValidationPresenter_Invokes_Validator_When_Bound_Value_Changes()
    {
        var value = new State<string?>("8080");

        var calls = 0;
        ValidationMessage? Validator(string? text)
        {
            calls++;
            _ = text;
            return null;
        }

        var presenter = new TextBox()
            .Text(value)
            .Validate(value.Bind.Value, Validator);

        using var driver = new TerminalAppTestDriver(new VStack { presenter }, TerminalHostKind.Fullscreen, new TerminalSize(40, 10));

        driver.Tick();
        Assert.AreNotEqual(0, calls);

        var previousCalls = calls;
        driver.Tick();
        Assert.AreEqual(previousCalls, calls);

        value.Value = "9090";
        driver.Tick();
        Assert.AreNotEqual(previousCalls, calls);
    }

    [TestMethod]
    public void ValidationPresenter_Validate_Reacts_To_The_Current_TextBox_Value()
    {
        var value = new State<string?>("8080");
        var textBox = new TextBox().Text(value);
        var presenter = textBox.Validate(value.Bind.Value, ValidatePort);

        using var driver = new TerminalAppTestDriver(new VStack { presenter }, TerminalHostKind.Fullscreen, new TerminalSize(60, 8));

        driver.Tick();
        Assert.AreSame(textBox, driver.App.FocusedElement);
        textBox.CaretIndex = textBox.Text?.Length ?? 0;

        driver.Backend.PushEvent(new TerminalTextEvent { Text = "1" });
        driver.Tick();

        Assert.AreEqual("80801", value.Value);

        var rendered = RenderText(driver, 60, 8);
        StringAssert.Contains(rendered, "Port must be in [1..65535].");
    }

    [TestMethod]
    public void ValidationPresenter_Renders_Message_Text_While_TextBox_Is_Focused()
    {
        var value = new State<string?>("80801");
        var textBox = new TextBox().Text(value);
        var presenter = textBox.Validate(value.Bind.Value, ValidatePort);

        using var driver = new TerminalAppTestDriver(new VStack { presenter }, TerminalHostKind.Fullscreen, new TerminalSize(60, 8));

        driver.Tick();
        Assert.AreSame(textBox, driver.App.FocusedElement);

        var rendered = RenderText(driver, 60, 8);
        StringAssert.Contains(rendered, "Port must be in [1..65535].");
    }

    [TestMethod]
    public void ValidationPresenter_Demo_Shows_Validation_Text_On_The_Focused_Control()
    {
        var value = new State<string?>("8080");
        var first = new TextBox().Text(value);
        var second = new TextBox().Text(value);
        var root = new VStack(
            first.Validate(value.Bind.Value, ValidatePort),
            second.Validate(value.Bind.Value, ValidatePort, ValidationPlacement.Above));

        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(80, 12));

        driver.Tick();
        Assert.AreSame(first, driver.App.FocusedElement);
        first.CaretIndex = first.Text?.Length ?? 0;

        driver.Backend.PushEvent(new TerminalTextEvent { Text = "1" });
        driver.Tick();
        driver.Backend.PushEvent(new TerminalTextEvent { Text = "1" });
        driver.Tick();

        var rendered = RenderText(driver, 80, 12);
        StringAssert.Contains(rendered, "Port must be in [1..65535].");
    }
}

