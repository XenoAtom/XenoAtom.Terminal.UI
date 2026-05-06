// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Hosting;
using XenoAtom.Terminal.UI.Layout;
using XenoAtom.Terminal.UI.Rendering;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class TextBlockRenderingTests
{
    [TestMethod]
    public void TextBlock_FuncText_Reapplies_On_Bindable_Model_Change()
    {
        var viewModel = new TextBlockBindableViewModel("Before");
        var textBlock = new TextBlock(() => viewModel.Text);

        using var driver = new TerminalAppTestDriver(new VStack(textBlock), TerminalHostKind.Fullscreen, new TerminalSize(20, 2));
        driver.Tick();

        viewModel.Text = "After";
        driver.Tick();

        Assert.AreEqual("After", textBlock.Text);
    }

    [TestMethod]
    public void TextBlock_FuncText_Reapplies_When_Measured_Before_Attach()
    {
        var viewModel = new TextBlockBindableViewModel("Before");
        var textBlock = new TextBlock(() => viewModel.Text);
        textBlock.Measure(LayoutConstraints.Unbounded);

        using var driver = new TerminalAppTestDriver(new VStack(textBlock), TerminalHostKind.Fullscreen, new TerminalSize(20, 2));
        driver.Tick();

        viewModel.Text = "After";
        driver.Tick();

        Assert.AreEqual("After", textBlock.Text);
    }

    [TestMethod]
    public void TextBlock_SingleLine_Reports_Horizontal_Shrink_Budget()
    {
        var tb = new TextBlock("HelloWorld")
        {
            Wrap = false,
            Trimming = TextTrimming.EndEllipsis,
        };

        tb.Measure(LayoutConstraints.Unbounded);

        Assert.AreEqual(1, tb.MeasureHints.Min.Width);
        Assert.AreEqual(10, tb.MeasureHints.Natural.Width);
        Assert.AreEqual(10, tb.MeasureHints.Max.Width);
        Assert.AreEqual(1, tb.MeasureHints.FlexShrinkX);
    }

    [TestMethod]
    public void TextBlock_Wrap_Reports_Horizontal_Shrink_Budget()
    {
        var tb = new TextBlock("Hello World")
        {
            Wrap = true,
        };

        tb.Measure(new LayoutConstraints(0, 8, 0, 5));

        Assert.AreEqual(1, tb.MeasureHints.Min.Width);
        Assert.AreEqual(8, tb.MeasureHints.Natural.Width);
        Assert.AreEqual(2, tb.MeasureHints.Natural.Height);
        Assert.AreEqual(1, tb.MeasureHints.FlexShrinkX);
    }

    [TestMethod]
    public void TextBlock_EndEllipsis_Trims_To_Width()
    {
        var tb = new TextBlock("HelloWorld")
        {
            Wrap = false,
            Trimming = TextTrimming.EndEllipsis,
            HorizontalAlignment = Align.Stretch,
            MaxWidth = 5,
        };

        var root = new VStack(tb);
        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(10, 2));
        driver.Tick();

        var outText = driver.Backend.GetOutText();
        StringAssert.Contains(outText, "Hell…");
    }

    [TestMethod]
    public void TextBlock_StartEllipsis_Trims_To_Width()
    {
        var tb = new TextBlock("HelloWorld")
        {
            Wrap = false,
            Trimming = TextTrimming.StartEllipsis,
            HorizontalAlignment = Align.Stretch,
            MaxWidth = 5,
        };

        var root = new VStack(tb);
        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(10, 2));
        driver.Tick();

        var outText = driver.Backend.GetOutText();
        StringAssert.Contains(outText, "…orld");
    }

    [TestMethod]
    public void TextBlock_Can_Center_Align_Text_When_Stretched()
    {
        var tb = new TextBlock("Hi")
        {
            Wrap = false,
            HorizontalAlignment = Align.Stretch,
            TextAlignment = TextAlignment.Center,
        };

        var root = new VStack(tb);
        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(10, 2));
        driver.Tick();

        var outText = driver.Backend.GetOutText();
        StringAssert.Contains(outText, "    Hi");
    }

    [TestMethod]
    public void TextBlock_Can_Apply_TextBlockStyle_Foreground()
    {
        var tb = new TextBlock("Hi")
            .Style(Theme.Default)
            .Style(TextBlockStyle.Default with { Foreground = Colors.Red });

        tb.Measure(new LayoutConstraints(0, 10, 0, 1));
        tb.Arrange(new Rectangle(0, 0, 10, 1));

        var buffer = new CellBuffer(10, 1);
        buffer.Clear(tb.GetTheme().BaseTextStyle());
        tb.RenderTree(buffer);

        Assert.IsTrue(buffer.UnsafeCells[0].TryGetForeground(out var fg));
        Assert.AreEqual(Colors.Red, fg);
    }

    [TestMethod]
    public void TextBlock_Can_Apply_TextBlockStyle_Background_Without_Fill()
    {
        var tb = new TextBlock("Hi")
            .Style(Theme.Default)
            .HorizontalAlignment(Align.Stretch)
            .Style(TextBlockStyle.Default with { Background = Colors.Blue, FillBackground = false });

        tb.Measure(new LayoutConstraints(0, 6, 0, 1));
        tb.Arrange(new Rectangle(0, 0, 6, 1));

        var buffer = new CellBuffer(6, 1);
        buffer.Clear(tb.GetTheme().BaseTextStyle());
        tb.RenderTree(buffer);

        Assert.IsTrue(buffer.UnsafeCells[0].TryGetBackground(out var bg0));
        Assert.AreEqual(Colors.Blue, bg0);

        Assert.IsTrue(buffer.UnsafeCells[4].TryGetBackground(out var bg4));
        Assert.AreNotEqual(Colors.Blue, bg4);
    }

    [TestMethod]
    public void TextBlock_Can_Fill_Background_With_TextBlockStyle()
    {
        var tb = new TextBlock("Hi")
            .Style(Theme.Default)
            .HorizontalAlignment(Align.Stretch)
            .Style(TextBlockStyle.Default with { Background = Colors.Blue, FillBackground = true });

        tb.Measure(new LayoutConstraints(0, 6, 0, 1));
        tb.Arrange(new Rectangle(0, 0, 6, 1));

        var buffer = new CellBuffer(6, 1);
        buffer.Clear(tb.GetTheme().BaseTextStyle());
        tb.RenderTree(buffer);

        Assert.IsTrue(buffer.UnsafeCells[0].TryGetBackground(out var bg0));
        Assert.AreEqual(Colors.Blue, bg0);

        Assert.IsTrue(buffer.UnsafeCells[4].TryGetBackground(out var bg4));
        Assert.AreEqual(Colors.Blue, bg4);
    }

    private sealed class TextBlockBindableViewModel
    {
        public static readonly BindingAccessor<string> TextAccessor = new(
            nameof(Text),
            owner => ((TextBlockBindableViewModel)owner)._text,
            (owner, value) => ((TextBlockBindableViewModel)owner).Text = value);

        private string _text;

        public TextBlockBindableViewModel(string text)
        {
            _text = text;
        }

        public string Text
        {
            get => BindingManager.Current.GetValue(this, ref _text, TextAccessor);
            set => BindingManager.Current.SetValue(this, ref _text, value, TextAccessor);
        }
    }
}
