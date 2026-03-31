// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Hosting;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class OverlayFocusRestoreTests
{
    [TestMethod]
    public void Popup_Close_Restores_Previous_Focus()
    {
        var focusedBefore = new TextBox("Before");
        var anchor = new Button("Anchor");
        var popupFocus = new TextBox("Popup");
        var root = new VStack(focusedBefore, anchor, new TextBox("After"));

        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen);
        driver.Tick();

        driver.App.Focus(focusedBefore);
        driver.Tick();

        var popup = new Popup
        {
            Anchor = anchor,
            Content = popupFocus,
            MatchAnchorWidth = false,
        };

        popup.Show();
        driver.Tick();

        Assert.AreSame(popupFocus, driver.App.FocusedElement);

        popup.Close();
        driver.Tick();

        Assert.AreSame(focusedBefore, driver.App.FocusedElement);
    }

    [TestMethod]
    public void Dialog_Close_Restores_Previous_Focus()
    {
        var focusedBefore = new TextBox("Before");
        var dialogFocus = new TextBox("Dialog");
        var root = new VStack(focusedBefore, new TextBox("After"));

        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen);
        driver.Tick();

        driver.App.Focus(focusedBefore);
        driver.Tick();

        var dialog = new Dialog
        {
            IsModal = true,
            Width = 20,
            Height = 6,
            Content = dialogFocus,
        };

        dialog.Show();
        driver.Tick();

        Assert.AreSame(dialogFocus, driver.App.FocusedElement);

        dialog.Close();
        driver.Tick();

        Assert.AreSame(focusedBefore, driver.App.FocusedElement);
    }

    [TestMethod]
    public void Nested_Overlays_Restore_Focus_Per_Level()
    {
        var focusedBefore = new TextBox("Before");
        var dialogFocus = new TextBox("Dialog");
        var popupFocus = new TextBox("Popup");
        var root = new VStack(focusedBefore, new TextBox("After"));

        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen);
        driver.Tick();

        driver.App.Focus(focusedBefore);
        driver.Tick();

        var dialog = new Dialog
        {
            IsModal = true,
            Width = 20,
            Height = 6,
            Content = dialogFocus,
        };

        dialog.Show();
        driver.Tick();

        Assert.AreSame(dialogFocus, driver.App.FocusedElement);

        var popup = new Popup
        {
            Anchor = dialogFocus,
            Content = popupFocus,
            MatchAnchorWidth = false,
        };

        popup.Show();
        driver.Tick();

        Assert.AreSame(popupFocus, driver.App.FocusedElement);

        popup.Close();
        driver.Tick();

        Assert.AreSame(dialogFocus, driver.App.FocusedElement);

        dialog.Close();
        driver.Tick();

        Assert.AreSame(focusedBefore, driver.App.FocusedElement);
    }
}
