// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Threading.Channels;
using System.Diagnostics.CodeAnalysis;
using XenoAtom.Terminal;
using XenoAtom.Terminal.Backends;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Hosting;
using XenoAtom.Terminal.UI.Threading;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class TerminalAppInputRelayTests
{
    [TestMethod]
    public async Task Run_ThrowsWhenInputRelayTerminatesUnexpectedly()
    {
        using var backend = new CompletingInputBackend(new TerminalSize(30, 10));
        using var session = Terminal.Open(backend, new TerminalOptions { ImplicitStartInput = true }, force: true);
        var waitBackend = new ProbeWaitBackend();
        await using var app = CreateApp(session.Instance, waitBackend);

        var runTask = Task.Run(() => app.Run());

        Assert.IsTrue(waitBackend.WaitEntered.Wait(TimeSpan.FromSeconds(2)), "The app did not enter the blocking wait.");

        backend.CompleteInput();

        InvalidOperationException exception;
        try
        {
            await runTask.WaitAsync(TimeSpan.FromSeconds(2));
            Assert.Fail("Expected the app run loop to fail when the input relay terminates unexpectedly.");
            return;
        }
        catch (InvalidOperationException ex)
        {
            exception = ex;
        }

        StringAssert.Contains(exception.Message, "input relay");
        Assert.IsInstanceOfType<ChannelClosedException>(exception.InnerException);
        Assert.IsTrue(waitBackend.WakeSignalCount >= 1, "Expected the input relay failure to wake the terminal loop.");
    }

    private static TerminalApp CreateApp(TerminalInstance terminal, ITerminalLoopWaitBackend waitBackend)
    {
        return new TerminalApp(
            new TextBlock("Relay test"),
            terminal,
            new TerminalAppOptions { HostKind = TerminalHostKind.Inline, LoopMode = TerminalLoopMode.Auto },
            loopClock: ConstantLoopClock.Instance,
            waitBackend);
    }

    private sealed class ConstantLoopClock : ITerminalLoopClock
    {
        public static readonly ConstantLoopClock Instance = new();

        public long Frequency => TimeSpan.TicksPerSecond;

        public long GetTimestamp() => 0;
    }

    private sealed class ProbeWaitBackend : ITerminalLoopWaitBackend
    {
        public readonly ManualResetEventSlim WaitEntered = new();
        private int _wakeSignalCount;

        public int WakeSignalCount => Volatile.Read(ref _wakeSignalCount);

        public TerminalLoopWaitResult WaitUntil(long deadline, AutoResetEvent wakeSignal, CancellationToken cancellationToken)
        {
            WaitEntered.Set();

            var signaled = WaitHandle.WaitAny([wakeSignal, cancellationToken.WaitHandle], TimeSpan.FromSeconds(5));
            if (signaled == 0)
            {
                Interlocked.Increment(ref _wakeSignalCount);
                return TerminalLoopWaitResult.WakeSignal;
            }

            return signaled == 1 ? TerminalLoopWaitResult.Canceled : TerminalLoopWaitResult.Deadline;
        }

        public TerminalLoopWaitDiagnostics GetDiagnosticsSnapshot() => default;

        public void Dispose() => WaitEntered.Dispose();
    }

    private sealed class CompletingInputBackend(TerminalSize size) : ITerminalBackend
    {
        private readonly InMemoryTerminalBackend _inner = new(size);
        private readonly Channel<TerminalEvent> _events = Channel.CreateUnbounded<TerminalEvent>(new UnboundedChannelOptions
        {
            SingleReader = false,
            SingleWriter = false,
            AllowSynchronousContinuations = false,
        });

        public TerminalCapabilities Capabilities => _inner.Capabilities;

        public TextWriter Out => _inner.Out;

        public TextWriter Error => _inner.Error;

        public bool IsInputRunning => _inner.IsInputRunning;

        public void CompleteInput(Exception? exception = null) => _events.Writer.TryComplete(exception);

        public TerminalSize GetSize() => _inner.GetSize();

        public bool TryGetCursorPosition(out TerminalPosition position) => _inner.TryGetCursorPosition(out position);

        public void SetCursorPosition(TerminalPosition position) => _inner.SetCursorPosition(position);

        public bool TryGetCursorVisible(out bool visible) => _inner.TryGetCursorVisible(out visible);

        public void SetCursorVisible(bool visible) => _inner.SetCursorVisible(visible);

        public bool TryGetTitle(out string title) => _inner.TryGetTitle(out title);

        public void SetTitle(string title) => _inner.SetTitle(title);

        public void SetForegroundColor(XenoAtom.Ansi.AnsiColor color) => _inner.SetForegroundColor(color);

        public void SetBackgroundColor(XenoAtom.Ansi.AnsiColor color) => _inner.SetBackgroundColor(color);

        public void ResetColors() => _inner.ResetColors();

        public bool TryGetClipboardText([NotNullWhen(true)] out string? text) => _inner.TryGetClipboardText(out text);

        public bool TrySetClipboardText(ReadOnlySpan<char> text) => _inner.TrySetClipboardText(text);

        public bool TryGetClipboardFormats([NotNullWhen(true)] out IReadOnlyList<string>? formats) => _inner.TryGetClipboardFormats(out formats);

        public bool TryGetClipboardData(string format, [NotNullWhen(true)] out byte[]? data) => _inner.TryGetClipboardData(format, out data);

        public bool TrySetClipboardData(string format, ReadOnlySpan<byte> data) => _inner.TrySetClipboardData(format, data);

        public TerminalSize GetWindowSize() => _inner.GetWindowSize();

        public TerminalSize GetBufferSize() => _inner.GetBufferSize();

        public TerminalSize GetLargestWindowSize() => _inner.GetLargestWindowSize();

        public void SetWindowSize(TerminalSize size) => _inner.SetWindowSize(size);

        public void SetBufferSize(TerminalSize size) => _inner.SetBufferSize(size);

        public void Beep() => _inner.Beep();

        public void Initialize(TerminalOptions options) => _inner.Initialize(options);

        public void Flush() => _inner.Flush();

        public TerminalScope UseRawMode(TerminalRawModeKind kind) => _inner.UseRawMode(kind);

        public TerminalScope UseAlternateScreen() => _inner.UseAlternateScreen();

        public TerminalScope HideCursor() => _inner.HideCursor();

        public TerminalScope EnableMouse(TerminalMouseMode mode) => _inner.EnableMouse(mode);

        public TerminalScope EnableBracketedPaste() => _inner.EnableBracketedPaste();

        public TerminalScope UseTitle(string title) => _inner.UseTitle(title);

        public TerminalScope SetInputEcho(bool enabled) => _inner.SetInputEcho(enabled);

        public void Clear(TerminalClearKind kind) => _inner.Clear(kind);

        public void StartInput(TerminalInputOptions options) => _inner.StartInput(options);

        public Task StopInputAsync(CancellationToken cancellationToken) => _inner.StopInputAsync(cancellationToken);

        public bool TryReadEvent(out TerminalEvent ev) => _events.Reader.TryRead(out ev!);

        public ValueTask<TerminalEvent> ReadEventAsync(CancellationToken cancellationToken) => _events.Reader.ReadAsync(cancellationToken);

        public IAsyncEnumerable<TerminalEvent> ReadEventsAsync(CancellationToken cancellationToken) => _events.Reader.ReadAllAsync(cancellationToken);

        public void Dispose()
        {
            _events.Writer.TryComplete();
            _inner.Dispose();
        }
    }
}
