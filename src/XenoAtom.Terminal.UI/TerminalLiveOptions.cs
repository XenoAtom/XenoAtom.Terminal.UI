// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI;

/// <summary>
/// Provides options for live rendering hosted by <see cref="Terminal"/>.
/// </summary>
/// <param name="RemoveOnEnd">True to clear the live region when it ends.</param>
public readonly record struct TerminalLiveOptions(bool RemoveOnEnd = false);
