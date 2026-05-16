// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.Backends;
using XenoAtom.Terminal;
using XenoAtom.Terminal.UI.Hosting;
using System.Diagnostics;

namespace XenoAtom.Terminal.UI.Tests;

internal sealed class TerminalAppTestDriver : IDisposable
{
    private readonly InMemoryTerminalBackend _backend;
    private readonly TerminalSession _session;
    private readonly TerminalApp _app;
    private long _timestamp;
    private readonly long _tickStep;

    public TerminalAppTestDriver(Visual root, TerminalHostKind hostKind = TerminalHostKind.Fullscreen, TerminalSize? size = null, TerminalAppOptions? appOptions = null, TerminalOptions? terminalOptions = null, TerminalCapabilities? capabilities = null)
    {
        ArgumentNullException.ThrowIfNull(root);

        _backend = new InMemoryTerminalBackend(size ?? new TerminalSize(80, 25));
        ITerminalBackend terminalBackend = capabilities is null ? _backend : new CapabilitiesOverrideTerminalBackend(_backend, capabilities);
        _session = global::XenoAtom.Terminal.Terminal.Open(terminalBackend, terminalOptions ?? new TerminalOptions { ImplicitStartInput = true }, force: true);
        var effectiveOptions = new TerminalAppOptions
        {
            HostKind = hostKind,
            RawMode = appOptions?.RawMode ?? TerminalRawModeKind.CBreak,
            DisableInputEcho = appOptions?.DisableInputEcho ?? true,
            EnableMouse = appOptions?.EnableMouse ?? true,
            MouseMode = appOptions?.MouseMode ?? TerminalMouseMode.Move,
            EnableBracketedPaste = appOptions?.EnableBracketedPaste ?? true,
            ToggleDebugOverlayGesture = appOptions?.ToggleDebugOverlayGesture ?? new Input.KeyGesture(TerminalKey.F12),
            ExitGesture = appOptions?.ExitGesture,
            InitialFocusMode = appOptions?.InitialFocusMode ?? InitialFocusMode.FirstFocusable,
            Culture = appOptions?.Culture ?? System.Globalization.CultureInfo.InvariantCulture,
            LoopMode = appOptions?.LoopMode ?? TerminalLoopMode.Auto,
            GraphicsPresenter = appOptions?.GraphicsPresenter,
            UpdateWaitDuration = appOptions?.UpdateWaitDuration ?? TimeSpan.FromMilliseconds(1),
            WideRuneResolver = appOptions?.WideRuneResolver ?? TerminalWideRuneResolvers.Default,
        };
        _app = new TerminalApp(root, _session.Instance, effectiveOptions);
        _app.BeginRun();

        _timestamp = Stopwatch.GetTimestamp();
        _tickStep = Math.Max(1, Stopwatch.Frequency / 100); // ~10ms per tick
    }

    public TerminalApp App => _app;

    public InMemoryTerminalBackend Backend => _backend;

    public TerminalInstance Terminal => _session.Instance;

    public void Tick(int count = 1)
    {
        for (var i = 0; i < count; i++)
        {
            _timestamp += _tickStep;
            _app.Tick(_timestamp);
        }
    }

    public void TickUntil(Func<bool> condition, int maxTicks = 50)
    {
        ArgumentNullException.ThrowIfNull(condition);

        for (var i = 0; i < maxTicks; i++)
        {
            _timestamp += _tickStep;
            _app.Tick(_timestamp);
            if (condition())
            {
                return;
            }
        }

        Assert.Fail("Timed out waiting for condition.");
    }

    public void Dispose()
    {
        _app.EndRun();
        _session.Dispose();
    }

    private sealed class CapabilitiesOverrideTerminalBackend(InMemoryTerminalBackend inner, TerminalCapabilities capabilities) : ITerminalBackend
    {
        public TerminalCapabilities Capabilities => capabilities;

        public TextWriter Out => inner.Out;

        public TextWriter Error => inner.Error;

        public bool IsInputRunning => inner.IsInputRunning;

        public TerminalSize GetSize() => inner.GetSize();

        public bool TryGetCursorPosition(out TerminalPosition position) => inner.TryGetCursorPosition(out position);

        public void SetCursorPosition(TerminalPosition position) => inner.SetCursorPosition(position);

        public bool TryGetCursorVisible(out bool visible) => inner.TryGetCursorVisible(out visible);

        public void SetCursorVisible(bool visible) => inner.SetCursorVisible(visible);

        public bool TryGetTitle(out string title) => inner.TryGetTitle(out title);

        public void SetTitle(string title) => inner.SetTitle(title);

        public void SetForegroundColor(XenoAtom.Ansi.AnsiColor color) => inner.SetForegroundColor(color);

        public void SetBackgroundColor(XenoAtom.Ansi.AnsiColor color) => inner.SetBackgroundColor(color);

        public void ResetColors() => inner.ResetColors();

        public bool TryGetClipboardText([System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out string? text) => inner.TryGetClipboardText(out text);

        public bool TrySetClipboardText(ReadOnlySpan<char> text) => inner.TrySetClipboardText(text);

        public bool TryGetClipboardFormats([System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out IReadOnlyList<string>? formats) => inner.TryGetClipboardFormats(out formats);

        public bool TryGetClipboardData(string format, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out byte[]? data) => inner.TryGetClipboardData(format, out data);

        public bool TrySetClipboardData(string format, ReadOnlySpan<byte> data) => inner.TrySetClipboardData(format, data);

        public TerminalSize GetWindowSize() => inner.GetWindowSize();

        public TerminalSize GetBufferSize() => inner.GetBufferSize();

        public TerminalSize GetLargestWindowSize() => inner.GetLargestWindowSize();

        public void SetWindowSize(TerminalSize size) => inner.SetWindowSize(size);

        public void SetBufferSize(TerminalSize size) => inner.SetBufferSize(size);

        public void Beep() => inner.Beep();

        public void Initialize(TerminalOptions options) => inner.Initialize(options);

        public void Flush() => inner.Flush();

        public TerminalScope UseRawMode(TerminalRawModeKind kind) => inner.UseRawMode(kind);

        public TerminalScope UseAlternateScreen() => inner.UseAlternateScreen();

        public TerminalScope HideCursor() => inner.HideCursor();

        public TerminalScope EnableMouse(TerminalMouseMode mode) => inner.EnableMouse(mode);

        public TerminalScope EnableBracketedPaste() => inner.EnableBracketedPaste();

        public TerminalScope UseTitle(string title) => inner.UseTitle(title);

        public TerminalScope SetInputEcho(bool enabled) => inner.SetInputEcho(enabled);

        public void Clear(TerminalClearKind kind) => inner.Clear(kind);

        public void StartInput(TerminalInputOptions options) => inner.StartInput(options);

        public Task StopInputAsync(CancellationToken cancellationToken) => inner.StopInputAsync(cancellationToken);

        public bool TryReadEvent(out TerminalEvent ev) => inner.TryReadEvent(out ev);

        public ValueTask<TerminalEvent> ReadEventAsync(CancellationToken cancellationToken) => inner.ReadEventAsync(cancellationToken);

        public IAsyncEnumerable<TerminalEvent> ReadEventsAsync(CancellationToken cancellationToken) => inner.ReadEventsAsync(cancellationToken);

        public void Dispose() => inner.Dispose();
    }
}
