// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Hosting;
using XenoAtom.Terminal.UI.Layout;
using XenoAtom.Terminal.UI.Rendering;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class FocusTraversalTests
{
    [TestMethod]
    public void Tab_Skips_Invisible_And_Disabled()
    {
        var a = new ProbeFocusable("A");
        var b = new ProbeFocusable("B") { IsVisible = false };
        var c = new ProbeFocusable("C") { IsEnabled = false };
        var d = new ProbeFocusable("D");

        var root = new VStack();
        root.Add(a);
        root.Add(b);
        root.Add(c);
        root.Add(d);

        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(20, 10));
        driver.Tick();

        Assert.AreSame(a, driver.App.FocusedElement);

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Tab });
        driver.TickUntil(() => ReferenceEquals(driver.App.FocusedElement, d));
    }

    [TestMethod]
    public void Tab_Skips_Non_Tab_Stop_Focusable()
    {
        var editor = new ProbeFocusable("Editor");
        var scrollViewer = new ScrollViewer(editor) { IsTabStop = false };
        var after = new ProbeFocusable("After");

        var root = new VStack();
        root.Add(scrollViewer);
        root.Add(after);

        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(20, 10));
        driver.Tick();

        Assert.AreSame(editor, driver.App.FocusedElement);

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Tab });
        driver.TickUntil(() => ReferenceEquals(driver.App.FocusedElement, after));
    }

    [TestMethod]
    public void ShiftTab_Skips_Non_Tab_Stop_Focusable()
    {
        var before = new ProbeFocusable("Before");
        var editor = new ProbeFocusable("Editor");
        var scrollViewer = new ScrollViewer(editor) { IsTabStop = false };

        var root = new VStack();
        root.Add(before);
        root.Add(scrollViewer);

        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(20, 10));
        driver.Tick();
        driver.App.Focus(editor);

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Tab, Modifiers = TerminalModifiers.Shift });
        driver.TickUntil(() => ReferenceEquals(driver.App.FocusedElement, before));
    }

    [TestMethod]
    public void Non_Tab_Stop_Focusable_Can_Still_Be_Focused_Programmatically()
    {
        var visual = new ProbeFocusable("A") { IsTabStop = false };

        using var driver = new TerminalAppTestDriver(visual, TerminalHostKind.Fullscreen, new TerminalSize(20, 10));
        driver.Tick();
        driver.App.Focus(visual);

        Assert.AreSame(visual, driver.App.FocusedElement);
    }

    [TestMethod]
    public void Tab_From_Non_Tab_Stop_Focusable_Moves_To_Next_Tab_Stop()
    {
        var before = new ProbeFocusable("Before");
        var skipped = new ProbeFocusable("Skipped") { IsTabStop = false };
        var after = new ProbeFocusable("After");

        var root = new VStack();
        root.Add(before);
        root.Add(skipped);
        root.Add(after);

        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(20, 10));
        driver.Tick();
        driver.App.Focus(skipped);

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Tab });
        driver.TickUntil(() => ReferenceEquals(driver.App.FocusedElement, after));
    }

    [TestMethod]
    public void ShiftTab_From_Non_Tab_Stop_Focusable_Moves_To_Previous_Tab_Stop()
    {
        var before = new ProbeFocusable("Before");
        var skipped = new ProbeFocusable("Skipped") { IsTabStop = false };
        var after = new ProbeFocusable("After");

        var root = new VStack();
        root.Add(before);
        root.Add(skipped);
        root.Add(after);

        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(20, 10));
        driver.Tick();
        driver.App.Focus(skipped);

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Tab, Modifiers = TerminalModifiers.Shift });
        driver.TickUntil(() => ReferenceEquals(driver.App.FocusedElement, before));
    }

    private sealed class ProbeFocusable : Visual
    {
        public ProbeFocusable(string text)
        {
            Focusable = true;
            Text = text;
        }

        public string Text { get; }

        protected override SizeHints MeasureCore(in LayoutConstraints constraints) => SizeHints.Fixed(constraints.Clamp(new Size(10, 1)));

        protected override void RenderOverride(CellBuffer buffer)
        {
            buffer.WriteText(Bounds.X, Bounds.Y, Text.AsSpan(), HasFocus ? (Style.None | TextStyle.Invert) : Style.None);
        }
    }
}

