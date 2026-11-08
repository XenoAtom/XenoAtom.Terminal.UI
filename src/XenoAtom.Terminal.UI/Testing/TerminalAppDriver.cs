// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.Backends;

namespace XenoAtom.Terminal.UI.Testing;

/// <summary>
/// Drives a <see cref="TerminalApp"/> from a test: runs it frame by frame on the calling thread, or runs its real loop with a
/// callback between frames, injects input through the app's terminal backend, moves the app's time and reads the rendered frame
/// through <see cref="Screen"/>.
/// </summary>
/// <remarks>
/// <para>
/// The app must be built over a <see cref="TerminalInstance"/> whose backend is a <see cref="VirtualTerminalBackend"/> (for
/// example <see cref="InMemoryTerminalBackend"/>). Input methods push events into that backend and the app reads them through its
/// <see cref="TerminalInstance"/>, as it reads events from a real terminal: the terminal's mouse mode filtering and double-click
/// synthesis, the app's focus scoping and the exit gesture all apply. In stepped mode the app drains the events on the UI thread
/// during <see cref="Tick()"/>; in running mode the loop's input relay thread reads them.
/// </para>
/// <para>
/// <b>Stepped mode</b>: <see cref="Start"/>, then <see cref="Tick()"/> or <see cref="Tick(TimeSpan)"/> for each frame, then
/// <see cref="Stop"/> (or <see cref="Dispose"/>). In this mode every input method also runs one frame, so the input has been
/// processed when it returns. <b>Running mode</b>: <see cref="Run(Func{TerminalAppDriver, bool})"/> runs the app's real loop on
/// the calling thread and invokes the callback after each frame; input methods called from the callback only queue their events.
/// Input methods throw <see cref="InvalidOperationException"/> outside of both modes.
/// </para>
/// <para>
/// <b>Time</b>: the app reads time from <see cref="TerminalApp.TimeProvider"/>. <see cref="Tick(TimeSpan)"/> moves this app's
/// time forward by the given amount, so animations, tooltip delays, toast durations and command sequence timeouts elapse
/// deterministically whatever the provider. The moved time belongs to the app: it is kept by later drivers of the same app and in
/// running mode. With a controllable provider such as <c>FakeTimeProvider</c>, advancing the provider has the same effect, and
/// both add up. Running mode waits between frames in real time, whatever the provider.
/// </para>
/// <para>
/// All members must be called on the thread that called <see cref="Start"/> or <see cref="Run(Func{TerminalAppDriver, bool})"/>,
/// which is the app's UI thread; other threads get an <see cref="InvalidOperationException"/>. One driver per test thread lets
/// tests run in parallel. The driver does not own the app or its terminal: dispose them as usual once the driver is disposed.
/// </para>
/// </remarks>
public sealed class TerminalAppDriver : IDisposable
{
    private readonly TerminalApp _app;
    private readonly VirtualTerminalBackend _backend;
    private readonly TerminalScreen _screen;
    private bool _isStepping;
    private bool _isRunning;
    private bool _isDisposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="TerminalAppDriver"/> class for an app that is not running yet.
    /// </summary>
    /// <param name="app">The app to drive. Its terminal must use a <see cref="VirtualTerminalBackend"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="app"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">The app's terminal backend is not a <see cref="VirtualTerminalBackend"/>.</exception>
    public TerminalAppDriver(TerminalApp app)
    {
        ArgumentNullException.ThrowIfNull(app);
        if (app.Terminal.Backend is not VirtualTerminalBackend backend)
        {
            throw new ArgumentException("The app's terminal must use a VirtualTerminalBackend (for example InMemoryTerminalBackend) so the driver can inject input.", nameof(app));
        }

        _app = app;
        _backend = backend;
        _screen = new TerminalScreen(app);
    }

    /// <summary>
    /// Gets the app being driven.
    /// </summary>
    public TerminalApp App => _app;

    /// <summary>
    /// Gets the backend of the app's terminal, through which input is injected.
    /// </summary>
    /// <remarks>
    /// To read the raw output, keep a reference to the <see cref="InMemoryTerminalBackend"/> the terminal was built with and
    /// call its <c>GetOutText()</c>.
    /// </remarks>
    public VirtualTerminalBackend Backend => _backend;

