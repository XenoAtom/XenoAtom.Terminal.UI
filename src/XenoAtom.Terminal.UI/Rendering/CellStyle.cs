// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI;

[Flags]
public enum CellStyle : uint
{
    None = 0,
    Invert = 1u << 0,
    Dim = 1u << 1,
    Bold = 1u << 2,

    Continuation = 1u << 3,
}
