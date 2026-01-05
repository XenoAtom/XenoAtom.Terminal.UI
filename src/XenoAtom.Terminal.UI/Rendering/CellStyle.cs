// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI;

[Flags]
public enum CellStyle : ulong
{
    None = 0,
    Invert = 1ul << 0,
    Dim = 1ul << 1,
    Bold = 1ul << 2,

    Continuation = 1ul << 3,
}
