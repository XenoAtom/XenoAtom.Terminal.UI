// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Reflection;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Hosting;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class CollapsibleTests
{
    [TestMethod]
    public void Collapsible_Toggles_On_Enter_Key()
    {
        var collapsible = new Collapsible("Header", "Body") { IsExpanded = false };

        using var driver = new TerminalAppTestDriver(collapsible, TerminalHostKind.Fullscreen, new TerminalSize(30, 8));
        driver.Tick();

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Enter });
        driver.Tick();

        Assert.IsTrue(collapsible.IsExpanded);
    }

    [TestMethod]
    public void Collapsible_Toggles_On_Header_Click()
    {
        var collapsible = new Collapsible("Header", "Body") { IsExpanded = false };

        using var driver = new TerminalAppTestDriver(collapsible, TerminalHostKind.Fullscreen, new TerminalSize(30, 8));
        driver.Tick();

        var x = collapsible.Bounds.X + 1;
        var y = collapsible.Bounds.Y;

        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Down, Button = TerminalMouseButton.Left, X = x, Y = y });
        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Up, Button = TerminalMouseButton.Left, X = x, Y = y });
        driver.Tick();

        Assert.IsTrue(collapsible.IsExpanded);
    }

    [TestMethod]
    public void Collapsible_Clears_Header_Hover_When_Pointer_Moves_To_Sibling()
    {
        var collapsible = new Collapsible("Header", "Body") { IsExpanded = false };
        var sibling = new Button("Next");
        var root = new VStack(collapsible, sibling).Spacing(0);

        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(30, 8));
        driver.Tick();

        var headerX = collapsible.Bounds.X + 1;
        var headerY = collapsible.Bounds.Y;
        var siblingX = sibling.Bounds.X + 1;
        var siblingY = sibling.Bounds.Y;

        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Move, Button = TerminalMouseButton.None, X = headerX, Y = headerY });
        driver.Tick();

        Assert.IsTrue(collapsible.IsHeaderHovered);

        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Move, Button = TerminalMouseButton.None, X = siblingX, Y = siblingY });
        driver.Tick();

        Assert.IsFalse(collapsible.IsHeaderHovered);
    }

    [TestMethod]
    public void Collapsible_Clears_Header_Hover_When_Pointer_Moves_To_Content()
    {
        var content = new Button("Body");
        var collapsible = new Collapsible("Header", content) { IsExpanded = true };

        using var driver = new TerminalAppTestDriver(collapsible, TerminalHostKind.Fullscreen, new TerminalSize(30, 8));
        driver.Tick();

        var headerX = collapsible.Bounds.X + 1;
        var headerY = collapsible.Bounds.Y;
        var contentX = content.Bounds.X + 1;
        var contentY = content.Bounds.Y;

        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Move, Button = TerminalMouseButton.None, X = headerX, Y = headerY });
        driver.Tick();

        Assert.IsTrue(collapsible.IsHeaderHovered);

        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Move, Button = TerminalMouseButton.None, X = contentX, Y = contentY });
        driver.Tick();

        Assert.IsFalse(collapsible.IsHeaderHovered);
    }

    [TestMethod]
    public void Collapsible_Remains_Keyboard_Navigable_After_Hover_Changes()
    {
        var collapsible = new Collapsible("Header", "Body") { IsExpanded = false };
        var nextButton = new Button("Next");
        var root = new VStack(collapsible, nextButton).Spacing(0);

        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(30, 8));
        driver.Tick();

        Assert.AreSame(collapsible, driver.App.FocusedElement);

        driver.Backend.PushEvent(new TerminalMouseEvent
        {
            Kind = TerminalMouseKind.Move,
            Button = TerminalMouseButton.None,
            X = collapsible.Bounds.X + 1,
            Y = collapsible.Bounds.Y,
        });
        driver.Tick();

        Assert.IsTrue(collapsible.IsHeaderHovered);

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Tab });
        driver.TickUntil(() => ReferenceEquals(driver.App.FocusedElement, nextButton));

        Assert.IsFalse(collapsible.IsExpanded);
    }

    [TestMethod]
    public void Tab_Moves_Focus_To_Next_Collapsible_After_Hover_Changes()
    {
        var first = new Collapsible("First", "Body 1") { IsExpanded = false };
        var second = new Collapsible("Second", "Body 2") { IsExpanded = false };
        var root = new VStack(first, second).Spacing(0);

        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(30, 8));
        driver.Tick();

        Assert.AreSame(first, driver.App.FocusedElement);

        driver.Backend.PushEvent(new TerminalMouseEvent
        {
            Kind = TerminalMouseKind.Move,
            Button = TerminalMouseButton.None,
            X = first.Bounds.X + 1,
            Y = first.Bounds.Y,
        });
        driver.Tick();

        Assert.IsTrue(first.IsHeaderHovered);

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Tab });
        driver.TickUntil(() => ReferenceEquals(driver.App.FocusedElement, second));
    }

    [TestMethod]
    public void Focused_Collapsible_Header_Has_Visible_Focus_Style()
    {
        var style = CollapsibleStyle.Default;
        var theme = Theme.Default;
        var normal = style.ResolveHeader(theme, enabled: true, focused: false, hovered: false, pressed: false);
        var hovered = style.ResolveHeader(theme, enabled: true, focused: false, hovered: true, pressed: false);
        var focused = style.ResolveHeader(theme, enabled: true, focused: true, hovered: false, pressed: false);

        Assert.IsTrue(focused.TryGetBackground(out var focusedBackground), "Expected a focused collapsible header to have a visible background highlight.");
        Assert.IsTrue(hovered.TryGetBackground(out var hoveredBackground), "Expected a hovered collapsible header to have a visible background highlight.");
        Assert.AreNotEqual(normal, focused, "Expected the focused collapsible header style to differ from the normal style.");
        Assert.AreNotEqual(hoveredBackground, focusedBackground, "Expected focused collapsible headers to use a stronger selected/focus fill than hover.");
        Assert.AreEqual(theme.Selection, focusedBackground, "Expected focused collapsible headers to use the theme selection/focus fill.");
        Assert.IsTrue((focused.TextStyle & TextStyle.Underline) != 0, "Expected focused collapsible text to remain underlined.");
    }

    [TestMethod]
    public void Focused_Collapsible_Treats_Self_Focus_As_Header_Focus()
    {
        var theme = Theme.Default;
        var collapsible = new Collapsible("Header", "Body") { IsExpanded = false };
        collapsible.Style(theme);

        using var driver = new TerminalAppTestDriver(collapsible, TerminalHostKind.Fullscreen, new TerminalSize(20, 4));
        driver.Tick();

        Assert.AreSame(collapsible, driver.App.FocusedElement);
        Assert.IsTrue(collapsible.HasFocus);

        var isHeaderFocused = (bool)typeof(Collapsible).GetMethod("IsHeaderFocused", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(collapsible, Array.Empty<object>())!;

        Assert.IsTrue(isHeaderFocused, "Expected a collapsible with keyboard focus to render its header using the focused style.");
    }
}
