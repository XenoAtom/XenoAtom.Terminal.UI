// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Rendering;

namespace XenoAtom.Terminal.UI.Hosting;

public sealed class FullscreenHost : IDisposable
{
    private readonly TerminalInstance _terminal;
    private readonly CellBufferDiffRenderer _renderer;

    public FullscreenHost(TerminalInstance terminal)
    {
        _terminal = terminal ?? throw new ArgumentNullException(nameof(terminal));
        _renderer = new CellBufferDiffRenderer();
    }

    public void Dispose()
    {
        _renderer.Reset();
    }

    public void Render(CellBuffer buffer)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        _renderer.RenderFullscreen(_terminal, buffer);
    }

    public void Reset() => _renderer.Reset();
}

