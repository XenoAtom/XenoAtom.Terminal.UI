// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Hosting;
using XenoAtom.Terminal.UI.Layout;

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
    public void EnumSelect_Renders_First_Selected_Value()
    {
        var select = new EnumSelect<TestChoice>();
        using var driver = new TerminalAppTestDriver(select, TerminalHostKind.Fullscreen, new TerminalSize(30, 3));

        driver.Tick();

        var screen = new AnsiTestScreen(30, 3);
        screen.Apply(driver.Backend.GetOutText());
        StringAssert.Contains(screen.GetText(), nameof(TestChoice.First));
    }

    [TestMethod]
    public void EnumSelect_Honors_MinWidth_And_HorizontalStretch()
    {
        var minWidthSelect = new EnumSelect<TestChoice>()
        {
            HorizontalAlignment = Align.Start,
            MinWidth = 15,
        };
        var stretchSelect = new EnumSelect<TestChoice>
        {
            HorizontalAlignment = Align.Stretch,
        };
        var root = new VStack(minWidthSelect, stretchSelect);

        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(40, 20));
        driver.Tick();

        Assert.AreEqual(15, minWidthSelect.Bounds.Width);
        Assert.AreEqual(40, stretchSelect.Bounds.Width);

        AssertPopupMatchesAnchor(minWidthSelect);
        AssertPopupMatchesAnchor(stretchSelect);

        void AssertPopupMatchesAnchor(EnumSelect<TestChoice> select)
        {
            driver.App.Focus(select);
            driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Enter });
            driver.TickUntil(() => driver.App.Root.EnumerateVisualsDepthFirst().OfType<Popup>().Any());

            var popup = driver.App.Root.EnumerateVisualsDepthFirst().OfType<Popup>().Single();
            var border = popup.EnumerateVisualsDepthFirst().OfType<Border>().Single();
            var list = popup.EnumerateVisualsDepthFirst().OfType<ListBox<TestChoice>>().Single();
            Assert.AreEqual(
                popup.PopupRect.Width,
                border.Bounds.Width,
                "The popup selection box should fill the width derived from its anchor.");
            Assert.IsTrue(border.Bounds.Width >= select.Bounds.Width);
            Assert.AreEqual(border.Bounds.Width - 2, list.Bounds.Width);

            driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Enter });
            driver.TickUntil(() => !driver.App.Root.EnumerateVisualsDepthFirst().OfType<Popup>().Any());
        }
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
