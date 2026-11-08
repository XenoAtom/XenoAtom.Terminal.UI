// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Reflection;
using XenoAtom.Terminal.Backends;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Hosting;
using XenoAtom.Terminal.UI.Input;
using XenoAtom.Terminal.UI.Layout;
using XenoAtom.Terminal.UI.Styling;
using XenoAtom.Terminal.UI.Testing;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class TerminalAppDriverTests
{
    // Running-mode tests end on this cap if a regression keeps the loop from calling back or returning.
    private static readonly TimeSpan RunCap = TimeSpan.FromSeconds(30);

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
    public void Screen_ToString_OnAnotherThread_DoesNotThrow()
    {
        using var terminal = CreateTerminal(20, 5);
        var app = CreateApp(new TextBlock("Hello"), terminal);
        using var driver = new TerminalAppDriver(app);
        driver.Start();
        driver.Tick();
        StringAssert.Contains(driver.Screen.ToString(), "Hello");

        string? text = null;
        var thread = new Thread(() => text = driver.Screen.ToString());
        thread.Start();
        thread.Join();

        Assert.AreEqual("<TerminalScreen: read it on the app's UI thread>", text);
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
    public void Screen_ReturnsMultiCodePointGraphemesWhole()
    {
        const string family = "\U0001F468‍\U0001F469‍\U0001F467";
        using var terminal = CreateTerminal(20, 5);
        var app = CreateApp(new TextBlock(family + "x"), terminal);
        using var driver = new TerminalAppDriver(app);
        driver.Start();
        driver.Tick();

        var screen = driver.Screen;
        Assert.AreEqual(family, screen[0, 0].Text);
        Assert.IsTrue(screen[1, 0].IsWideContinuation);
        Assert.AreEqual("x", screen[2, 0].Text);
        Assert.StartsWith(family + "x", screen.GetLine(0));
    }

    [TestMethod]
    public void Screen_InInlineMode_CoversTheLiveRegionOnly()
    {
        using var terminal = CreateTerminal(20, 10);
        var app = new TerminalApp(new TextBlock("Hello"), terminal, new TerminalAppOptions
        {
            HostKind = TerminalHostKind.Inline,
            TimeProvider = new ManualTimeProvider(),
        });
        using var driver = new TerminalAppDriver(app);
        driver.Start();
        driver.Tick();

        Assert.AreEqual(20, driver.Screen.Width);
        Assert.AreEqual(1, driver.Screen.Height, "The live region is one row high, not the terminal's 10 rows.");
        Assert.AreEqual("Hello", driver.Screen.GetLine(0).TrimEnd());
    }

    [TestMethod]
    public void Render_RendersAFrame()
    {
        using var terminal = CreateTerminal(20, 5);
        var app = CreateApp(new TextBlock("Hello"), terminal);
        using var driver = new TerminalAppDriver(app);
        driver.Start();

        driver.Render();
        Assert.AreEqual("Hello", driver.Screen.GetLine(0).TrimEnd());

        driver.Render();
        Assert.AreEqual("Hello", driver.Screen.GetLine(0).TrimEnd());
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
    public void SendText_ControlCharacters_AreSentAsTheirKeys()
    {
        var probe = new InputProbe();
        using var terminal = CreateTerminal(20, 5);
        var app = CreateApp(probe, terminal);
        using var driver = new TerminalAppDriver(app);
        driver.Start();
        driver.Tick();

        driver.SendText("ab\ncd\t\b\x1b");

        CollectionAssert.AreEqual(
            new[]
            {
                "key:Unknown:a", "key:Unknown:b", "text:ab",
                "key:Enter:\r",
                "key:Unknown:c", "key:Unknown:d", "text:cd",
                "key:Tab:\t", "key:Backspace:\b", "key:Escape:\x1b",
            },
            probe.Events);
    }

    [TestMethod]
    public void SendText_WithANewLine_TypesTwoLinesInATextArea()
    {
        var text = new State<string?>(string.Empty);
        using var terminal = CreateTerminal(20, 5);
        var app = CreateApp(new TextArea().Text(text), terminal);
        using var driver = new TerminalAppDriver(app);
        driver.Start();
        driver.Tick();

        driver.SendText("ab\ncd");

        Assert.AreEqual("ab\ncd", text.Value?.ReplaceLineEndings("\n"));
    }

    [TestMethod]
    public void SendKey_Space_TypesASpace()
    {
        var text = new State<string?>(string.Empty);
        using var terminal = CreateTerminal(20, 5);
        var app = CreateApp(new TextBox().Text(text), terminal);
        using var driver = new TerminalAppDriver(app);
        driver.Start();
        driver.Tick();

        driver.SendText("a");
        driver.SendKey(TerminalKey.Space);
        driver.SendText("b");

        Assert.AreEqual("a b", text.Value);
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
        var probe = new InputProbe();
        using var terminal = CreateTerminal(20, 5);
        var app = CreateApp(probe, terminal);
        using var driver = new TerminalAppDriver(app);
        driver.Start();
        driver.Tick();

        driver.SendKey('a', TerminalModifiers.Ctrl);
        CollectionAssert.AreEqual(new[] { $"key:Unknown:{TerminalChar.CtrlA}" }, probe.Events);
        Assert.AreEqual(TerminalModifiers.Ctrl, probe.LastModifiers);

        probe.Events.Clear();
        driver.SendKey('a');
        CollectionAssert.AreEqual(new[] { "key:Unknown:a", "text:a" }, probe.Events);
    }

    [TestMethod]
    public void SendKey_Unknown_Throws()
    {
        using var terminal = CreateTerminal(20, 5);
        var app = CreateApp(new TextBlock("Hello"), terminal);
        using var driver = new TerminalAppDriver(app);
        driver.Start();

        Assert.ThrowsExactly<ArgumentException>(() => driver.SendKey(TerminalKey.Unknown));
    }

    [TestMethod]
    public void SendKey_DefaultExitGesture_RequestsStop()
    {
        using var terminal = CreateTerminal(20, 5);
        var app = CreateApp(new TextBlock("Hello"), terminal);
        using var driver = new TerminalAppDriver(app);
        driver.Start();
        driver.Tick();
        Assert.IsFalse(driver.IsStopRequested);

        // The default fullscreen exit gesture is Ctrl+Q.
        driver.SendKey('q', TerminalModifiers.Ctrl);

        Assert.IsTrue(driver.IsStopRequested);
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
    public void SendRaw_TypedCharacters_AreDecodedIntoKeyAndTextEvents()
    {
        var text = new State<string?>(string.Empty);
        using var terminal = CreateTerminal(20, 5);
        var app = CreateApp(new TextBox().Text(text), terminal);
        using var driver = new TerminalAppDriver(app);
        driver.Start();
        driver.Tick();

        driver.SendRaw("abc");

        Assert.AreEqual("abc", text.Value);
        StringAssert.Contains(driver.Screen.GetText(), "abc");
    }

    [TestMethod]
    public void SendRaw_EscapeSequence_IsDecodedIntoTheKey()
    {
        var list = new ListBox<string>(["a", "b", "c"], selectedIndex: 0);
        using var terminal = CreateTerminal(20, 5);
        var app = CreateApp(list, terminal);
        using var driver = new TerminalAppDriver(app);
        driver.Start();
        driver.Tick();

        driver.SendRaw("\x1b[B"); // Down arrow
        Assert.AreEqual(1, list.SelectedIndex);

        driver.SendRaw("\x1b[A"); // Up arrow
        Assert.AreEqual(0, list.SelectedIndex);
    }

    [TestMethod]
    public void SendRaw_BracketedPaste_IsDecodedIntoAPasteEvent()
    {
        var text = new State<string?>(string.Empty);
        using var terminal = CreateTerminal(20, 5);
        var app = CreateApp(new TextBox().Text(text), terminal);
        using var driver = new TerminalAppDriver(app);
        driver.Start();
        driver.Tick();

        driver.SendRaw("\x1b[200~pasted\x1b[201~");

        Assert.AreEqual("pasted", text.Value);
    }

    [TestMethod]
    public void SendRaw_LoneEscape_IsFlushedAsTheEscapeKey()
    {
        var probe = new InputProbe();
        using var terminal = CreateTerminal(20, 5);
        var app = CreateApp(probe, terminal);
        using var driver = new TerminalAppDriver(app);
        driver.Start();
        driver.Tick();

        driver.SendRaw("\x1b");

        Assert.HasCount(1, probe.Events);
        Assert.StartsWith($"key:{TerminalKey.Escape}:", probe.Events[0]);
    }

    [TestMethod]
    public void SendRaw_SgrMouseClick_ActivatesTheButtonUnderTheMouse()
    {
        var clicks = 0;
        var button = new Button("OK").Click(() => clicks++);
        using var terminal = CreateTerminal(20, 5);
        var app = CreateApp(new VStack(new TextBlock("Title"), button), terminal);
        using var driver = new TerminalAppDriver(app);
        driver.Start();
        driver.Tick();

        // SGR mouse reports are 1-based.
        var bounds = button.GetAbsoluteBounds();
        var x = bounds.X + 1;
        var y = bounds.Y + 1;
        driver.SendRaw($"\x1b[<0;{x};{y}M\x1b[<0;{x};{y}m");

        Assert.AreEqual(1, clicks);
    }

    [TestMethod]
    public void SendRaw_OutsideOfAStartOrRun_Throws()
    {
        using var terminal = CreateTerminal(20, 5);
        var app = CreateApp(new TextBlock("Hello"), terminal);
        using var driver = new TerminalAppDriver(app);

        Assert.ThrowsExactly<InvalidOperationException>(() => driver.SendRaw("a"));
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
    public void DoubleClick_DeliversExactlyOneDoubleClick()
    {
        var probe = new InputProbe();
        using var terminal = CreateTerminal(20, 5);
        var app = CreateApp(probe, terminal);
        using var driver = new TerminalAppDriver(app);
        driver.Start();
        driver.Tick();

        driver.DoubleClick(1, 0);

        Assert.AreEqual(1, probe.DoubleClicks);
        Assert.AreEqual(2, probe.Presses - probe.DoubleClicks, "Two button downs, then the synthesized double click.");
    }

    [TestMethod]
    public void TwoClicks_OnAdjacentRows_AreADoubleClick()
    {
        var probe = new InputProbe();
        using var terminal = CreateTerminal(20, 5);
        var app = CreateApp(probe, terminal);
        using var driver = new TerminalAppDriver(app);
        driver.Start();
        driver.Tick();

        driver.Click(1, 0);
        driver.Click(1, 1);

        Assert.AreEqual(1, probe.DoubleClicks, "The terminal treats downs within one cell and 500 ms as a double click.");
    }

    [TestMethod]
    public void Wheel_ScrollsTheControlUnderTheMouse()
    {
        var list = new ListBox<string>(["a", "b", "c", "d"], selectedIndex: 0);
        using var terminal = CreateTerminal(20, 5);
        var app = CreateApp(list, terminal);
        using var driver = new TerminalAppDriver(app);
        driver.Start();
        driver.Tick();

        var bounds = list.GetAbsoluteBounds();
        driver.Wheel(bounds.X, bounds.Y, -1);
        Assert.AreEqual(1, list.SelectedIndex, "A negative delta scrolls down.");

        driver.Wheel(bounds.X, bounds.Y, 1);
        Assert.AreEqual(0, list.SelectedIndex, "A positive delta scrolls up.");
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
    public void Tick_WithALargeAdvance_CatchesUpSkippedFrames()
    {
        var spinner = new Spinner();
        spinner.Style(new SpinnerStyle("Test", TimeSpan.FromMilliseconds(100), "a", "b", "c", "d") { TextStyle = TextStyle.None });
        using var terminal = CreateTerminal(10, 3);
        var app = CreateApp(spinner, terminal);
        using var driver = new TerminalAppDriver(app);
        driver.Start();
        driver.Tick();
        Assert.AreEqual("a", driver.Screen[0, 0].Text);

        // Frame 0 is due again at 100 ms; at 350 ms, 1 + (350 - 100) / 100 = 3 steps have elapsed: frame 3, not frame 1.
        driver.Tick(TimeSpan.FromMilliseconds(350));
        Assert.AreEqual("d", driver.Screen[0, 0].Text);
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
        using var cap = new CancellationTokenSource(RunCap);

        driver.Run(d =>
        {
            frames++;
            Assert.AreEqual("Hello", d.Screen.GetLine(0).TrimEnd());
            return frames < 3;
        }, cap.Token);

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
        using var cap = new CancellationTokenSource(RunCap);

        driver.Run(d =>
        {
            frames++;
            if (frames == 1)
            {
                d.SendText("hi");
                return true;
            }

            return text.Value != "hi" && frames < 100;
        }, cap.Token);

        Assert.AreEqual("hi", text.Value);
    }

    [TestMethod]
    public void Run_Twice_ThenStart_TheAppCanBeDrivenAgain()
    {
        using var terminal = CreateTerminal(20, 5);
        var app = CreateApp(new TextBlock("Hello"), terminal);
        using var driver = new TerminalAppDriver(app);
        using var cap = new CancellationTokenSource(RunCap);

        var firstFrames = 0;
        driver.Run(_ => ++firstFrames < 3, cap.Token);
        var secondFrames = 0;
        driver.Run(_ => ++secondFrames < 3, cap.Token);

        Assert.AreEqual(3, firstFrames);
        Assert.AreEqual(3, secondFrames);
        Assert.IsFalse(driver.IsStopRequested, "Ending a driver run does not stop the app.");

        driver.Start();
        driver.Tick();
        Assert.AreEqual("Hello", driver.Screen.GetLine(0).TrimEnd());
    }

    [TestMethod]
    public void Run_FromASecondDriver_WhileRunning_Throws_AndTheFirstRunContinues()
    {
        using var terminal = CreateTerminal(20, 5);
        var app = CreateApp(new TextBlock("Hello"), terminal);
        using var driver = new TerminalAppDriver(app);
        using var cap = new CancellationTokenSource(RunCap);
        Exception? secondError = null;
        var secondCallbacks = 0;
        var frames = 0;

        driver.Run(_ =>
        {
            frames++;
            if (frames == 1)
            {
                var thread = new Thread(() =>
                {
                    try
                    {
                        new TerminalAppDriver(app).Run(_ =>
                        {
                            Interlocked.Increment(ref secondCallbacks);
                            return false;
                        }, cap.Token);
                    }
                    catch (Exception ex)
                    {
                        secondError = ex;
                    }
                });
                thread.Start();
                thread.Join();
            }

            return frames < 5;
        }, cap.Token);

        Assert.IsInstanceOfType<InvalidOperationException>(secondError);
        Assert.AreEqual("The app is already running.", secondError.Message);
        Assert.AreEqual(0, secondCallbacks);
        Assert.AreEqual(5, frames, "The first run's callback keeps firing.");
    }

    [TestMethod]
    public void Run_WhenTheCallbackThrows_RethrowsAndReleasesTheApp()
    {
        using var terminal = CreateTerminal(20, 5);
        var app = CreateApp(new TextBlock("Hello"), terminal);
        using var driver = new TerminalAppDriver(app);
        using var cap = new CancellationTokenSource(RunCap);

        var error = Assert.ThrowsExactly<NotSupportedException>(() => driver.Run(_ => throw new NotSupportedException("boom"), cap.Token));
        Assert.AreEqual("boom", error.Message);

        var hook = typeof(TerminalApp).GetField("_frameCompleted", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(hook);
        Assert.IsNull(hook.GetValue(app), "The frame hook is cleared when the run ends.");

        driver.Start();
        driver.Tick();
        Assert.AreEqual("Hello", driver.Screen.GetLine(0).TrimEnd());
    }

    [TestMethod]
    public void Run_EndsWhenTheTokenIsCanceled()
    {
        using var terminal = CreateTerminal(20, 5);
        var app = CreateApp(new TextBlock("Hello"), terminal);
        using var driver = new TerminalAppDriver(app);
        using var cap = new CancellationTokenSource(RunCap);
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cap.Token);
        var frames = 0;

        driver.Run(_ =>
        {
            if (++frames == 3)
            {
                cts.Cancel();
            }

            return true;
        }, cts.Token);

        Assert.AreEqual(3, frames);
        Assert.IsFalse(cap.IsCancellationRequested);
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
    public void Stop_ThenStart_DrivesTheAppAgain()
    {
        using var terminal = CreateTerminal(20, 5);
        var app = CreateApp(new TextBlock("Hello"), terminal);
        using var driver = new TerminalAppDriver(app);
        driver.Start();
        driver.Tick();
        driver.Stop();

        Assert.ThrowsExactly<InvalidOperationException>(driver.Tick);

        driver.Start();
        driver.Tick();
        Assert.AreEqual("Hello", driver.Screen.GetLine(0).TrimEnd());
    }

    [TestMethod]
    public void Input_OutsideOfAStartOrRun_Throws()
    {
        using var terminal = CreateTerminal(20, 5);
        var app = CreateApp(new TextBlock("Hello"), terminal);
        using var driver = new TerminalAppDriver(app);

        var error = Assert.ThrowsExactly<InvalidOperationException>(() => driver.SendText("a"));
        Assert.AreEqual("The driver is not started or running.", error.Message);
        Assert.ThrowsExactly<InvalidOperationException>(() => driver.Click(0, 0));
        Assert.IsFalse(driver.Backend.TryReadEvent(out _), "Nothing was queued.");
    }

    [TestMethod]
    public void Calls_FromAnotherThread_Throw_WithoutSideEffects()
    {
        var text = new State<string?>(string.Empty);
        using var terminal = CreateTerminal(20, 5);
        var app = CreateApp(new TextBox().Text(text), terminal);
        using var driver = new TerminalAppDriver(app);
        driver.Start();
        driver.Tick();

        var errors = new List<Exception>();
        var thread = new Thread(() =>
        {
            foreach (var action in new Action[] { () => driver.SendText("x"), driver.Tick, () => driver.Tick(TimeSpan.FromSeconds(1)), driver.Stop, driver.Dispose })
            {
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    errors.Add(ex);
                }
            }
        });
        thread.Start();
        thread.Join();

        Assert.HasCount(5, errors);
        foreach (var error in errors)
        {
            Assert.IsInstanceOfType<InvalidOperationException>(error);
        }

        driver.Tick();
        Assert.AreEqual(string.Empty, text.Value, "The text was not typed.");
        driver.SendText("y");
        Assert.AreEqual("y", text.Value, "The driver is still started and not disposed.");
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
        Assert.ThrowsExactly<ObjectDisposedException>(() => driver.SendText("a"));
        Assert.ThrowsExactly<ObjectDisposedException>(() => driver.SendKey(TerminalKey.Enter));
        Assert.ThrowsExactly<ObjectDisposedException>(driver.Tick);

        using var second = new TerminalAppDriver(app);
        second.Start();
        second.Tick();
        Assert.AreEqual("Hello", second.Screen.GetLine(0).TrimEnd());
    }

    private static TerminalInstance CreateTerminal(int width, int height)
        => new(new InMemoryTerminalBackend(new TerminalSize(width, height)));

    // A clock that only moves through the driver, so no real time leaks into the tests.
    private static TerminalApp CreateApp(Visual root, TerminalInstance terminal)
        => new(root, terminal, new TerminalAppOptions { HostKind = TerminalHostKind.Fullscreen, TimeProvider = new ManualTimeProvider() });

    private sealed class InputProbe : Visual
    {
        public InputProbe()
        {
            Focusable = true;
        }

        public List<string> Events { get; } = new();

        public TerminalModifiers LastModifiers { get; private set; }

        public int Presses { get; private set; }

        public int DoubleClicks { get; private set; }

        protected override SizeHints MeasureCore(in LayoutConstraints constraints) => SizeHints.Fixed(constraints.Clamp(new Size(10, 3)));

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.RoutingPhase == RoutingPhase.Bubble)
            {
                Events.Add($"key:{e.Key}:{e.Char}");
                LastModifiers = e.Modifiers;
            }
        }

        protected override void OnTextInput(TextInputEventArgs e)
        {
            if (e.RoutingPhase == RoutingPhase.Bubble)
            {
                Events.Add($"text:{e.Text}");
            }
        }

        protected override void OnPointerPressed(PointerEventArgs e)
        {
            if (e.RoutingPhase != RoutingPhase.Bubble)
            {
                return;
            }

            Presses++;
            if (e.ClickCount == 2)
            {
                DoubleClicks++;
            }
        }
    }
}
