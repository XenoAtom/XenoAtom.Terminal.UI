// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Hosting;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class SelectTests
{
    [TestMethod]
    public void Select_Opens_And_Selects_Item_On_Click()
    {
        var select = new Select<string>()
            .Items(["First", "Second", "Third"]);

        var root = new VStack { select };

        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(40, 10));
        driver.Tick();

        // Click the select to open the popup.
        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Down, Button = TerminalMouseButton.Left, X = 1, Y = 0 });
        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Up, Button = TerminalMouseButton.Left, X = 1, Y = 0 });
        driver.Tick();

        // Click the second item in the popup list (roughly below the select).
        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Down, Button = TerminalMouseButton.Left, X = 2, Y = 3 });
        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Up, Button = TerminalMouseButton.Left, X = 2, Y = 3 });
        driver.TickUntil(() => select.SelectedIndex == 1);

        Assert.AreEqual(1, select.SelectedIndex);
    }

    [TestMethod]
    public void Select_KeyboardPopup_Updates_SelectedIndex_When_Highlight_Changes()
    {
        var selectedIndex = new State<int>(0);
        var select = new Select<string>()
            .Items(["First", "Second", "Third"])
            .SelectedIndex(selectedIndex);

        var root = new VStack { select };

        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(40, 10));
        driver.Tick();

        driver.App.Focus(select);
        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Enter });
        driver.Tick();
        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Down });
        driver.TickUntil(() => selectedIndex.Value == 1);

        Assert.AreEqual(1, selectedIndex.Value);
        Assert.AreEqual(1, select.SelectedIndex);
        Assert.AreEqual(
            1,
            driver.App.Root.EnumerateVisualsDepthFirst().OfType<Popup>().Count(),
            "Changing the popup highlight should update the selection without closing the popup.");

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Enter });
        driver.TickUntil(() => driver.App.Root.EnumerateVisualsDepthFirst().OfType<Popup>().Count() == 0);
    }

    [TestMethod]
    public void Select_Popup_Scrolls_To_CurrentSelection_WhenOpened()
    {
        var select = new Select<string>()
            .Items(Enumerable.Range(0, 30).Select(i => $"Item {i:00}"))
            .SelectedIndex(20);

        var root = new VStack { select };

        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(40, 10));
        driver.Tick();

        driver.App.Focus(select);
        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Enter });
        driver.TickUntil(() => driver.App.Root.EnumerateVisualsDepthFirst().OfType<Popup>().Any());

        var popup = driver.App.Root.EnumerateVisualsDepthFirst().OfType<Popup>().Single();
        var list = popup.EnumerateVisualsDepthFirst().OfType<ListBox<string>>().Single();

        Assert.IsGreaterThan(0, popup.ScrollHost.VerticalOffset, "Expected the popup scroll host to scroll to the selected item.");
        Assert.IsTrue(
            list.Scroll.OffsetY <= select.SelectedIndex && select.SelectedIndex < list.Scroll.OffsetY + list.Scroll.ViewportHeight,
            "Expected the selected item to be inside the list viewport after opening the popup.");

        var screen = new AnsiTestScreen(40, 10);
        screen.Apply(driver.Backend.GetOutText());
        StringAssert.Contains(screen.GetText(), "Item 20", "Expected the selected item text to be visible in the popup.");
    }

    [TestMethod]
    public void Select_Popup_Scrolls_WhenKeyboardSelectionMoves()
    {
        var select = new Select<string>()
            .Items(Enumerable.Range(0, 30).Select(i => $"Item {i:00}"));

        var root = new VStack { select };

        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(40, 10));
        driver.Tick();

        driver.App.Focus(select);
        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Enter });
        driver.TickUntil(() => driver.App.Root.EnumerateVisualsDepthFirst().OfType<Popup>().Any());

        var popup = driver.App.Root.EnumerateVisualsDepthFirst().OfType<Popup>().Single();
        var list = popup.EnumerateVisualsDepthFirst().OfType<ListBox<string>>().Single();

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.End });
        driver.TickUntil(() => select.SelectedIndex == 29 && popup.ScrollHost.VerticalOffset > 0);

        Assert.IsTrue(
            list.Scroll.OffsetY <= select.SelectedIndex && select.SelectedIndex < list.Scroll.OffsetY + list.Scroll.ViewportHeight,
            "Expected End to scroll the selected item into view.");

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Home });
        driver.TickUntil(() => select.SelectedIndex == 0 && popup.ScrollHost.VerticalOffset == 0);

        for (var i = 0; i < 8; i++)
        {
            driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Down });
        }

        driver.TickUntil(() => select.SelectedIndex == 8 && popup.ScrollHost.VerticalOffset > 0);

        Assert.IsTrue(
            list.Scroll.OffsetY <= select.SelectedIndex && select.SelectedIndex < list.Scroll.OffsetY + list.Scroll.ViewportHeight,
            "Expected Down to scroll the selected item into view once it moves past the visible viewport.");
    }

    [TestMethod]
    public void Select_PopupAboveAnchor_Selects_Clicked_Item()
    {
        var selectedIndex = new State<int>(0);
        var select = new Select<string>()
            .Items(["First", "Second", "Third"])
            .SelectedIndex(selectedIndex);

        var root = new VStack
        {
            new TextBlock("row0"),
            new TextBlock("row1"),
            new TextBlock("row2"),
            new TextBlock("row3"),
            new TextBlock("row4"),
            new TextBlock("row5"),
            new TextBlock("row6"),
            select,
        };

        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(40, 8));
        driver.Tick();

        var clickX = select.Bounds.X + 1;
        var clickY = select.Bounds.Y;
        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Down, Button = TerminalMouseButton.Left, X = clickX, Y = clickY });
        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Up, Button = TerminalMouseButton.Left, X = clickX, Y = clickY });
        driver.Tick();

        var popup = driver.App.Root.EnumerateVisualsDepthFirst().OfType<Popup>().Single();
        Assert.IsTrue(popup.PopupRect.Bottom <= select.Bounds.Y, "Expected the popup to be placed above the bottom anchor.");

        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Down, Button = TerminalMouseButton.Left, X = popup.PopupRect.X + 2, Y = popup.PopupRect.Y + 2 });
        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Up, Button = TerminalMouseButton.Left, X = popup.PopupRect.X + 2, Y = popup.PopupRect.Y + 2 });
        driver.Tick();

        Assert.AreEqual(1, selectedIndex.Value);
        Assert.AreEqual(1, select.SelectedIndex);
    }

    [TestMethod]
    public void Select_IdleTick_DoesNotRebuild_SelectedContent()
    {
        var select = new Select<string>()
            .Items(["First", "Second", "Third"]);

        var root = new VStack { select };

        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(40, 10));
        driver.Tick();

        var initialContent = select.Content;
        Assert.IsNotNull(initialContent);

        driver.Tick();

        Assert.AreSame(initialContent, select.Content);
    }

    [TestMethod]
    public void Select_SelectedItemChange_Rebuilds_SelectedContent()
    {
        var select = new Select<string>()
            .Items(["First", "Second", "Third"]);

        var root = new VStack { select };

        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(40, 10));
        driver.Tick();

        var initialContent = select.Content;
        Assert.IsNotNull(initialContent);

        select.Items[0] = "Updated";
        driver.Tick();

        Assert.AreNotSame(initialContent, select.Content);
    }

    [TestMethod]
    public void Select_Bound_SelectedIndex_Does_Not_Write_Source_During_Tick()
    {
        var selectedIndex = new State<int>(0);
        var select = new Select<string>()
            .Items(["First", "Second", "Third"])
            .SelectedIndex(selectedIndex);

        var root = new VStack { select };

        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(40, 10));
        driver.Tick();

        var rewroteSource = false;
        select.SelectionChanged((_, e) =>
        {
            if (rewroteSource || e.NewIndex != 1)
            {
                return;
            }

            rewroteSource = true;
            selectedIndex.Value = 0;
        });

        selectedIndex.Value = 1;

        driver.Tick();

        Assert.IsTrue(rewroteSource, "Expected the bound source change to notify the control once.");
        Assert.AreEqual(0, selectedIndex.Value);
        Assert.AreEqual(0, select.SelectedIndex);
    }

    [TestMethod]
    public void Select_Out_Of_Range_State_Does_Not_Get_Clamped_During_Tick()
    {
        var selectedIndex = new State<int>(10);
        var select = new Select<string>()
            .Items(["First", "Second", "Third"])
            .SelectedIndex(selectedIndex);

        var root = new VStack { select };

        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(40, 10));
        driver.Tick();

        Assert.AreEqual(10, selectedIndex.Value, "The bound source should not be rewritten during prepare/measure.");
        Assert.AreEqual(2, select.SelectedIndex, "The control should clamp its local selected index outside guarded callbacks.");
        Assert.IsNotNull(select.Content);
    }

    [TestMethod]
    public void Select_Bound_SelectedIndex_Reapplies_Source_After_Items_Expand()
    {
        var selectedIndex = new State<int>(0);
        var select = new Select<string>()
            .SelectedIndex((Binding<int>)selectedIndex);
        select.Items.Add("placeholder");

        using var driver = new TerminalAppTestDriver(new VStack { select }, TerminalHostKind.Fullscreen, new TerminalSize(40, 5));
        driver.Tick();

        selectedIndex.Value = 9;

        Assert.AreEqual(9, selectedIndex.Value);
        Assert.AreEqual(0, select.SelectedIndex, "The target clamps while only the placeholder item exists.");

        select.Items.Clear();
        for (var i = 0; i < 18; i++)
        {
            select.Items.Add(i == 9 ? "preferred-model" : $"listed-{i}");
        }

        select.SelectedIndex = selectedIndex.Value;
        driver.Tick();

        Assert.AreEqual(9, selectedIndex.Value);
        Assert.AreEqual(9, select.SelectedIndex);

        var screen = new AnsiTestScreen(40, 5);
        screen.Apply(driver.Backend.GetOutText());
        StringAssert.Contains(screen.GetText(), "preferred-model");
    }

    [TestMethod]
    public void Select_Inside_Dialog_Opens_Interactable_Popup_And_Closing_Dialog_Removes_It()
    {
        var select = new Select<string>()
            .Items(["First", "Second", "Third"]);

        var dialog = new Dialog
        {
            Title = "Dialog",
            Width = 24,
            Height = 8,
            Left = 10,
            Top = 4,
            Content = new Padder(select).Padding(new Thickness(2, 1, 0, 0)),
        };

        using var driver = new TerminalAppTestDriver(new VStack(), TerminalHostKind.Fullscreen, new TerminalSize(50, 20));
        driver.Tick();

        dialog.Show();
        driver.Tick();

        var clickX = select.Bounds.X + 1;
        var clickY = select.Bounds.Y;
        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Down, Button = TerminalMouseButton.Left, X = clickX, Y = clickY });
        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Up, Button = TerminalMouseButton.Left, X = clickX, Y = clickY });
        driver.Tick();

        var screen = new AnsiTestScreen(50, 20);
        screen.Apply(driver.Backend.GetOutText());
        StringAssert.Contains(screen.GetText(), "Second", "Opening the select inside a dialog should render the popup above the dialog.");

        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Down, Button = TerminalMouseButton.Left, X = clickX + 1, Y = clickY + 3 });
        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Up, Button = TerminalMouseButton.Left, X = clickX + 1, Y = clickY + 3 });
        driver.TickUntil(() => select.SelectedIndex == 1);

        dialog.Close();
        driver.Tick();

        Assert.AreEqual(0, driver.App.Root.EnumerateVisualsDepthFirst().OfType<Popup>().Count(), "Closing the dialog should also close the select popup.");
    }
}
