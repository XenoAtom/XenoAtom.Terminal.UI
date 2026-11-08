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
/// example <see cref="InMemoryTerminalBackend"/>). Input methods push events into that backend, so they take the same path as
/// events from a real terminal: input relay, mouse mode filtering, focus scoping and the exit gesture all stay in the loop.
/// </para>
/// <para>
/// <b>Stepped mode</b>: <see cref="Start"/>, then <see cref="Tick()"/> or <see cref="Tick(TimeSpan)"/> for each frame, then
/// <see cref="Stop"/> (or <see cref="Dispose"/>). In this mode every input method also runs one frame, so the input has been
/// processed when it returns. <b>Running mode</b>: <see cref="Run(Func{TerminalAppDriver, bool})"/> runs the app's real loop on
/// the calling thread and invokes the callback after each frame; input methods called from the callback only queue their events.
/// </para>
/// <para>
/// <b>Time</b>: the app reads time from <see cref="TerminalApp.TimeProvider"/>. <see cref="Tick(TimeSpan)"/> moves this app's
/// time forward by the given amount, so animations, tooltip delays, toast durations and command sequence timeouts elapse
/// deterministically whatever the provider. With a controllable provider such as <c>FakeTimeProvider</c>, advancing the provider
/// has the same effect.
/// </para>
/// <para>
/// All members must be called on the thread that called <see cref="Start"/> or <see cref="Run(Func{TerminalAppDriver, bool})"/>,
/// which is the app's UI thread. One driver per test thread lets tests run in parallel. The driver does not own the app or its
/// terminal: dispose them as usual once the driver is disposed.
/// </para>
/// </remarks>
public sealed class TerminalAppDriver : IDisposable
{
    private readonly TerminalApp _app;
    private readonly VirtualTerminalBackend _backend;
    private readonly TerminalScreen _screen;
    private long _timeOffsetTicks;
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
    /// Gets the backend of the app's terminal, through which input is injected and the raw output can be read.
    /// </summary>
    public VirtualTerminalBackend Backend => _backend;

    /// <summary>
    /// Gets a read-only view of the last rendered frame.
    /// </summary>
    public TerminalScreen Screen => _screen;

    /// <summary>
    /// Gets the app's current time, in ticks of <see cref="TerminalApp.TimeProvider"/>, including the time moved by <see cref="Tick(TimeSpan)"/>.
    /// </summary>
    public long Timestamp => _app.TimeProvider.GetTimestamp() + _timeOffsetTicks;

    /// <summary>
    /// Starts the app on the calling thread without blocking (stepped mode). Follow with <see cref="Tick()"/> calls.
    /// </summary>
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
    /// <exception cref="InvalidOperationException">The driver is not started (see <see cref="Start"/>).</exception>
    public void Tick()
    {
        ThrowIfNotStepping();
        _app.Tick(Timestamp);
    }

