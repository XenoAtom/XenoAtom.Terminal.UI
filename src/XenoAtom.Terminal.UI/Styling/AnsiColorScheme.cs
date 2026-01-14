// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Ansi;

namespace XenoAtom.Terminal.UI.Styling;

/// <summary>
/// A 16-color ANSI scheme (8 normal + 8 bright) with a foreground/background baseline.
/// </summary>
public sealed partial record AnsiColorScheme
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
        CursorColor = AnsiColor.Basic16(7), // white
        SelectionBackground = AnsiColor.Basic16(12), // bright blue
        Background = null, // terminal default
        Foreground = null, // terminal default
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

    // https://rootloops.sh/?sugar=7&colors=9&sogginess=4&flavor=1&milk=0.94&fruit=8
    public static AnsiColorScheme RootLoopsDark { get; } = new AnsiColorScheme
    {
        Name = "Root Loops (Dark)",
        CursorColor = AnsiColor.Rgb(0x7D, 0xA9, 0xC9),
        SelectionBackground = AnsiColor.Rgb(0x7D, 0xA9, 0xC9),
        Background = AnsiColor.Rgb(0x0F, 0x1D, 0x27),
        Foreground = AnsiColor.Rgb(0xCD, 0xE0, 0xEE),
        Black = AnsiColor.Rgb(0x1F, 0x35, 0x44),
        Blue = AnsiColor.Rgb(0x50, 0x9A, 0xF6),
        Cyan = AnsiColor.Rgb(0x1F, 0xAE, 0xAE),
        Green = AnsiColor.Rgb(0x67, 0xAF, 0x34),
        Purple = AnsiColor.Rgb(0xCA, 0x64, 0xF3),
        Red = AnsiColor.Rgb(0xF7, 0x5B, 0x72),
        White = AnsiColor.Rgb(0x7D, 0xA9, 0xC9),
        Yellow = AnsiColor.Rgb(0xC9, 0x8B, 0x1A),
        BrightBlack = AnsiColor.Rgb(0x3B, 0x5E, 0x76),
        BrightBlue = AnsiColor.Rgb(0x77, 0xB1, 0xFB),
        BrightCyan = AnsiColor.Rgb(0x24, 0xC6, 0xC6),
        BrightGreen = AnsiColor.Rgb(0x75, 0xC7, 0x3B),
        BrightPurple = AnsiColor.Rgb(0xD6, 0x8A, 0xF7),
        BrightRed = AnsiColor.Rgb(0xFB, 0x85, 0x90),
        BrightWhite = AnsiColor.Rgb(0xBA, 0xD4, 0xE8),
        BrightYellow = AnsiColor.Rgb(0xE4, 0x9F, 0x27),
    };

    // https://rootloops.sh/?sugar=7&colors=9&sogginess=4&flavor=1&milk=3&fruit=8
    public static AnsiColorScheme RootLoopsLight { get; } = new AnsiColorScheme
    {
        Name = "Root Loops (Light)",
        CursorColor = AnsiColor.Rgb(0x36, 0x56, 0x6C),
        SelectionBackground = AnsiColor.Rgb(0x36, 0x56, 0x6C),
        Background = AnsiColor.Rgb(0xEE, 0xF5, 0xF9),
        Foreground = AnsiColor.Rgb(0x02, 0x06, 0x0B),
        Black = AnsiColor.Rgb(0xD5, 0xE5, 0xF1),
        Blue = AnsiColor.Rgb(0x50, 0x9A, 0xF6),
        Cyan = AnsiColor.Rgb(0x1F, 0xAE, 0xAE),
        Green = AnsiColor.Rgb(0x67, 0xAF, 0x34),
        Purple = AnsiColor.Rgb(0xCA, 0x64, 0xF3),
        Red = AnsiColor.Rgb(0xF7, 0x5B, 0x72),
        White = AnsiColor.Rgb(0x36, 0x56, 0x6C),
        Yellow = AnsiColor.Rgb(0xC9, 0x8B, 0x1A),
        BrightBlack = AnsiColor.Rgb(0x87, 0xB1, 0xD0),
        BrightBlue = AnsiColor.Rgb(0x77, 0xB1, 0xFB),
        BrightCyan = AnsiColor.Rgb(0x24, 0xC6, 0xC6),
        BrightGreen = AnsiColor.Rgb(0x75, 0xC7, 0x3B),
        BrightPurple = AnsiColor.Rgb(0xD6, 0x8A, 0xF7),
        BrightRed = AnsiColor.Rgb(0xFB, 0x85, 0x90),
        BrightWhite = AnsiColor.Rgb(0x14, 0x25, 0x30),
        BrightYellow = AnsiColor.Rgb(0xE4, 0x9F, 0x27),
    };
}
