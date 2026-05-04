// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Reflection;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Rendering;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class TabControlRenderingTests
{
    [TestMethod]
    public void TabControl_Renders_TabHeaders_Like_Buttons_With_Pressed_State()
    {
        var tabControl = new TabControl(
            new TabPage("One", new TextBlock("A")),
            new TabPage("Two", new TextBlock("B")));

        var theme = Theme.FromScheme(ColorScheme.RootLoopsDark);
        tabControl.Style(theme);
        tabControl.Style(TabControlStyle.Legacy);

        tabControl.Measure(new Size(40, 6));
        tabControl.Arrange(new Rectangle(0, 0, 40, 6));

        var buffer = new CellBuffer(40, 6);
        buffer.Clear(theme.BaseTextStyle());

        typeof(Visual).GetMethod("RenderTree", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(tabControl, new object[] { buffer });

        var cells = (Style[])typeof(CellBuffer).GetField("_cells", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(buffer)!;

        Assert.IsTrue(cells[0].TryGetBackground(out var tabBg), "Expected tab header cell to have a background color.");
        Assert.IsTrue(cells[39].TryGetBackground(out var stripBg), "Expected strip cell to have a background color.");
        Assert.AreNotEqual(stripBg, tabBg, "Expected tab header background to differ from the header strip background.");

        var pressedFill = theme.ControlFillPressed ?? theme.Selection ?? throw new AssertFailedException("Theme is expected to provide a pressed background.");
        typeof(TabControl).GetProperty("PressedIndex", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(tabControl, 0);
        typeof(TabControl).GetProperty("PressedPart", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(tabControl, TabControl.TabHeaderPart.Tab);
        typeof(TabControl).GetProperty("IsPressedInside", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(tabControl, true);

        buffer.Clear(theme.BaseTextStyle());
        typeof(Visual).GetMethod("RenderTree", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(tabControl, new object[] { buffer });
        cells = (Style[])typeof(CellBuffer).GetField("_cells", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(buffer)!;

        Assert.IsTrue(cells[0].TryGetBackground(out var pressedBg));
        Assert.IsTrue(cells[39].TryGetBackground(out var stripBgPressed), "Expected strip cell to have a background color.");

        var expected = pressedFill.Kind == ColorKind.RgbA ? BlendLinear(pressedFill, stripBgPressed) : pressedFill;
        AssertClose(expected, pressedBg);
    }

    [TestMethod]
    public void TabControl_Applies_TabContentTemplateFactory()
    {
        var tabControl = new TabControl(
            new TabPage("One", new TextBlock("A")));

        tabControl.Style(Theme.FromScheme(ColorScheme.RootLoopsDark));
        tabControl.Style(TabControlStyle.Rounded);

        tabControl.Measure(new Size(20, 6));
        tabControl.Arrange(new Rectangle(0, 0, 20, 6));

        var buffer = new CellBuffer(20, 6);
        buffer.Clear(tabControl.GetTheme().BaseTextStyle());

        typeof(Visual).GetMethod("RenderTree", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(tabControl, new object[] { buffer });

        var scalars = (int[])typeof(CellBuffer).GetField("_scalars", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(buffer)!;

        var expectedTopLeft = LineGlyphs.Rounded.TopLeft.Value;
        Assert.AreEqual(expectedTopLeft, scalars[buffer.Width], "Expected the tab content to be wrapped by the rounded border template.");
    }

    [TestMethod]
    public void TabControl_Default_Renders_Attached_Rounded_Header_And_Content_Frame()
    {
        var tabControl = new TabControl(
            new TabPage("One", new TextBlock("A")),
            new TabPage("Two", new TextBlock("B")));

        var theme = Theme.FromScheme(ColorScheme.RootLoopsDark);
        tabControl.Style(theme);

        tabControl.Measure(new Size(24, 8));
        tabControl.Arrange(new Rectangle(0, 0, 24, 8));

        var buffer = new CellBuffer(24, 8);
        buffer.Clear(theme.BaseTextStyle());

        typeof(Visual).GetMethod("RenderTree", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(tabControl, new object[] { buffer });

        var scalars = (int[])typeof(CellBuffer).GetField("_scalars", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(buffer)!;

        Assert.AreEqual(LineGlyphs.Rounded.TopLeft.Value, scalars[0], "Expected the selected tab to use a rounded top-left corner.");

        var separatorRow = scalars.AsSpan(buffer.Width * 2, buffer.Width);
        Assert.AreEqual(LineGlyphs.Rounded.BottomRight.Value, separatorRow[0], "Expected the first selected tab edge to connect with a rounded left hook.");
        Assert.AreEqual(new Rune(' ').Value, separatorRow[1], "Expected the separator row to stay open beneath the selected tab interior.");
        Assert.IsTrue(separatorRow.IndexOf(LineGlyphs.Rounded.BottomLeft.Value) >= 0 || separatorRow.IndexOf(LineGlyphs.Rounded.BottomRight.Value) >= 0, "Expected the separator row to contain a tab joint glyph.");
        Assert.IsTrue(separatorRow.IndexOf(LineGlyphs.Rounded.Horizontal.Value) >= 0, "Expected the separator row to continue after the selected tab.");
    }

    [TestMethod]
    public void TabControl_Style_Factory_Switch_Updates_Content_Border_Glyphs()
    {
        var useDouble = new State<bool>(false);
        var tabControl = new TabControl(
                new TabPage("One", new TextBlock("A")),
                new TabPage("Two", new TextBlock("B")))
            .Style(() => useDouble.Value ? TabControlStyle.Double : TabControlStyle.Rounded);

        using var driver = new TerminalAppTestDriver(tabControl, TerminalHostKind.Fullscreen, new TerminalSize(30, 8));
        driver.Tick();

        var screen = new AnsiTestScreen(30, 8);
        screen.Apply(driver.Backend.GetOutText());
        StringAssert.Contains(screen.GetText(), char.ConvertFromUtf32(LineGlyphs.Rounded.TopLeft.Value));

        useDouble.Value = true;
        driver.Tick();

        screen = new AnsiTestScreen(30, 8);
        screen.Apply(driver.Backend.GetOutText());
        StringAssert.Contains(screen.GetText(), char.ConvertFromUtf32(LineGlyphs.Double.TopLeft.Value));
    }

    [TestMethod]
    public void TabControl_Selected_First_Visible_Tab_Uses_Rounded_Left_Join_When_Overflow_Is_Shown()
    {
        var tabControl = new TabControl(
            new TabPage("Status", new TextBlock("A")),
            new TabPage("Logs", new TextBlock("B")),
            new TabPage("Metrics", new TextBlock("C")),
            new TabPage("Search", new TextBlock("D")),
            new TabPage("Preview", new TextBlock("E")),
            new TabPage("History", new TextBlock("F")));

        var theme = Theme.FromScheme(ColorScheme.RootLoopsDark);
        tabControl.Style(theme);

        tabControl.Measure(new Size(36, 8));
        tabControl.Arrange(new Rectangle(0, 0, 36, 8));

        var buffer = new CellBuffer(36, 8);
        buffer.Clear(theme.BaseTextStyle());

        typeof(Visual).GetMethod("RenderTree", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(tabControl, new object[] { buffer });

        var layouts = (System.Collections.IEnumerable)typeof(TabControl)
            .GetField("_headerLayouts", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(tabControl)!;
        var enumerator = layouts.GetEnumerator();
        Assert.IsTrue(enumerator.MoveNext(), "Expected at least one visible tab layout.");
        var firstLayout = enumerator.Current!;
        var start = (int)firstLayout.GetType().GetProperty("Start")!.GetValue(firstLayout)!;

        var scalars = (int[])typeof(CellBuffer).GetField("_scalars", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(buffer)!;
        var separatorRow = scalars.AsSpan(buffer.Width * 2, buffer.Width);

        Assert.AreEqual(LineGlyphs.Rounded.BottomRight.Value, separatorRow[start], "Expected the first visible selected tab to use a rounded left join above the separator.");
    }

    [TestMethod]
    public void TabControl_First_Visible_Inactive_Tab_Uses_Straight_Separator_Join_When_Overflow_Is_Shown()
    {
        var tabControl = new TabControl(
            new TabPage("Status", new TextBlock("A")),
            new TabPage("Logs", new TextBlock("B")),
            new TabPage("Metrics", new TextBlock("C")),
            new TabPage("Search", new TextBlock("D")),
            new TabPage("Preview", new TextBlock("E")),
            new TabPage("History", new TextBlock("F")))
        {
            SelectedIndex = 1,
        };

        var theme = Theme.FromScheme(ColorScheme.RootLoopsDark);
        tabControl.Style(theme);

        tabControl.Measure(new Size(36, 8));
        tabControl.Arrange(new Rectangle(0, 0, 36, 8));

        var buffer = new CellBuffer(36, 8);
        buffer.Clear(theme.BaseTextStyle());

        typeof(Visual).GetMethod("RenderTree", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(tabControl, new object[] { buffer });

        var layouts = (System.Collections.IEnumerable)typeof(TabControl)
            .GetField("_headerLayouts", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(tabControl)!;
        var enumerator = layouts.GetEnumerator();
        Assert.IsTrue(enumerator.MoveNext(), "Expected at least one visible tab layout.");
        var firstLayout = enumerator.Current!;
        var start = (int)firstLayout.GetType().GetProperty("Start")!.GetValue(firstLayout)!;

        var scalars = (int[])typeof(CellBuffer).GetField("_scalars", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(buffer)!;
        var separatorRow = scalars.AsSpan(buffer.Width * 2, buffer.Width);

        Assert.AreEqual(LineGlyphs.Single.TeeBottom.Value, separatorRow[start], "Expected the first visible inactive tab to use a straight separator join instead of a rounded hook or vertical drop.");
    }

    [TestMethod]
    public void TabControl_Renders_Close_Button_Hover_With_Error_Background()
    {
        var tabControl = new TabControl(
            new TabPage("One", new TextBlock("A")) { ShowCloseButton = true });

        var theme = Theme.FromScheme(ColorScheme.RootLoopsDark);
        tabControl.Style(theme);
        tabControl.Style(TabControlStyle.Legacy);

        tabControl.Measure(new Size(20, 6));
        tabControl.Arrange(new Rectangle(0, 0, 20, 6));

        typeof(TabControl).GetProperty("HoveredIndex", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(tabControl, 0);
        typeof(TabControl).GetProperty("HoveredPart", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(tabControl, TabControl.TabHeaderPart.CloseButton);

        var buffer = new CellBuffer(20, 6);
        buffer.Clear(theme.BaseTextStyle());

        typeof(Visual).GetMethod("RenderTree", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(tabControl, new object[] { buffer });

        var cells = (Style[])typeof(CellBuffer).GetField("_cells", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(buffer)!;
        var closeStyle = cells[8];

        Assert.IsTrue(closeStyle.TryGetBackground(out var closeBg), "Expected close button cell to have a background color.");
        AssertClose(theme.Error ?? throw new AssertFailedException("Theme is expected to provide an error color."), closeBg);
    }

    [TestMethod]
    public void TabControl_Focused_Tab_Style_Does_Not_Add_Underline()
    {
        var theme = Theme.FromScheme(ColorScheme.RootLoopsDark);

        var defaultFocused = TabControlStyle.Default.ResolveTabStyle(
            theme,
            enabled: true,
            focused: true,
            selected: true,
            hovered: false,
            pressed: false);
        var compactFocused = TabControlStyle.Compact.ResolveTabStyle(
            theme,
            enabled: true,
            focused: true,
            selected: true,
            hovered: false,
            pressed: false);
        var legacyFocused = TabControlStyle.Legacy.ResolveTabStyle(
            theme,
            enabled: true,
            focused: true,
            selected: true,
            hovered: false,
            pressed: false);

        Assert.AreEqual(TextStyle.None, defaultFocused.TextStyle & TextStyle.Underline);
        Assert.AreEqual(TextStyle.None, compactFocused.TextStyle & TextStyle.Underline);
        Assert.AreEqual(TextStyle.None, legacyFocused.TextStyle & TextStyle.Underline);
    }

    private static void AssertClose(Color expected, Color actual)
    {
        Assert.AreEqual(ColorKind.Rgb, actual.Kind);

        Assert.IsLessThanOrEqualTo(1, Math.Abs(expected.R - actual.R));
        Assert.IsLessThanOrEqualTo(1, Math.Abs(expected.G - actual.G));
        Assert.IsLessThanOrEqualTo(1, Math.Abs(expected.B - actual.B));
    }

    private static Color BlendLinear(Color src, Color dst)
    {
        Assert.AreEqual(ColorKind.RgbA, src.Kind);
        Assert.AreEqual(ColorKind.Rgb, dst.Kind);

        var sa = src.A / 255.0;
        var invSa = 1.0 - sa;

        var srcR = SrgbToLinear(src.R);
        var srcG = SrgbToLinear(src.G);
        var srcB = SrgbToLinear(src.B);

        var dstR = SrgbToLinear(dst.R);
        var dstG = SrgbToLinear(dst.G);
        var dstB = SrgbToLinear(dst.B);

        var outR = (srcR * sa) + (dstR * invSa);
        var outG = (srcG * sa) + (dstG * invSa);
        var outB = (srcB * sa) + (dstB * invSa);

        return Color.Rgb(LinearToSrgb(outR), LinearToSrgb(outG), LinearToSrgb(outB));
    }

    private static double SrgbToLinear(byte value)
    {
        var srgb = value / 255.0;
        return srgb <= 0.04045 ? srgb / 12.92 : Math.Pow((srgb + 0.055) / 1.055, 2.4);
    }

    private static byte LinearToSrgb(double linear)
    {
        linear = Math.Clamp(linear, 0.0, 1.0);
        var srgb = linear <= 0.0031308 ? 12.92 * linear : (1.055 * Math.Pow(linear, 1.0 / 2.4)) - 0.055;
        var value = (int)Math.Round(srgb * 255.0);
        return (byte)Math.Clamp(value, 0, 255);
    }
}
