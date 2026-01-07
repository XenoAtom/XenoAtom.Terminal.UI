// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI.Styling;

public static class SpinnerStyles
{
    private static SpinnerStyle S(string name, int intervalMs, params string[] frames)
        => new(name, TimeSpan.FromMilliseconds(intervalMs), frames);

    public static SpinnerStyle Line { get; } = S("Line", 80, "|", "/", "-", "\\");

    public static SpinnerStyle Line2 { get; } = S("Line2", 80, "⎺", "⎻", "⎼", "⎽");

    public static SpinnerStyle Line3 { get; } = S("Line3", 80, "─", "╱", "│", "╲");

    public static SpinnerStyle Dots { get; } = S("Dots", 80, "⠋", "⠙", "⠹", "⠸", "⠼", "⠴", "⠦", "⠧", "⠇", "⠏");

    public static SpinnerStyle Dots2 { get; } = S("Dots2", 80, "⣾", "⣽", "⣻", "⢿", "⡿", "⣟", "⣯", "⣷");

    public static SpinnerStyle Dots3 { get; } = S("Dots3", 80, "⠁", "⠂", "⠄", "⡀", "⢀", "⠠", "⠐", "⠈");

    public static SpinnerStyle Dots4 { get; } = S("Dots4", 80, "⠈", "⠐", "⠠", "⢀", "⡀", "⠄", "⠂", "⠁");

    public static SpinnerStyle Dots5 { get; } = S("Dots5", 80, "⠋", "⠙", "⠹", "⠸", "⠼", "⠴", "⠦", "⠧", "⠇", "⠏");

    public static SpinnerStyle Dots6 { get; } = S("Dots6", 80, "⠁", "⠉", "⠙", "⠚", "⠞", "⠖", "⠦", "⠤", "⠠");

    public static SpinnerStyle Dots7 { get; } = S("Dots7", 80, "⠄", "⠆", "⠇", "⠋", "⠙", "⠸", "⠰", "⠠");

    public static SpinnerStyle Dots8 { get; } = S("Dots8", 80, "⢹", "⢺", "⢼", "⣸", "⣇", "⡧", "⡗", "⡏");

    public static SpinnerStyle Dots9 { get; } = S("Dots9", 80, "⣄", "⣆", "⣇", "⣋", "⣉", "⣈", "⣐", "⣠");

    public static SpinnerStyle Dots10 { get; } = S("Dots10", 80, "⠁", "⠃", "⠇", "⠧", "⠷", "⠿", "⠾", "⠼", "⠸", "⠰", "⠠");

    public static SpinnerStyle Dots11 { get; } = S("Dots11", 80, "⠈", "⠘", "⠸", "⢸", "⣸", "⣾", "⣷", "⣧", "⣇", "⡇", "⠇", "⠃");

    public static SpinnerStyle Dots12 { get; } = S("Dots12", 80, "⢀", "⢂", "⢆", "⢎", "⢞", "⢾", "⣾", "⣼", "⣸", "⣰", "⡰", "⡠");

    public static SpinnerStyle Circle { get; } = S("Circle", 100, "◐", "◓", "◑", "◒");

    public static SpinnerStyle Circle2 { get; } = S("Circle2", 100, "◴", "◷", "◶", "◵");

    public static SpinnerStyle Circle3 { get; } = S("Circle3", 100, "◆", "◇");

    public static SpinnerStyle Circle4 { get; } = S("Circle4", 100, "○", "◔", "◑", "◕", "●", "◕", "◑", "◔");

    public static SpinnerStyle Squares { get; } = S("Squares", 80, "◰", "◳", "◲", "◱");

    public static SpinnerStyle Triangles { get; } = S("Triangles", 80, "▴", "▸", "▾", "◂");

    public static SpinnerStyle Triangles2 { get; } = S("Triangles2", 80, "◢", "◣", "◥", "◤");

    public static SpinnerStyle Arrows { get; } = S("Arrows", 80, "←", "↖", "↑", "↗", "→", "↘", "↓", "↙");

    public static SpinnerStyle Arrows2 { get; } = S("Arrows2", 80, "⇐", "⇑", "⇒", "⇓");

    public static SpinnerStyle Arrows3 { get; } = S("Arrows3", 80, "⬅", "⬆", "➡", "⬇");

    public static SpinnerStyle Arrows4 { get; } = S("Arrows4", 80, "◀", "▲", "▶", "▼");

