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
}

