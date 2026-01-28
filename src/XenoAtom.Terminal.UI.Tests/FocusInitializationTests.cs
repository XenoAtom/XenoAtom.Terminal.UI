// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Hosting;
using XenoAtom.Terminal.UI.Layout;
using XenoAtom.Terminal.UI.Rendering;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class FocusInitializationTests
{
    [TestMethod]
    public void AutoFocus_Is_Preferred_When_Focus_Is_Initialized()
    {
        var search = new ProbeFocusable("Search");
        var list = new ProbeFocusable("List").AutoFocus(true);

        var root = new VStack();
        root.Add(search);
        root.Add(list);

        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(20, 10));
        driver.Tick();

        Assert.AreSame(list, driver.App.FocusedElement);
    }

    [TestMethod]
    public void InitialFocusMode_None_Leaves_No_Focus()
    {
        var root = new VStack();
        root.Add(new ProbeFocusable("A"));

        using var driver = new TerminalAppTestDriver(
            root,
            TerminalHostKind.Fullscreen,
            new TerminalSize(20, 10),
            new TerminalAppOptions { InitialFocusMode = InitialFocusMode.None });

        driver.Tick();

        Assert.IsNull(driver.App.FocusedElement);
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