    public static SpinnerStyle Caret { get; } = S("Caret", 80, "^", ">", "v", "<");

    public static SpinnerStyle Bars { get; } = S("Bars", 80, "▁", "▂", "▃", "▄", "▅", "▆", "▇", "█", "▇", "▆", "▅", "▄", "▃", "▂", "▁");

    public static SpinnerStyle Bars2 { get; } = S("Bars2", 80, "▏", "▎", "▍", "▌", "▋", "▊", "▉", "█");

    public static SpinnerStyle Bars3 { get; } = S("Bars3", 80, "▾", "◂", "▴", "▸");

    public static SpinnerStyle Bars4 { get; } = S("Bars4", 80, "▏", "▎", "▍", "▌", "▋", "▊", "▉", "█", "▉", "▊", "▋", "▌", "▍", "▎", "▏");

    public static SpinnerStyle Pulse { get; } = S("Pulse", 80, "░", "▒", "▓", "█", "▓", "▒");

    public static SpinnerStyle Pulse2 { get; } = S("Pulse2", 90, ".", ":", "*", ":", ".");

    public static SpinnerStyle Pulse3 { get; } = S("Pulse3", 90, ".", "·", "∙", "•", "●", "•", "∙", "·");

    public static SpinnerStyle Moon { get; } = S("Moon", 100, "◐", "◑", "◒", "◓");

    public static SpinnerStyle Moon2 { get; } = S("Moon2", 100, "○", "◔", "◑", "◕", "●", "◕", "◑", "◔");

    public static SpinnerStyle QuarterBlocks { get; } = S("QuarterBlocks", 90, "▖", "▘", "▝", "▗");

    public static SpinnerStyle Blocks { get; } = S("Blocks", 90, "▘", "▝", "▗", "▖");

    public static SpinnerStyle Blocks2 { get; } = S("Blocks2", 90, "▛", "▜", "▙", "▟");

    public static SpinnerStyle Blocks3 { get; } = S("Blocks3", 90, "▀", "▐", "▄", "▌");

    public static SpinnerStyle Blocks4 { get; } = S("Blocks4", 80, "▚", "▞");

    public static SpinnerStyle Arc { get; } = S("Arc", 90, "◜", "◠", "◝", "◞", "◡", "◟");

    public static SpinnerStyle Star { get; } = S("Star", 90, "✶", "✷", "✸", "✹", "✺", "✹", "✸", "✷");

    public static SpinnerStyle Star2 { get; } = S("Star2", 90, "+", "x", "*", "x");

    public static SpinnerStyle Asterisk { get; } = S("Asterisk", 90, "✶", "✸", "✹", "✺", "✹", "✸");

    public static SpinnerStyle Pong { get; } = S("Pong", 80, "┃", "━", "┃", "━");

    public static SpinnerStyle Pipe { get; } = S("Pipe", 80, "┤", "┘", "┴", "└", "├", "┌", "┬", "┐");

    public static SpinnerStyle Corners { get; } = S("Corners", 80, "⌜", "⌝", "⌟", "⌞");

    public static SpinnerStyle Snake { get; } = S("Snake", 70, "○", "◔", "◑", "◕", "●", "◕", "◑", "◔");

    public static SpinnerStyle Toggle { get; } = S("Toggle", 90, "□", "▣", "■", "▣");

    public static SpinnerStyle Toggle2 { get; } = S("Toggle2", 90, "□", "■");

    public static SpinnerStyle Toggle3 { get; } = S("Toggle3", 100, "▢", "▣");

    public static SpinnerStyle Grow { get; } = S("Grow", 90, ".", "o", "O", "o");

    public static SpinnerStyle Grow2 { get; } = S("Grow2", 90, "·", "∙", "•", "●", "•", "∙");

    public static SpinnerStyle Grow3 { get; } = S("Grow3", 100, "◦", "•", "●", "•");

    public static SpinnerStyle Flip { get; } = S("Flip", 80, "_", "‾");

    public static SpinnerStyle Flip2 { get; } = S("Flip2", 80, "_", "‾", "_", "‾");

    public static SpinnerStyle ChasingDots { get; } = S("ChasingDots", 80, "•", "∙", "·", "∙");

    public static SpinnerStyle ChasingDots2 { get; } = S("ChasingDots2", 80, "●", "○");

    public static SpinnerStyle Noise { get; } = S("Noise", 60, "░", "▒", "▓", "▒");

