// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI;

/// <summary>
/// Context available while running a live inline region.
/// </summary>
public sealed class TerminalLiveContext
{
    private readonly TerminalApp _app;

    internal TerminalLiveContext(TerminalApp app)
    {
        _app = app ?? throw new ArgumentNullException(nameof(app));
    }

    public TerminalApp App => _app;

    public XenoAtom.Terminal.TerminalInstance Terminal => _app.Terminal;

    public void WriteMarkupLine(string markup) => _app.WriteMarkupLine(markup);

    public void Append(Visual visual) => _app.Append(visual);

    public void Stop() => _app.Stop();
}

