// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal;

namespace XenoAtom.Terminal.UI;

public sealed class TerminalAppOptions
{
    public TerminalHostKind HostKind { get; init; } = TerminalHostKind.Inline;

    public TerminalRawModeKind RawMode { get; init; } = TerminalRawModeKind.CBreak;

    public bool DisableInputEcho { get; init; } = true;

    public bool EnableMouse { get; init; } = true;

    public TerminalMouseMode MouseMode { get; init; } = TerminalMouseMode.Move;

    public bool EnableBracketedPaste { get; init; } = true;
}
