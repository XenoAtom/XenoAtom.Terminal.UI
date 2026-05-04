// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class FocusStyleTests
{
    [TestMethod]
    public void Select_Default_Focused_Style_Uses_Focus_Foreground()
    {
        var theme = Theme.Default;
        var normal = SelectStyle.Default.ResolveStyle(theme, enabled: true, focused: false, hovered: false);
        var focused = SelectStyle.Default.ResolveStyle(theme, enabled: true, focused: true, hovered: false);

        Assert.AreNotEqual(normal, focused);
        AssertFocusForeground(theme, focused);
        Assert.IsTrue((focused.TextStyle & TextStyle.Bold) != 0, "Focused select controls should use a visible focused text treatment.");
    }

    [TestMethod]
    public void MenuBar_Default_Focused_Items_Use_Focus_Foreground()
    {
        var theme = Theme.Default;
        var normal = MenuBarStyle.Default.ResolveItemStyle(theme, enabled: true, open: false, selected: false, hovered: false);
        var selected = MenuBarStyle.Default.ResolveItemStyle(theme, enabled: true, open: false, selected: true, hovered: false);
        var open = MenuBarStyle.Default.ResolveItemStyle(theme, enabled: true, open: true, selected: false, hovered: false);

        Assert.AreNotEqual(normal, selected);
        AssertFocusForeground(theme, selected);
        AssertFocusForeground(theme, open);
        Assert.IsTrue((selected.TextStyle & TextStyle.Bold) != 0, "Selected top-level menu items should use a visible focused text treatment.");
        Assert.IsTrue((open.TextStyle & TextStyle.Bold) != 0, "Open top-level menu items should use a visible focused text treatment.");
    }

    [TestMethod]
    public void MenuList_Default_Selected_Items_Use_Focus_Foreground()
    {
        var theme = Theme.Default;
        var normal = MenuListStyle.Default.ResolveItemStyle(theme, enabled: true, selected: false, hovered: false);
        var selected = MenuListStyle.Default.ResolveItemStyle(theme, enabled: true, selected: true, hovered: false);

        Assert.AreNotEqual(normal, selected);
        AssertFocusForeground(theme, selected);
        Assert.IsTrue((selected.TextStyle & TextStyle.Bold) != 0, "Selected menu items should use a visible focused text treatment.");
    }

    private static void AssertFocusForeground(Theme theme, Style style)
    {
        Assert.IsNotNull(theme.FocusBorder, "The default theme should define a focus color for this assertion.");
        Assert.IsTrue(style.TryGetForeground(out var foreground), "Expected the focused style to define a foreground color.");
        Assert.AreEqual(theme.FocusBorder, foreground);
    }
}