    /// <summary>
    /// Gets a read-only view of the last rendered frame.
    /// </summary>
    public TerminalScreen Screen => _screen;

    /// <summary>
    /// Gets the app's current time, in ticks of <see cref="TerminalApp.TimeProvider"/> (see <see cref="TimeProvider.TimestampFrequency"/>),
    /// including the time moved by <see cref="Tick(TimeSpan)"/>. This is the timestamp the next frame sees.
    /// </summary>
    public long Timestamp => _app.GetTimestamp();

    /// <summary>
    /// Gets a value indicating whether the app was asked to stop, for example by its exit gesture, <see cref="TerminalApp.Stop"/>
    /// or a quit command. In stepped mode frames keep running when a stop is requested; check this property to assert it.
    /// </summary>
    public bool IsStopRequested => _app.IsStopRequested;

    /// <summary>
    /// Starts the app on the calling thread without blocking (stepped mode). Follow with <see cref="Tick()"/> calls.
    /// </summary>
    /// <remarks>A driver can be started again after <see cref="Stop"/>.</remarks>
    /// <exception cref="InvalidOperationException">The driver is already started or running, or the app is already running.</exception>
    /// <exception cref="ObjectDisposedException">The driver is disposed.</exception>
    public void Start()
    {
        ThrowIfDisposed();
        ThrowIfActive();
        _app.BeginRun();
        _isStepping = true;
    }

    /// <summary>
    /// Runs one frame (pending actions, input, animations, update, layout and render) at the app's current time.
    /// </summary>
    /// <exception cref="InvalidOperationException">The driver is not started (see <see cref="Start"/>), or the call is not on the app's UI thread.</exception>
    /// <exception cref="ObjectDisposedException">The driver is disposed.</exception>
    public void Tick()
    {
        VerifyStepping();
        _app.Tick();
    }