    public static SpinnerStyle Noise2 { get; } = S("Noise2", 60, "░", "▒", "▓", "█", "▓", "▒");

    public static SpinnerStyle Bounce { get; } = S("Bounce", 80, "○", "●");

    public static SpinnerStyle Bounce2 { get; } = S("Bounce2", 80, "◦", "●", "◦", "○");

    public static SpinnerStyle Hearts { get; } = S("Hearts", 120, "♡", "♥");

    // Multi-frame (same cell width) spinners.
    public static SpinnerStyle DotsBounce { get; } = S("DotsBounce", 80, "⠁  ", " ⠁ ", "  ⠁", " ⠁ ");

    public static SpinnerStyle DotsBounce2 { get; } = S("DotsBounce2", 80, "⠂  ", " ⠂ ", "  ⠂", " ⠂ ");

    public static SpinnerStyle DotSlide { get; } = S("DotSlide", 70, ".    ", " .   ", "  .  ", "   . ", "    .", "   . ", "  .  ", " .   ");

    public static SpinnerStyle DotSlide2 { get; } = S("DotSlide2", 70, "●    ", " ●   ", "  ●  ", "   ● ", "    ●", "   ● ", "  ●  ", " ●   ");

    public static SpinnerStyle ArrowSlide { get; } = S("ArrowSlide", 70, "<    ", " <   ", "  <  ", "   < ", "    <", "   < ", "  <  ", " <   ");

    public static SpinnerStyle BoxSlide { get; } = S("BoxSlide", 80, "[■    ]", "[ ■   ]", "[  ■  ]", "[   ■ ]", "[    ■]", "[   ■ ]", "[  ■  ]", "[ ■   ]");

    public static SpinnerStyle BouncingBall { get; } = S("BouncingBall", 80, "(●     )", "( ●    )", "(  ●   )", "(   ●  )", "(    ● )", "(     ●)");

    public static SpinnerStyle Runner { get; } = S("Runner", 90, "🏃    ", " 🏃   ", "  🏃  ", "   🏃 ", "    🏃", "   🏃 ", "  🏃  ", " 🏃   ");

    public static SpinnerStyle Earth { get; } = S("Earth", 120, "🌍", "🌎", "🌏");

    public static SpinnerStyle MoonEmoji { get; } = S("MoonEmoji", 120, "🌑", "🌒", "🌓", "🌔", "🌕", "🌖", "🌗", "🌘");

    public static SpinnerStyle Clock { get; } = S("Clock", 120, "🕛", "🕐", "🕑", "🕒", "🕓", "🕔", "🕕", "🕖", "🕗", "🕘", "🕙", "🕚");

    public static SpinnerStyle Weather { get; } = S("Weather", 160, "SUN", "CLD", "RAN", "STO", "SNW", "WND");

    public static SpinnerStyle Sparkle { get; } = S("Sparkle", 120, "✨  ", " ✨ ", "  ✨", " ✨ ");

    public static IReadOnlyList<SpinnerStyle> All { get; } = new[]
    {
        Line,
        Line2,
        Line3,
        Dots,
        Dots2,
        Dots3,
        Dots4,
        Dots5,
        Dots6,
        Dots7,
        Dots8,
        Dots9,
        Dots10,
        Dots11,
        Dots12,
        Circle,
        Circle2,
        Circle3,
        Circle4,
        Squares,
        Triangles,
        Triangles2,
        Arrows,
        Arrows2,
        Arrows3,
        Arrows4,
        Caret,
        Bars,
        Bars2,
        Bars3,
        Bars4,
        Pulse,
        Pulse2,
        Pulse3,
        Moon,
        Moon2,
        QuarterBlocks,
        Blocks,
        Blocks2,
        Blocks3,
        Blocks4,
        Arc,
        Star,
        Star2,
        Asterisk,
        Pong,
        Pipe,
        Corners,
        Snake,
        Toggle,
        Toggle2,
        Toggle3,
        Grow,
        Grow2,
        Grow3,
        Flip,
        Flip2,
        ChasingDots,
        ChasingDots2,
        Noise,
        Noise2,
        Bounce,
        Bounce2,
        Hearts,
        DotsBounce,
        DotsBounce2,
        DotSlide,
        DotSlide2,
        ArrowSlide,
        BoxSlide,
        BouncingBall,
        Runner,
        Earth,
        MoonEmoji,
        Clock,
        Weather,
        Sparkle,
    };
}
