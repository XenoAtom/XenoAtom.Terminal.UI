// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Hosting;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class EnumSelectTests
{
    private enum TestChoice
    {
        First = 1,
        Second = 2,
        Third = 3,
    }

    [TestMethod]
    public void EnumSelect_Initializes_With_First_Enum_Value()
    {
        var select = new EnumSelect<TestChoice>();

        Assert.AreEqual(TestChoice.First, select.Value);
        Assert.AreEqual(0, select.SelectedIndex);
        Assert.AreEqual(3, select.Items.Count);
    }

    [TestMethod]
    public void EnumSelect_Value_And_SelectedIndex_Stay_In_Sync()
    {
        var select = new EnumSelect<TestChoice>();
        using var driver = new TerminalAppTestDriver(select, TerminalHostKind.Fullscreen, new TerminalSize(30, 6));
        driver.Tick();

        select.Value = TestChoice.Third;
        driver.Tick();
        Assert.AreEqual(2, select.SelectedIndex);

        select.SelectedIndex = 1;
        driver.Tick();
        Assert.AreEqual(TestChoice.Second, select.Value);
    }

    [TestMethod]
    public void EnumSelect_Invalid_Value_Falls_Back_To_First()
    {
        var select = new EnumSelect<TestChoice>
        {
            Value = (TestChoice)999,
        };

        Assert.AreEqual(TestChoice.First, select.Value);
        Assert.AreEqual(0, select.SelectedIndex);
    }
}
