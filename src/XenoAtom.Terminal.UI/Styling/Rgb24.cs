// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI;

public readonly record struct Rgb24(byte R, byte G, byte B)
{
    public uint Packed => (uint)((R << 16) | (G << 8) | B);

    public static Rgb24 FromPacked(uint packed)
        => new((byte)((packed >> 16) & 0xFF), (byte)((packed >> 8) & 0xFF), (byte)(packed & 0xFF));

    public string ToMarkup() => $"#{Packed:x6}";
}