    /// <summary>
    /// Moves the app's time forward by <paramref name="advance"/>, then runs one frame.
    /// </summary>
    /// <param name="advance">The time to add to the app's clock before the frame.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="advance"/> is negative.</exception>
    /// <exception cref="InvalidOperationException">The driver is not started (see <see cref="Start"/>).</exception>
    public void Tick(TimeSpan advance)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(advance, TimeSpan.Zero);
        ThrowIfNotStepping();
        _timeOffsetTicks += ToTimestampTicks(advance);
        _app.Tick(Timestamp);
    }

    /// <summary>
    /// Runs frames at the app's current time until <paramref name="condition"/> returns <see langword="true"/>.
    /// </summary>
    /// <param name="condition">The condition checked after each frame.</param>
    /// <param name="maxFrames">The maximum number of frames to run.</param>
    /// <exception cref="ArgumentNullException"><paramref name="condition"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxFrames"/> is not positive.</exception>
    /// <exception cref="TimeoutException">The condition is still false after <paramref name="maxFrames"/> frames.</exception>
    /// <exception cref="InvalidOperationException">The driver is not started (see <see cref="Start"/>).</exception>
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
    /// <exception cref="InvalidOperationException">The driver is not started (see <see cref="Start"/>).</exception>
    public void TickUntil(Func<bool> condition, TimeSpan advancePerFrame, int maxFrames)
    {
        ArgumentNullException.ThrowIfNull(condition);
        ArgumentOutOfRangeException.ThrowIfLessThan(advancePerFrame, TimeSpan.Zero);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(maxFrames, 0);
        ThrowIfNotStepping();

        for (var i = 0; i < maxFrames; i++)
        {
            _timeOffsetTicks += ToTimestampTicks(advancePerFrame);
            _app.Tick(Timestamp);
            if (condition())
            {
                return;
            }
        }

        throw new TimeoutException($"The condition was still false after {maxFrames} frames.");
    }

    /// <summary>
    /// Ends a run started with <see cref="Start"/>. Does nothing when the driver is not started.
    /// </summary>
    public void Stop()
    {
        if (!_isStepping)
        {
            return;
        }

        _isStepping = false;
        _app.EndRun();
    }

    /// <summary>
    /// Runs the app's real loop on the calling thread (running mode) and invokes <paramref name="onFrame"/> on that thread after
    /// each frame. Returns when the callback returns <see langword="false"/> or the app stops.
    /// </summary>
    /// <remarks>
    /// While the callback is attached, the loop keeps producing frames at its active frame rate even when the app is idle, so the
    /// callback keeps being called; frames happen in real time, not in the app's injected time.
    /// </remarks>
    /// <param name="onFrame">
    /// Called after each frame with this driver; inspect <see cref="Screen"/>, inject input, and return <see langword="false"/> to stop the app.
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="onFrame"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">The driver is already started or running, or the app is already running.</exception>
    /// <exception cref="ObjectDisposedException">The driver is disposed.</exception>
    public void Run(Func<TerminalAppDriver, bool> onFrame) => Run(onFrame, CancellationToken.None);

    /// <summary>
    /// Runs the app's real loop on the calling thread (running mode) and invokes <paramref name="onFrame"/> on that thread after
    /// each frame. Returns when the callback returns <see langword="false"/>, the app stops or <paramref name="cancellationToken"/> is canceled.
    /// </summary>
    /// <param name="onFrame">
    /// Called after each frame with this driver; inspect <see cref="Screen"/>, inject input, and return <see langword="false"/> to stop the app.
    /// </param>
    /// <param name="cancellationToken">A token that stops the run when canceled.</param>
    /// <exception cref="ArgumentNullException"><paramref name="onFrame"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">The driver is already started or running, or the app is already running.</exception>
    /// <exception cref="ObjectDisposedException">The driver is disposed.</exception>
    public void Run(Func<TerminalAppDriver, bool> onFrame, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(onFrame);
        ThrowIfDisposed();
        ThrowIfActive();

        _isRunning = true;
        _app.FrameCompleted = () =>
        {
            if (!onFrame(this))
            {
                _app.Stop();
            }
        };

        try
        {
            _app.Run(cancellationToken);
        }
        finally
        {
            _app.FrameCompleted = null;
            _isRunning = false;
        }
    }

    /// <summary>
    /// Sends a key press without modifiers (for example <see cref="TerminalKey.Enter"/> or <see cref="TerminalKey.Up"/>).
    /// </summary>
    /// <param name="key">The key.</param>
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
    public void SendKey(TerminalKey key, TerminalModifiers modifiers)
    {
        if (key == TerminalKey.Space)
        {
            SendKey(' ', modifiers);
            return;
        }

        char? ch = key switch
        {
            TerminalKey.Enter => '\r',
            TerminalKey.Tab => '\t',
            TerminalKey.Backspace => '\b',
            TerminalKey.Escape => '\x1b',
            _ => null,
        };

        Push(new TerminalKeyEvent { Key = key, Char = ch, Modifiers = modifiers });
        TickAfterInput();
    }

    /// <summary>
    /// Sends a character key press without modifiers, as typing it on a terminal does: a key event followed by a text event.
    /// </summary>
    /// <param name="ch">The character.</param>
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
    public void SendKey(char ch, TerminalModifiers modifiers)
    {
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
    /// Types text, as a terminal delivers it: one key event per character, then one text event with the printable text.
    /// </summary>
    /// <param name="text">The text to type.</param>
    /// <exception cref="ArgumentNullException"><paramref name="text"/> is <see langword="null"/>.</exception>
    public void SendText(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        var hasPrintable = false;
        foreach (var ch in text)
        {
            Push(new TerminalKeyEvent { Key = TerminalKey.Unknown, Char = ch, Modifiers = TerminalModifiers.None });
            hasPrintable |= !char.IsControl(ch);
        }

        if (hasPrintable)
        {
            Push(new TerminalTextEvent { Text = text });
        }

        TickAfterInput();
    }

    /// <summary>
    /// Pastes text, as a bracketed paste from the terminal.
    /// </summary>
    /// <param name="text">The pasted text.</param>
    /// <exception cref="ArgumentNullException"><paramref name="text"/> is <see langword="null"/>.</exception>
    public void SendPaste(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        Push(new TerminalPasteEvent { Text = text });
        TickAfterInput();
    }

    /// <summary>
    /// Moves the mouse to a cell with no button pressed (hover).
    /// </summary>
    /// <param name="x">The column, from 0.</param>
    /// <param name="y">The row, from 0.</param>
    public void MoveMouse(int x, int y)
    {
        Push(new TerminalMouseEvent { X = x, Y = y, Kind = TerminalMouseKind.Move, Button = TerminalMouseButton.None });
        TickAfterInput();
    }

    /// <summary>
    /// Clicks the left mouse button at a cell (button down, then up).
    /// </summary>
    /// <param name="x">The column, from 0.</param>
    /// <param name="y">The row, from 0.</param>
    /// <remarks>
    /// The terminal turns two clicks within 500 ms at the same cell into a double click, as a real terminal does. Use
    /// <see cref="DoubleClick"/> to double-click on purpose, and different cells or a pause for two separate clicks.
    /// </remarks>
    public void Click(int x, int y) => Click(x, y, TerminalMouseButton.Left);

    /// <summary>
    /// Clicks a mouse button at a cell (button down, then up).
    /// </summary>
    /// <param name="x">The column, from 0.</param>
    /// <param name="y">The row, from 0.</param>
    /// <param name="button">The button.</param>
    /// <remarks>See <see cref="Click(int, int)"/> about double-click detection.</remarks>
    public void Click(int x, int y, TerminalMouseButton button)
    {
        Push(new TerminalMouseEvent { X = x, Y = y, Kind = TerminalMouseKind.Down, Button = button });
        Push(new TerminalMouseEvent { X = x, Y = y, Kind = TerminalMouseKind.Up, Button = button });
        TickAfterInput();
    }

    /// <summary>
    /// Double-clicks the left mouse button at a cell: a click, then the double-click press and its release.
    /// </summary>
    /// <param name="x">The column, from 0.</param>
    /// <param name="y">The row, from 0.</param>
    public void DoubleClick(int x, int y)
    {
        Push(new TerminalMouseEvent { X = x, Y = y, Kind = TerminalMouseKind.Down, Button = TerminalMouseButton.Left });
        Push(new TerminalMouseEvent { X = x, Y = y, Kind = TerminalMouseKind.Up, Button = TerminalMouseButton.Left });
        Push(new TerminalMouseEvent { X = x, Y = y, Kind = TerminalMouseKind.DoubleClick, Button = TerminalMouseButton.Left });
        Push(new TerminalMouseEvent { X = x, Y = y, Kind = TerminalMouseKind.Up, Button = TerminalMouseButton.Left });
        TickAfterInput();
    }

    /// <summary>
    /// Scrolls the mouse wheel at a cell.
    /// </summary>
    /// <param name="x">The column, from 0.</param>
    /// <param name="y">The row, from 0.</param>
    /// <param name="delta">The wheel delta, with the sign of <see cref="TerminalMouseEvent.WheelDelta"/> (positive scrolls up).</param>
    public void Wheel(int x, int y, int delta)
    {
        Push(new TerminalMouseEvent { X = x, Y = y, Kind = TerminalMouseKind.Wheel, Button = TerminalMouseButton.Wheel, WheelDelta = delta });
        TickAfterInput();
    }

    /// <summary>
    /// Resizes the terminal and notifies the app, as a terminal resize does.
    /// </summary>
    /// <param name="columns">The new width, in cells.</param>
    /// <param name="rows">The new height, in cells.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="columns"/> or <paramref name="rows"/> is not positive.</exception>
    public void Resize(int columns, int rows)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(columns, 0);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(rows, 0);
        _backend.SetSize(new TerminalSize(columns, rows), raiseEvent: true);
        TickAfterInput();
    }

    /// <summary>
    /// Forces a full layout and render pass at the app's current time, without moving time.
    /// </summary>
    /// <exception cref="InvalidOperationException">The driver is not started (see <see cref="Start"/>).</exception>
    public void Render()
    {
        ThrowIfNotStepping();
        _app.RequestFullRender();
        _app.Tick(Timestamp);
    }

    /// <summary>
    /// Ends the run when the driver is in stepped mode. The app and its terminal are not disposed.
    /// </summary>
    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        Stop();
    }

    private void Push(TerminalEvent ev) => _backend.PushEvent(ev);

    private void TickAfterInput()
    {
        if (_isStepping)
        {
            _app.Tick(Timestamp);
        }
    }

    private long ToTimestampTicks(TimeSpan interval)
        => (long)(interval.Ticks * ((double)_app.TimestampFrequency / TimeSpan.TicksPerSecond));

    private void ThrowIfNotStepping()
    {
        ThrowIfDisposed();
        if (!_isStepping)
        {
            throw new InvalidOperationException("The driver is not started. Call Start() first (stepped mode); frames are driven by the loop in Run mode.");
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
