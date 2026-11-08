// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.Backends;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Hosting;
using XenoAtom.Terminal.UI.Styling;
using XenoAtom.Terminal.UI.Testing;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class TerminalAppDriverTests
{
    [TestMethod]
    public void Screen_ReadsTheRenderedFrame()
    {
        using var terminal = CreateTerminal(20, 5);
        var app = CreateApp(new TextBlock("Hello"), terminal);
        using var driver = new TerminalAppDriver(app);
        driver.Start();
        driver.Tick();

        var screen = driver.Screen;
        Assert.AreEqual(20, screen.Width);
        Assert.AreEqual(5, screen.Height);
        Assert.AreEqual("Hello", screen.GetLine(0).TrimEnd());
        Assert.AreEqual(20, screen.GetLine(0).Length, "Trailing spaces are kept.");
        Assert.HasCount(5, screen.GetText().Split('\n'));
        Assert.AreEqual("H", screen[0, 0].Text);
        Assert.AreEqual(" ", screen[5, 0].Text);
        Assert.IsFalse(screen[0, 0].IsWideContinuation);
    }

    [TestMethod]
    public void Screen_BeforeTheFirstFrame_Throws()
    {
        using var terminal = CreateTerminal(20, 5);
        var app = CreateApp(new TextBlock("Hello"), terminal);
        using var driver = new TerminalAppDriver(app);

        Assert.ThrowsExactly<InvalidOperationException>(() => _ = driver.Screen.Width);
        Assert.AreEqual("<no frame rendered>", driver.Screen.ToString());
    }

    [TestMethod]
    public void Screen_ReportsCellStyles()
    {
        using var terminal = CreateTerminal(20, 5);
        var app = CreateApp(new TextBlock("Hi").Style(TextBlockStyle.Default with { Foreground = Colors.Red }), terminal);
        using var driver = new TerminalAppDriver(app);
        driver.Start();
        driver.Tick();

        Assert.IsTrue(driver.Screen[0, 0].Style.TryGetForeground(out var foreground));
        Assert.AreEqual(Colors.Red, foreground);
    }

    [TestMethod]
    public void Screen_ReportsWideGlyphs()
    {
        using var terminal = CreateTerminal(20, 5);
        var app = CreateApp(new TextBlock("\U0001F600x"), terminal);
        using var driver = new TerminalAppDriver(app);
        driver.Start();
        driver.Tick();

        var screen = driver.Screen;
        Assert.AreEqual("\U0001F600", screen[0, 0].Text);
        Assert.IsFalse(screen[0, 0].IsWideContinuation);
        Assert.AreEqual(" ", screen[1, 0].Text);
        Assert.IsTrue(screen[1, 0].IsWideContinuation);
        Assert.AreEqual("x", screen[2, 0].Text);
        Assert.StartsWith("\U0001F600x", screen.GetLine(0));
    }

    [TestMethod]
    public void SendText_TypesIntoTheFocusedTextBox()
    {
        var text = new State<string?>(string.Empty);
        using var terminal = CreateTerminal(20, 5);
        var app = CreateApp(new TextBox().Text(text), terminal);
        using var driver = new TerminalAppDriver(app);
        driver.Start();
        driver.Tick();

        driver.SendText("abc");

        Assert.AreEqual("abc", text.Value);
        StringAssert.Contains(driver.Screen.GetText(), "abc");
    }

    [TestMethod]
    public void SendKey_Tab_MovesFocus()
    {
        var first = new TextBox();
        var second = new TextBox();
        using var terminal = CreateTerminal(20, 5);
        var app = CreateApp(new VStack(first, second), terminal);
        using var driver = new TerminalAppDriver(app);
        driver.Start();
        driver.Tick();
        Assert.AreSame(first, app.FocusedElement);

        driver.SendKey(TerminalKey.Tab);

        Assert.AreSame(second, app.FocusedElement);
    }

    [TestMethod]
    public void SendKey_CtrlLetter_IsAControlCharacter_NotText()
    {
        var text = new State<string?>(string.Empty);
        using var terminal = CreateTerminal(20, 5);
        var app = CreateApp(new TextBox().Text(text), terminal);
        using var driver = new TerminalAppDriver(app);
        driver.Start();
        driver.Tick();

        driver.SendKey('a', TerminalModifiers.Ctrl);
        Assert.AreEqual(string.Empty, text.Value);

        driver.SendKey('a');
        Assert.AreEqual("a", text.Value);
    }

    [TestMethod]
    public void SendPaste_PastesIntoTheFocusedTextBox()
    {
        var text = new State<string?>(string.Empty);
        using var terminal = CreateTerminal(20, 5);
        var app = CreateApp(new TextBox().Text(text), terminal);
        using var driver = new TerminalAppDriver(app);
        driver.Start();
        driver.Tick();

        driver.SendPaste("pasted");

        Assert.AreEqual("pasted", text.Value);
    }

    [TestMethod]
    public void Click_ActivatesTheButtonUnderTheMouse()
    {
        var clicks = 0;
        var button = new Button("OK").Click(() => clicks++);
        using var terminal = CreateTerminal(20, 5);
        var app = CreateApp(new VStack(new TextBlock("Title"), button), terminal);
        using var driver = new TerminalAppDriver(app);
        driver.Start();
        driver.Tick();

        var bounds = button.GetAbsoluteBounds();
        driver.Click(bounds.X, bounds.Y);

        Assert.AreEqual(1, clicks);
    }

    [TestMethod]
    public void Resize_ChangesTheScreenSize()
    {
        using var terminal = CreateTerminal(20, 5);
        var app = CreateApp(new TextBlock("Hello"), terminal);
        using var driver = new TerminalAppDriver(app);
        driver.Start();
        driver.Tick();

        driver.Resize(30, 6);

        Assert.AreEqual(30, driver.Screen.Width);
        Assert.AreEqual(6, driver.Screen.Height);
        Assert.AreEqual("Hello", driver.Screen.GetLine(0).TrimEnd());
    }

    [TestMethod]
    public void Tick_WithAdvance_DrivesTimeBasedControls()
    {
        var spinner = new Spinner();
        spinner.Style(new SpinnerStyle("Test", TimeSpan.FromMilliseconds(100), "a", "b") { TextStyle = TextStyle.None });
        using var terminal = CreateTerminal(10, 3);
        var app = CreateApp(spinner, terminal);
        using var driver = new TerminalAppDriver(app);
        driver.Start();
        driver.Tick();
        Assert.AreEqual("a", driver.Screen[0, 0].Text);

        driver.Tick(TimeSpan.FromMilliseconds(150));
        Assert.AreEqual("b", driver.Screen[0, 0].Text);

        // 250 ms after the start: frame 2.
        driver.Tick(TimeSpan.FromMilliseconds(100));
        Assert.AreEqual("a", driver.Screen[0, 0].Text);
    }

    [TestMethod]
    public void TickUntil_StopsWhenTheConditionHolds_AndTimesOutOtherwise()
    {
        var spinner = new Spinner();
        spinner.Style(new SpinnerStyle("Test", TimeSpan.FromMilliseconds(100), "a", "b") { TextStyle = TextStyle.None });
        using var terminal = CreateTerminal(10, 3);
        var app = CreateApp(spinner, terminal);
        using var driver = new TerminalAppDriver(app);
        driver.Start();

        driver.TickUntil(() => driver.Screen[0, 0].Text == "b", TimeSpan.FromMilliseconds(30), maxFrames: 10);
        Assert.AreEqual("b", driver.Screen[0, 0].Text);

        Assert.ThrowsExactly<TimeoutException>(() => driver.TickUntil(() => false, maxFrames: 3));
    }

    [TestMethod]
    public void Run_InvokesTheCallbackAfterEachFrame_AndStopsWhenItReturnsFalse()
    {
        var frames = 0;
        using var terminal = CreateTerminal(20, 5);
        var app = CreateApp(new TextBlock("Hello"), terminal);
        using var driver = new TerminalAppDriver(app);

        driver.Run(d =>
        {
            frames++;
            Assert.AreEqual("Hello", d.Screen.GetLine(0).TrimEnd());
            return frames < 3;
        });

        Assert.AreEqual(3, frames);
    }

    [TestMethod]
    public void Run_InputFromTheCallback_IsProcessedByTheLoop()
    {
        var text = new State<string?>(string.Empty);
        var frames = 0;
        using var terminal = CreateTerminal(20, 5);
        var app = CreateApp(new TextBox().Text(text), terminal);
        using var driver = new TerminalAppDriver(app);

        driver.Run(d =>
        {
            frames++;
            if (frames == 1)
            {
                d.SendText("hi");
                return true;
            }

            return text.Value != "hi" && frames < 100;
        });

        Assert.AreEqual("hi", text.Value);
    }

    [TestMethod]
    public void Start_Twice_Throws_AndTick_BeforeStart_Throws()
    {
        using var terminal = CreateTerminal(20, 5);
        var app = CreateApp(new TextBlock("Hello"), terminal);
        using var driver = new TerminalAppDriver(app);

        Assert.ThrowsExactly<InvalidOperationException>(driver.Tick);

        driver.Start();
        Assert.ThrowsExactly<InvalidOperationException>(driver.Start);
    }

    [TestMethod]
    public void Dispose_EndsTheRun_SoTheAppCanBeDrivenAgain()
    {
        using var terminal = CreateTerminal(20, 5);
        var app = CreateApp(new TextBlock("Hello"), terminal);
        var driver = new TerminalAppDriver(app);
        driver.Start();
        driver.Tick();
        driver.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(driver.Start);

        using var second = new TerminalAppDriver(app);
        second.Start();
        second.Tick();
        Assert.AreEqual("Hello", second.Screen.GetLine(0).TrimEnd());
    }

    private static TerminalInstance CreateTerminal(int width, int height)
        => new(new InMemoryTerminalBackend(new TerminalSize(width, height)));

    private static TerminalApp CreateApp(Visual root, TerminalInstance terminal)
        => new(root, terminal, new TerminalAppOptions { HostKind = TerminalHostKind.Fullscreen });
}
