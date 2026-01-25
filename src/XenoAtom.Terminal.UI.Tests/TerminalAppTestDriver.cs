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

    public TerminalAppTestDriver(Visual root, TerminalHostKind hostKind = TerminalHostKind.Fullscreen, TerminalSize? size = null, TerminalAppOptions? appOptions = null)
    {
        ArgumentNullException.ThrowIfNull(root);

        _backend = new InMemoryTerminalBackend(size ?? new TerminalSize(80, 25));
        _session = global::XenoAtom.Terminal.Terminal.Open(_backend, new TerminalOptions { ImplicitStartInput = true }, force: true);
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
            Culture = appOptions?.Culture ?? System.Globalization.CultureInfo.InvariantCulture,
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
}