    /// <summary>
    /// Moves the app's time forward by <paramref name="advance"/>, then runs one frame.
    /// </summary>
    /// <param name="advance">The time to add to the app's clock before the frame.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="advance"/> is negative.</exception>
    /// <exception cref="InvalidOperationException">The driver is not started (see <see cref="Start"/>), or the call is not on the app's UI thread.</exception>
    /// <exception cref="ObjectDisposedException">The driver is disposed.</exception>
    public void Tick(TimeSpan advance)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(advance, TimeSpan.Zero);
        VerifyStepping();
        _app.AdvanceTime(advance);
        _app.Tick();
    }

    /// <summary>
    /// Runs frames at the app's current time until <paramref name="condition"/> returns <see langword="true"/>.
    /// </summary>
    /// <param name="condition">The condition checked after each frame.</param>
    /// <param name="maxFrames">The maximum number of frames to run.</param>
    /// <exception cref="ArgumentNullException"><paramref name="condition"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxFrames"/> is not positive.</exception>
    /// <exception cref="TimeoutException">The condition is still false after <paramref name="maxFrames"/> frames.</exception>
    /// <exception cref="InvalidOperationException">The driver is not started (see <see cref="Start"/>), or the call is not on the app's UI thread.</exception>
    /// <exception cref="ObjectDisposedException">The driver is disposed.</exception>
    public void TickUntil(Func<bool> condition, int maxFrames) => TickUntil(condition, TimeSpan.Zero, maxFrames);

    /// <summary>
    /// Runs frames, moving the app's time forward by <paramref name="advancePerFrame"/> before each one, until
    /// <paramref name="condition"/> returns <see langword="true"/>.
    /// </summary>
    /// <param name="condition">The condition checked after each frame.</param>
    /// <param name="advancePerFrame">The time added to the app's clock before each frame.</param>
    /// <param name="maxFrames">The maximum number of frames to run.</param>
    /// <exception cref="ArgumentNullException"><paramref name="condition"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="advancePerFrame"/> is negative or <paramref name="maxFrames"/> is not positive.</exception>
    /// <exception cref="TimeoutException">The condition is still false after <paramref name="maxFrames"/> frames.</exception>
    /// <exception cref="InvalidOperationException">The driver is not started (see <see cref="Start"/>), or the call is not on the app's UI thread.</exception>
    /// <exception cref="ObjectDisposedException">The driver is disposed.</exception>
    public void TickUntil(Func<bool> condition, TimeSpan advancePerFrame, int maxFrames)
    {
        ArgumentNullException.ThrowIfNull(condition);
        ArgumentOutOfRangeException.ThrowIfLessThan(advancePerFrame, TimeSpan.Zero);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(maxFrames, 0);
        VerifyStepping();

        for (var i = 0; i < maxFrames; i++)
        {
            if (advancePerFrame > TimeSpan.Zero)
            {
                _app.AdvanceTime(advancePerFrame);
            }

            _app.Tick();
            if (condition())
            {
                return;
            }
        }

        throw new TimeoutException($"The condition was still false after {maxFrames} frames.");
    }

    /// <summary>
    /// Ends a run started with <see cref="Start"/>. Does nothing when the driver is not in stepped mode.
    /// </summary>
    /// <exception cref="InvalidOperationException">The driver is in stepped mode and the call is not on the app's UI thread.</exception>
    /// <exception cref="ObjectDisposedException">The driver is disposed.</exception>
    public void Stop()
    {
        ThrowIfDisposed();
        if (!_isStepping)
        {
            return;
        }

        _app.VerifyAccess();
        _isStepping = false;
        _app.EndRun();
    }

    /// <summary>
    /// Runs the app's real loop on the calling thread (running mode) and invokes <paramref name="onFrame"/> on that thread after
    /// each frame. Returns when the callback returns <see langword="false"/> or the app stops.
    /// </summary>
    /// <remarks>
    /// While the callback is attached, the loop keeps producing frames at its active frame rate (about 60 per second) even when
    /// the app is idle, so the callback keeps being called. The loop waits between frames in real time, whatever
    /// <see cref="TerminalApp.TimeProvider"/>: with a provider that does not move on its own, frames keep coming and the test
    /// moves time itself. Prefer the overload with a <see cref="CancellationToken"/> so a test that never returns
    /// <see langword="false"/> cannot hang.
    /// </remarks>
    /// <param name="onFrame">
    /// Called after each frame with this driver; inspect <see cref="Screen"/>, inject input, and return <see langword="false"/> to end the run.
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="onFrame"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">The driver is already started or running, or the app is already running.</exception>
    /// <exception cref="ObjectDisposedException">The driver is disposed.</exception>
    public void Run(Func<TerminalAppDriver, bool> onFrame) => Run(onFrame, CancellationToken.None);

    /// <summary>
    /// Runs the app's real loop on the calling thread (running mode) and invokes <paramref name="onFrame"/> on that thread after
    /// each frame. Returns when the callback returns <see langword="false"/>, the app stops or <paramref name="cancellationToken"/> is canceled.
    /// </summary>
    /// <remarks>
    /// See <see cref="Run(Func{TerminalAppDriver, bool})"/>. Ending the run from the callback or the token does not stop the app
    /// itself, so the same app can be run or started again afterwards. An exception thrown by the callback ends the run and is
    /// rethrown by this method.
    /// </remarks>
    /// <param name="onFrame">
    /// Called after each frame with this driver; inspect <see cref="Screen"/>, inject input, and return <see langword="false"/> to end the run.
    /// </param>
    /// <param name="cancellationToken">A token that ends the run when canceled.</param>
    /// <exception cref="ArgumentNullException"><paramref name="onFrame"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">The driver is already started or running, or the app is already running.</exception>
    /// <exception cref="ObjectDisposedException">The driver is disposed.</exception>
    public void Run(Func<TerminalAppDriver, bool> onFrame, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(onFrame);
        ThrowIfDisposed();
        ThrowIfActive();

        // The driver ends the run with its own token rather than TerminalApp.Stop(), which would leave the app stopped for good.
        using var runCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _isRunning = true;
        try
        {
            _app.Run(runCts.Token, () =>
            {
                if (!onFrame(this))
                {
                    runCts.Cancel();
                }
            });
        }
        finally
        {
            _isRunning = false;
        }
    }

    /// <summary>
    /// Sends a key press without modifiers (for example <see cref="TerminalKey.Enter"/> or <see cref="TerminalKey.Up"/>).
    /// </summary>
    /// <param name="key">The key.</param>
    /// <exception cref="ArgumentException"><paramref name="key"/> is <see cref="TerminalKey.Unknown"/>; send a character with <see cref="SendKey(char)"/> instead.</exception>
    /// <exception cref="InvalidOperationException">The driver is neither started nor running, or the call is not on the app's UI thread.</exception>
    /// <exception cref="ObjectDisposedException">The driver is disposed.</exception>
    public void SendKey(TerminalKey key) => SendKey(key, TerminalModifiers.None);

    /// <summary>
    /// Sends a key press with modifiers.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="modifiers">The modifiers held.</param>
    /// <remarks>
    /// The event carries the same <see cref="TerminalKeyEvent.Char"/> a terminal produces for the key (<c>'\r'</c> for Enter,
    /// <c>'\t'</c> for Tab, <c>'\b'</c> for Backspace, <c>ESC</c> for Escape). <see cref="TerminalKey.Space"/> is sent as the
    /// character <c>' '</c>, as a terminal does.
    /// </remarks>
    /// <exception cref="ArgumentException"><paramref name="key"/> is <see cref="TerminalKey.Unknown"/>; send a character with <see cref="SendKey(char, TerminalModifiers)"/> instead.</exception>
    /// <exception cref="InvalidOperationException">The driver is neither started nor running, or the call is not on the app's UI thread.</exception>
    /// <exception cref="ObjectDisposedException">The driver is disposed.</exception>
    public void SendKey(TerminalKey key, TerminalModifiers modifiers)
    {
        if (key == TerminalKey.Unknown)
        {
            throw new ArgumentException("TerminalKey.Unknown is not a key; send a character with SendKey(char) instead.", nameof(key));
        }

        if (key == TerminalKey.Space)
        {
            SendKey(' ', modifiers);
            return;
        }

        VerifyCanSendInput();
        Push(CreateKeyEvent(key, modifiers));
        TickAfterInput();
    }

    /// <summary>
    /// Sends a character key press without modifiers, as typing it on a terminal does: a key event followed by a text event.
    /// </summary>
    /// <param name="ch">The character.</param>
    /// <exception cref="InvalidOperationException">The driver is neither started nor running, or the call is not on the app's UI thread.</exception>
    /// <exception cref="ObjectDisposedException">The driver is disposed.</exception>
    public void SendKey(char ch) => SendKey(ch, TerminalModifiers.None);

    /// <summary>
    /// Sends a character key press with modifiers.
    /// </summary>
    /// <param name="ch">The character.</param>
    /// <param name="modifiers">The modifiers held.</param>
    /// <remarks>
    /// As on a terminal, <see cref="TerminalModifiers.Ctrl"/> with an ASCII letter sends the matching control character
    /// (<c>Ctrl+Q</c> is <see cref="TerminalChar.CtrlQ"/>), and a text event follows the key event only when no modifier other
    /// than <see cref="TerminalModifiers.Shift"/> is held and the character is printable.
    /// </remarks>
    /// <exception cref="InvalidOperationException">The driver is neither started nor running, or the call is not on the app's UI thread.</exception>
    /// <exception cref="ObjectDisposedException">The driver is disposed.</exception>
    public void SendKey(char ch, TerminalModifiers modifiers)
    {
        VerifyCanSendInput();
        var effective = ch;
        if ((modifiers & TerminalModifiers.Ctrl) != 0 && char.IsAsciiLetter(ch))
        {
            effective = TerminalChar.Ctrl(ch);
        }

        Push(new TerminalKeyEvent { Key = TerminalKey.Unknown, Char = effective, Modifiers = modifiers });
        if ((modifiers & ~TerminalModifiers.Shift) == TerminalModifiers.None && !char.IsControl(effective))
        {
            Push(new TerminalTextEvent { Text = effective.ToString() });
        }

        TickAfterInput();
    }

    /// <summary>
    /// Types text, as a terminal delivers it.
    /// </summary>
    /// <param name="text">The text to type.</param>
    /// <remarks>
    /// Each run of printable characters produces one key event per character followed by one text event with the run. Control
    /// characters produce the key a terminal reports for them: <c>'\r'</c> and <c>'\n'</c> are <see cref="TerminalKey.Enter"/>
    /// (with <see cref="TerminalKeyEvent.Char"/> <c>'\r'</c>), <c>'\t'</c> is <see cref="TerminalKey.Tab"/>, <c>'\b'</c> is
    /// <see cref="TerminalKey.Backspace"/> and <c>ESC</c> is <see cref="TerminalKey.Escape"/>. For example <c>"ab\ncd"</c> sends
    /// keys <c>a</c>, <c>b</c>, text <c>"ab"</c>, Enter, keys <c>c</c>, <c>d</c>, text <c>"cd"</c>.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="text"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">The driver is neither started nor running, or the call is not on the app's UI thread.</exception>
    /// <exception cref="ObjectDisposedException">The driver is disposed.</exception>
    public void SendText(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        VerifyCanSendInput();

        var runStart = 0;
        for (var i = 0; i < text.Length; i++)
        {
            var ch = text[i];
            if (!char.IsControl(ch))
            {
                Push(new TerminalKeyEvent { Key = TerminalKey.Unknown, Char = ch, Modifiers = TerminalModifiers.None });
                continue;
            }

            PushTextRun(text, runStart, i);
            runStart = i + 1;
            Push(ch switch
            {
                '\r' or '\n' => CreateKeyEvent(TerminalKey.Enter, TerminalModifiers.None),
                '\t' => CreateKeyEvent(TerminalKey.Tab, TerminalModifiers.None),
                '\b' => CreateKeyEvent(TerminalKey.Backspace, TerminalModifiers.None),
                '\x1b' => CreateKeyEvent(TerminalKey.Escape, TerminalModifiers.None),
                _ => new TerminalKeyEvent { Key = TerminalKey.Unknown, Char = ch, Modifiers = TerminalModifiers.None },
            });
        }

        PushTextRun(text, runStart, text.Length);
        TickAfterInput();
    }

    /// <summary>
    /// Pastes text, as a bracketed paste from the terminal.
    /// </summary>
    /// <param name="text">The pasted text.</param>
    /// <exception cref="ArgumentNullException"><paramref name="text"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">The driver is neither started nor running, or the call is not on the app's UI thread.</exception>
    /// <exception cref="ObjectDisposedException">The driver is disposed.</exception>
    public void SendPaste(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        VerifyCanSendInput();
        Push(new TerminalPasteEvent { Text = text });
        TickAfterInput();
    }

    /// <summary>
    /// Sends raw terminal input (characters and escape sequences) through the same VT input decoder the real backends use, so the
    /// events the app receives are the ones a terminal sending these bytes would produce.
    /// </summary>
    /// <param name="input">The raw input, for example <c>"\x1b[A"</c> for the Up arrow or an SGR mouse report.</param>
    /// <remarks>
    /// The input is treated as complete: a pending partial sequence is flushed afterwards, as the real backends do when the input
    /// goes idle, so a lone <c>ESC</c> is an Escape key. To feed a sequence in several chunks, call
    /// <see cref="VirtualTerminalBackend.FeedInput(ReadOnlySpan{char}, bool)"/> on <see cref="Backend"/> directly. Use this method
    /// for input the typed methods cannot express (keyboard protocol sequences, mouse reports with modifiers); use
    /// <see cref="SendKey(TerminalKey)"/>, <see cref="SendText"/> and the mouse methods for everyday input.
    /// </remarks>
    /// <exception cref="InvalidOperationException">The driver is neither started nor running, or the call is not on the app's UI thread.</exception>
    /// <exception cref="ObjectDisposedException">The driver is disposed.</exception>
    public void SendRaw(ReadOnlySpan<char> input)
    {
        VerifyCanSendInput();
        _backend.FeedInput(input);
        TickAfterInput();
    }

    /// <summary>
    /// Moves the mouse to a cell with no button pressed (hover).
    /// </summary>
    /// <param name="x">The column, from 0.</param>
    /// <param name="y">The row, from 0.</param>
    /// <exception cref="InvalidOperationException">The driver is neither started nor running, or the call is not on the app's UI thread.</exception>
    /// <exception cref="ObjectDisposedException">The driver is disposed.</exception>
    public void MoveMouse(int x, int y)
    {
        VerifyCanSendInput();
        Push(new TerminalMouseEvent { X = x, Y = y, Kind = TerminalMouseKind.Move, Button = TerminalMouseButton.None });
        TickAfterInput();
    }

    /// <summary>
    /// Clicks the left mouse button at a cell (button down, then up).
    /// </summary>
    /// <param name="x">The column, from 0.</param>
    /// <param name="y">The row, from 0.</param>
    /// <remarks>
    /// The terminal turns two button-down events of the same button within 500 ms (real time) and within one cell of each other
    /// (adjacent cells included) into a double click, as a real terminal does. Two quick <see cref="Click(int, int)"/> calls at
    /// the same or adjacent cells therefore produce a double click; use <see cref="DoubleClick"/> to double-click on purpose, and
    /// cells at least two apart for two separate clicks.
    /// </remarks>
    /// <exception cref="InvalidOperationException">The driver is neither started nor running, or the call is not on the app's UI thread.</exception>
    /// <exception cref="ObjectDisposedException">The driver is disposed.</exception>
    public void Click(int x, int y) => Click(x, y, TerminalMouseButton.Left);

    /// <summary>
    /// Clicks a mouse button at a cell (button down, then up).
    /// </summary>
    /// <param name="x">The column, from 0.</param>
    /// <param name="y">The row, from 0.</param>
    /// <param name="button">The button.</param>
    /// <remarks>See <see cref="Click(int, int)"/> about double-click detection.</remarks>
    /// <exception cref="InvalidOperationException">The driver is neither started nor running, or the call is not on the app's UI thread.</exception>
    /// <exception cref="ObjectDisposedException">The driver is disposed.</exception>
    public void Click(int x, int y, TerminalMouseButton button)
    {
        VerifyCanSendInput();
        Push(new TerminalMouseEvent { X = x, Y = y, Kind = TerminalMouseKind.Down, Button = button });
        Push(new TerminalMouseEvent { X = x, Y = y, Kind = TerminalMouseKind.Up, Button = button });
        TickAfterInput();
    }

    /// <summary>
    /// Double-clicks the left mouse button at a cell: two clicks, the second of which the terminal reports as a double click.
    /// </summary>
    /// <param name="x">The column, from 0.</param>
    /// <param name="y">The row, from 0.</param>
    /// <remarks>
    /// The driver sends down, up, down, up; the terminal synthesizes the <see cref="TerminalMouseKind.DoubleClick"/> event after
    /// the second down (see <see cref="Click(int, int)"/>), which needs mouse input enabled with a mouse mode other than
    /// <see cref="TerminalMouseMode.Off"/>.
    /// </remarks>
    /// <exception cref="InvalidOperationException">The driver is neither started nor running, or the call is not on the app's UI thread.</exception>
    /// <exception cref="ObjectDisposedException">The driver is disposed.</exception>
    public void DoubleClick(int x, int y)
    {
        VerifyCanSendInput();
        for (var i = 0; i < 2; i++)
        {
            Push(new TerminalMouseEvent { X = x, Y = y, Kind = TerminalMouseKind.Down, Button = TerminalMouseButton.Left });
            Push(new TerminalMouseEvent { X = x, Y = y, Kind = TerminalMouseKind.Up, Button = TerminalMouseButton.Left });
        }

        TickAfterInput();
    }

    /// <summary>
    /// Scrolls the mouse wheel at a cell.
    /// </summary>
    /// <param name="x">The column, from 0.</param>
    /// <param name="y">The row, from 0.</param>
    /// <param name="delta">
    /// The wheel delta (<see cref="TerminalMouseEvent.WheelDelta"/>): positive scrolls up, negative scrolls down, as the terminal's
    /// VT input decoder reports it.
    /// </param>
    /// <remarks>
    /// The event has the shape the VT input decoder produces: <see cref="TerminalMouseKind.Wheel"/> with
    /// <see cref="TerminalMouseButton.None"/>.
    /// </remarks>
    /// <exception cref="InvalidOperationException">The driver is neither started nor running, or the call is not on the app's UI thread.</exception>
    /// <exception cref="ObjectDisposedException">The driver is disposed.</exception>
    public void Wheel(int x, int y, int delta)
    {
        VerifyCanSendInput();
        Push(new TerminalMouseEvent { X = x, Y = y, Kind = TerminalMouseKind.Wheel, Button = TerminalMouseButton.None, WheelDelta = delta });
        TickAfterInput();
    }

    /// <summary>
    /// Resizes the terminal and notifies the app, as a terminal resize does.
    /// </summary>
    /// <param name="columns">The new width, in cells.</param>
    /// <param name="rows">The new height, in cells.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="columns"/> or <paramref name="rows"/> is not positive.</exception>
    /// <exception cref="InvalidOperationException">The driver is neither started nor running, or the call is not on the app's UI thread.</exception>
    /// <exception cref="ObjectDisposedException">The driver is disposed.</exception>
    public void Resize(int columns, int rows)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(columns, 0);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(rows, 0);
        VerifyCanSendInput();
        _backend.SetSize(new TerminalSize(columns, rows), raiseEvent: true);
        TickAfterInput();
    }

    /// <summary>
    /// Forces a full layout and render pass at the app's current time, without moving time.
    /// </summary>
    /// <exception cref="InvalidOperationException">The driver is not started (see <see cref="Start"/>), or the call is not on the app's UI thread.</exception>
    /// <exception cref="ObjectDisposedException">The driver is disposed.</exception>
    public void Render()
    {
        VerifyStepping();
        _app.RequestFullRender();
        _app.Tick();
    }

    /// <summary>
    /// Ends the run when the driver is in stepped mode. The app and its terminal are not disposed.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// The driver is in stepped mode and the call is not on the app's UI thread (the same rule as <see cref="TerminalApp.DisposeAsync"/>);
    /// the driver is left undisposed.
    /// </exception>
    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        if (_isStepping)
        {
            _app.VerifyAccess();
        }

        _isDisposed = true;
        if (_isStepping)
        {
            _isStepping = false;
            _app.EndRun();
        }
    }

    private void Push(TerminalEvent ev) => _backend.PushEvent(ev);

    private void PushTextRun(string text, int start, int end)
    {
        if (end > start)
        {
            Push(new TerminalTextEvent { Text = text[start..end] });
        }
    }

    private static TerminalKeyEvent CreateKeyEvent(TerminalKey key, TerminalModifiers modifiers)
    {
        char? ch = key switch
        {
            TerminalKey.Enter => '\r',
            TerminalKey.Tab => '\t',
            TerminalKey.Backspace => '\b',
            TerminalKey.Escape => '\x1b',
            _ => null,
        };

        return new TerminalKeyEvent { Key = key, Char = ch, Modifiers = modifiers };
    }

    private void TickAfterInput()
    {
        // Stepped mode: process the input now. Running mode: the loop processes it on its next frame.
        if (_isStepping)
        {
            _app.Tick();
        }
    }

    private void VerifyStepping()
    {
        ThrowIfDisposed();
        _app.VerifyAccess();
        if (!_isStepping)
        {
            throw new InvalidOperationException("The driver is not started. Call Start() first (stepped mode); frames are driven by the loop in Run mode.");
        }
    }

    private void VerifyCanSendInput()
    {
        ThrowIfDisposed();
        _app.VerifyAccess();
        if (!_isStepping && !_isRunning)
        {
            throw new InvalidOperationException("The driver is not started or running.");
        }
    }

    private void ThrowIfActive()
    {
        if (_isStepping || _isRunning)
        {
            throw new InvalidOperationException("The driver is already started or running.");
        }
    }

    private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(_isDisposed, this);
}
