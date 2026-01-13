// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Ansi;

namespace XenoAtom.Terminal.UI.Styling;

/// <summary>
/// A 16-color ANSI scheme (8 normal + 8 bright) with a foreground/background baseline.
/// </summary>
public sealed record AnsiColorScheme
{
    public required string Name { get; init; }

    public required AnsiColor CursorColor { get; init; }

    public required AnsiColor SelectionBackground { get; init; }

    public AnsiColor? Background { get; init; }

    public AnsiColor? Foreground { get; init; }

    public required AnsiColor Black { get; init; }

    public required AnsiColor Blue { get; init; }

    public required AnsiColor Cyan { get; init; }

    public required AnsiColor Green { get; init; }

    public required AnsiColor Purple { get; init; }

    public required AnsiColor Red { get; init; }

    public required AnsiColor White { get; init; }

    public required AnsiColor Yellow { get; init; }

    public required AnsiColor BrightBlack { get; init; }

    public required AnsiColor BrightBlue { get; init; }

    public required AnsiColor BrightCyan { get; init; }

    public required AnsiColor BrightGreen { get; init; }

    public required AnsiColor BrightPurple { get; init; }

    public required AnsiColor BrightRed { get; init; }

    public required AnsiColor BrightWhite { get; init; }

    public required AnsiColor BrightYellow { get; init; }

    public static AnsiColorScheme Terminal { get; } = new AnsiColorScheme
    {
        Name = "Terminal",
        CursorColor = AnsiColor.Basic16(7),           // white
        SelectionBackground = AnsiColor.Basic16(12),  // bright blue
        Background = null,                            // terminal default
        Foreground = null,                            // terminal default
        Black = AnsiColor.Basic16(0),
        Red = AnsiColor.Basic16(1),
        Green = AnsiColor.Basic16(2),
        Yellow = AnsiColor.Basic16(3),
        Blue = AnsiColor.Basic16(4),
        Purple = AnsiColor.Basic16(5),
        Cyan = AnsiColor.Basic16(6),
        White = AnsiColor.Basic16(7),
        BrightBlack = AnsiColor.Basic16(8),
        BrightRed = AnsiColor.Basic16(9),
        BrightGreen = AnsiColor.Basic16(10),
        BrightYellow = AnsiColor.Basic16(11),
        BrightBlue = AnsiColor.Basic16(12),
        BrightPurple = AnsiColor.Basic16(13),
        BrightCyan = AnsiColor.Basic16(14),
        BrightWhite = AnsiColor.Basic16(15),
    };

    public static AnsiColorScheme RootLoops { get; } = new AnsiColorScheme
    {
        Name = "Root Loops",
        CursorColor = AnsiColor.Rgb(0x8C, 0x9E, 0xD6),
        SelectionBackground = AnsiColor.Rgb(0x8C, 0x9E, 0xD6),
        Background = AnsiColor.Rgb(0x17, 0x1F, 0x3C),
        Foreground = AnsiColor.Rgb(0xCC, 0xD5, 0xEF),
        Black = AnsiColor.Rgb(0x28, 0x34, 0x5C),
        Blue = AnsiColor.Rgb(0x77, 0xB1, 0xFB),
        Cyan = AnsiColor.Rgb(0x24, 0xC6, 0xC6),
        Green = AnsiColor.Rgb(0x75, 0xC7, 0x3B),
        Purple = AnsiColor.Rgb(0xD6, 0x8A, 0xF7),
        Red = AnsiColor.Rgb(0xFB, 0x85, 0x90),
        White = AnsiColor.Rgb(0x8C, 0x9E, 0xD6),
        Yellow = AnsiColor.Rgb(0xE4, 0x9F, 0x27),
        BrightBlack = AnsiColor.Rgb(0x46, 0x58, 0x92),
        BrightBlue = AnsiColor.Rgb(0x9E, 0xC7, 0xFC),
        BrightCyan = AnsiColor.Rgb(0x31, 0xDE, 0xDE),
        BrightGreen = AnsiColor.Rgb(0x84, 0xE0, 0x41),
        BrightPurple = AnsiColor.Rgb(0xE1, 0xAC, 0xF9),
        BrightRed = AnsiColor.Rgb(0xFD, 0xAA, 0xAF),
        BrightWhite = AnsiColor.Rgb(0xBE, 0xCA, 0xEB),
        BrightYellow = AnsiColor.Rgb(0xF9, 0xB6, 0x4D),
    };
}
