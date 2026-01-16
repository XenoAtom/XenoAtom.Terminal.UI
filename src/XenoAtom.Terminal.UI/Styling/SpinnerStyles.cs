// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI.Styling;

/// <summary>
/// Provides a catalog of built-in <see cref="SpinnerStyle"/> definitions.
/// </summary>
public static class SpinnerStyles
{
    private static SpinnerStyle S(string name, int intervalMs, params string[] frames)
        => new(name, TimeSpan.FromMilliseconds(intervalMs), frames);

    /// <summary>
    /// Gets the <c>Line</c> spinner style.
    /// </summary>
    public static SpinnerStyle Line { get; } = S("Line", 80, "|", "/", "-", "\\");

    /// <summary>
    /// Gets the <c>Line2</c> spinner style.
    /// </summary>
    public static SpinnerStyle Line2 { get; } = S("Line2", 80, "⎺", "⎻", "⎼", "⎽");

    /// <summary>
    /// Gets the <c>Line3</c> spinner style.
    /// </summary>
    public static SpinnerStyle Line3 { get; } = S("Line3", 80, "─", "╱", "│", "╲");

    /// <summary>
    /// Gets the <c>Dots</c> spinner style.
    /// </summary>
    public static SpinnerStyle Dots { get; } = S("Dots", 80, "⠋", "⠙", "⠹", "⠸", "⠼", "⠴", "⠦", "⠧", "⠇", "⠏");

    /// <summary>
    /// Gets the <c>Dots2</c> spinner style.
    /// </summary>
    public static SpinnerStyle Dots2 { get; } = S("Dots2", 80, "⣾", "⣽", "⣻", "⢿", "⡿", "⣟", "⣯", "⣷");

    /// <summary>
    /// Gets the <c>Dots3</c> spinner style.
    /// </summary>
    public static SpinnerStyle Dots3 { get; } = S("Dots3", 80, "⠁", "⠂", "⠄", "⡀", "⢀", "⠠", "⠐", "⠈");

    /// <summary>
    /// Gets the <c>Dots4</c> spinner style.
    /// </summary>
    public static SpinnerStyle Dots4 { get; } = S("Dots4", 80, "⠈", "⠐", "⠠", "⢀", "⡀", "⠄", "⠂", "⠁");

    /// <summary>
    /// Gets the <c>Dots5</c> spinner style.
    /// </summary>
    public static SpinnerStyle Dots5 { get; } = S("Dots5", 80, "⠋", "⠙", "⠹", "⠸", "⠼", "⠴", "⠦", "⠧", "⠇", "⠏");

    /// <summary>
    /// Gets the <c>Dots6</c> spinner style.
    /// </summary>
    public static SpinnerStyle Dots6 { get; } = S("Dots6", 80, "⠁", "⠉", "⠙", "⠚", "⠞", "⠖", "⠦", "⠤", "⠠");

    /// <summary>
    /// Gets the <c>Dots7</c> spinner style.
    /// </summary>
    public static SpinnerStyle Dots7 { get; } = S("Dots7", 80, "⠄", "⠆", "⠇", "⠋", "⠙", "⠸", "⠰", "⠠");

    /// <summary>
    /// Gets the <c>Dots8</c> spinner style.
    /// </summary>
    public static SpinnerStyle Dots8 { get; } = S("Dots8", 80, "⢹", "⢺", "⢼", "⣸", "⣇", "⡧", "⡗", "⡏");

    /// <summary>
    /// Gets the <c>Dots9</c> spinner style.
    /// </summary>
    public static SpinnerStyle Dots9 { get; } = S("Dots9", 80, "⣄", "⣆", "⣇", "⣋", "⣉", "⣈", "⣐", "⣠");

    /// <summary>
    /// Gets the <c>Dots10</c> spinner style.
    /// </summary>
    public static SpinnerStyle Dots10 { get; } = S("Dots10", 80, "⠁", "⠃", "⠇", "⠧", "⠷", "⠿", "⠾", "⠼", "⠸", "⠰", "⠠");

    /// <summary>
    /// Gets the <c>Dots11</c> spinner style.
    /// </summary>
    public static SpinnerStyle Dots11 { get; } = S("Dots11", 80, "⠈", "⠘", "⠸", "⢸", "⣸", "⣾", "⣷", "⣧", "⣇", "⡇", "⠇", "⠃");

    /// <summary>
    /// Gets the <c>Dots12</c> spinner style.
    /// </summary>
    public static SpinnerStyle Dots12 { get; } = S("Dots12", 80, "⢀", "⢂", "⢆", "⢎", "⢞", "⢾", "⣾", "⣼", "⣸", "⣰", "⡰", "⡠");

    /// <summary>
    /// Gets the <c>Circle</c> spinner style.
    /// </summary>
    public static SpinnerStyle Circle { get; } = S("Circle", 100, "◐", "◓", "◑", "◒");

    /// <summary>
    /// Gets the <c>Circle2</c> spinner style.
    /// </summary>
    public static SpinnerStyle Circle2 { get; } = S("Circle2", 100, "◴", "◷", "◶", "◵");

    /// <summary>
    /// Gets the <c>Circle3</c> spinner style.
    /// </summary>
    public static SpinnerStyle Circle3 { get; } = S("Circle3", 100, "◆", "◇");

    /// <summary>
    /// Gets the <c>Circle4</c> spinner style.
    /// </summary>
    public static SpinnerStyle Circle4 { get; } = S("Circle4", 100, "○", "◔", "◑", "◕", "●", "◕", "◑", "◔");

    /// <summary>
    /// Gets the <c>Squares</c> spinner style.
    /// </summary>
    public static SpinnerStyle Squares { get; } = S("Squares", 80, "◰", "◳", "◲", "◱");

    /// <summary>
    /// Gets the <c>Triangles</c> spinner style.
    /// </summary>
    public static SpinnerStyle Triangles { get; } = S("Triangles", 80, "▴", "▸", "▾", "◂");

    /// <summary>
    /// Gets the <c>Triangles2</c> spinner style.
    /// </summary>
    public static SpinnerStyle Triangles2 { get; } = S("Triangles2", 80, "◢", "◣", "◥", "◤");

    /// <summary>
    /// Gets the <c>Arrows</c> spinner style.
    /// </summary>
    public static SpinnerStyle Arrows { get; } = S("Arrows", 80, "←", "↖", "↑", "↗", "→", "↘", "↓", "↙");

    /// <summary>
    /// Gets the <c>Arrows2</c> spinner style.
    /// </summary>
    public static SpinnerStyle Arrows2 { get; } = S("Arrows2", 80, "⇐", "⇑", "⇒", "⇓");

    /// <summary>
    /// Gets the <c>Arrows3</c> spinner style.
    /// </summary>
    public static SpinnerStyle Arrows3 { get; } = S("Arrows3", 80, "⬅", "⬆", "➡", "⬇");

    /// <summary>
    /// Gets the <c>Arrows4</c> spinner style.
    /// </summary>
    public static SpinnerStyle Arrows4 { get; } = S("Arrows4", 80, "◀", "▲", "▶", "▼");

    /// <summary>
    /// Gets the <c>Caret</c> spinner style.
    /// </summary>
    public static SpinnerStyle Caret { get; } = S("Caret", 80, "^", ">", "v", "<");

    /// <summary>
    /// Gets the <c>Bars</c> spinner style.
    /// </summary>
    public static SpinnerStyle Bars { get; } = S("Bars", 80, "▁", "▂", "▃", "▄", "▅", "▆", "▇", "█", "▇", "▆", "▅", "▄", "▃", "▂", "▁");

    /// <summary>
    /// Gets the <c>Bars2</c> spinner style.
    /// </summary>
    public static SpinnerStyle Bars2 { get; } = S("Bars2", 80, "▏", "▎", "▍", "▌", "▋", "▊", "▉", "█");

    /// <summary>
    /// Gets the <c>Bars3</c> spinner style.
    /// </summary>
    public static SpinnerStyle Bars3 { get; } = S("Bars3", 80, "▾", "◂", "▴", "▸");

    /// <summary>
    /// Gets the <c>Bars4</c> spinner style.
    /// </summary>
    public static SpinnerStyle Bars4 { get; } = S("Bars4", 80, "▏", "▎", "▍", "▌", "▋", "▊", "▉", "█", "▉", "▊", "▋", "▌", "▍", "▎", "▏");

    /// <summary>
    /// Gets the <c>Pulse</c> spinner style.
    /// </summary>
    public static SpinnerStyle Pulse { get; } = S("Pulse", 80, "░", "▒", "▓", "█", "▓", "▒");

    /// <summary>
    /// Gets the <c>Pulse2</c> spinner style.
    /// </summary>
    public static SpinnerStyle Pulse2 { get; } = S("Pulse2", 90, ".", ":", "*", ":", ".");

    /// <summary>
    /// Gets the <c>Pulse3</c> spinner style.
    /// </summary>
    public static SpinnerStyle Pulse3 { get; } = S("Pulse3", 90, ".", "·", "∙", "•", "●", "•", "∙", "·");

    /// <summary>
    /// Gets the <c>Moon</c> spinner style.
    /// </summary>
    public static SpinnerStyle Moon { get; } = S("Moon", 100, "◐", "◑", "◒", "◓");

    /// <summary>
    /// Gets the <c>Moon2</c> spinner style.
    /// </summary>
    public static SpinnerStyle Moon2 { get; } = S("Moon2", 100, "○", "◔", "◑", "◕", "●", "◕", "◑", "◔");

    /// <summary>
    /// Gets the <c>QuarterBlocks</c> spinner style.
    /// </summary>
    public static SpinnerStyle QuarterBlocks { get; } = S("QuarterBlocks", 90, "▖", "▘", "▝", "▗");

    /// <summary>
    /// Gets the <c>Blocks</c> spinner style.
    /// </summary>
    public static SpinnerStyle Blocks { get; } = S("Blocks", 90, "▘", "▝", "▗", "▖");

    /// <summary>
    /// Gets the <c>Blocks2</c> spinner style.
    /// </summary>
    public static SpinnerStyle Blocks2 { get; } = S("Blocks2", 90, "▛", "▜", "▙", "▟");

    /// <summary>
    /// Gets the <c>Blocks3</c> spinner style.
    /// </summary>
    public static SpinnerStyle Blocks3 { get; } = S("Blocks3", 90, "▀", "▐", "▄", "▌");

    /// <summary>
    /// Gets the <c>Blocks4</c> spinner style.
    /// </summary>
    public static SpinnerStyle Blocks4 { get; } = S("Blocks4", 80, "▚", "▞");

    /// <summary>
    /// Gets the <c>Arc</c> spinner style.
    /// </summary>
    public static SpinnerStyle Arc { get; } = S("Arc", 90, "◜", "◠", "◝", "◞", "◡", "◟");

    /// <summary>
    /// Gets the <c>Star</c> spinner style.
    /// </summary>
    public static SpinnerStyle Star { get; } = S("Star", 90, "✶", "✷", "✸", "✹", "✺", "✹", "✸", "✷");

    /// <summary>
    /// Gets the <c>Star2</c> spinner style.
    /// </summary>
    public static SpinnerStyle Star2 { get; } = S("Star2", 90, "+", "x", "*", "x");

    /// <summary>
    /// Gets the <c>Asterisk</c> spinner style.
    /// </summary>
    public static SpinnerStyle Asterisk { get; } = S("Asterisk", 90, "✶", "✸", "✹", "✺", "✹", "✸");

    /// <summary>
    /// Gets the <c>Pong</c> spinner style.
    /// </summary>
    public static SpinnerStyle Pong { get; } = S("Pong", 80, "┃", "━", "┃", "━");

    /// <summary>
    /// Gets the <c>Pipe</c> spinner style.
    /// </summary>
    public static SpinnerStyle Pipe { get; } = S("Pipe", 80, "┤", "┘", "┴", "└", "├", "┌", "┬", "┐");

    /// <summary>
    /// Gets the <c>Corners</c> spinner style.
    /// </summary>
    public static SpinnerStyle Corners { get; } = S("Corners", 80, "⌜", "⌝", "⌟", "⌞");

    /// <summary>
    /// Gets the <c>Snake</c> spinner style.
    /// </summary>
    public static SpinnerStyle Snake { get; } = S("Snake", 70, "○", "◔", "◑", "◕", "●", "◕", "◑", "◔");

    /// <summary>
    /// Gets the <c>Toggle</c> spinner style.
    /// </summary>
    public static SpinnerStyle Toggle { get; } = S("Toggle", 90, "□", "▣", "■", "▣");

    /// <summary>
    /// Gets the <c>Toggle2</c> spinner style.
    /// </summary>
    public static SpinnerStyle Toggle2 { get; } = S("Toggle2", 90, "□", "■");

    /// <summary>
    /// Gets the <c>Toggle3</c> spinner style.
    /// </summary>
    public static SpinnerStyle Toggle3 { get; } = S("Toggle3", 100, "▢", "▣");

    /// <summary>
    /// Gets the <c>Grow</c> spinner style.
    /// </summary>
    public static SpinnerStyle Grow { get; } = S("Grow", 90, ".", "o", "O", "o");

    /// <summary>
    /// Gets the <c>Grow2</c> spinner style.
    /// </summary>
    public static SpinnerStyle Grow2 { get; } = S("Grow2", 90, "·", "∙", "•", "●", "•", "∙");

    /// <summary>
    /// Gets the <c>Grow3</c> spinner style.
    /// </summary>
    public static SpinnerStyle Grow3 { get; } = S("Grow3", 100, "◦", "•", "●", "•");

    /// <summary>
    /// Gets the <c>Flip</c> spinner style.
    /// </summary>
    public static SpinnerStyle Flip { get; } = S("Flip", 80, "_", "‾");

    /// <summary>
    /// Gets the <c>Flip2</c> spinner style.
    /// </summary>
    public static SpinnerStyle Flip2 { get; } = S("Flip2", 80, "_", "‾", "_", "‾");

    /// <summary>
    /// Gets the <c>ChasingDots</c> spinner style.
    /// </summary>
    public static SpinnerStyle ChasingDots { get; } = S("ChasingDots", 80, "•", "∙", "·", "∙");

    /// <summary>
    /// Gets the <c>ChasingDots2</c> spinner style.
    /// </summary>
    public static SpinnerStyle ChasingDots2 { get; } = S("ChasingDots2", 80, "●", "○");

    /// <summary>
    /// Gets the <c>Noise</c> spinner style.
    /// </summary>
    public static SpinnerStyle Noise { get; } = S("Noise", 60, "░", "▒", "▓", "▒");

    /// <summary>
    /// Gets the <c>Noise2</c> spinner style.
    /// </summary>
    public static SpinnerStyle Noise2 { get; } = S("Noise2", 60, "░", "▒", "▓", "█", "▓", "▒");

    /// <summary>
    /// Gets the <c>Bounce</c> spinner style.
    /// </summary>
    public static SpinnerStyle Bounce { get; } = S("Bounce", 80, "○", "●");

    /// <summary>
    /// Gets the <c>Bounce2</c> spinner style.
    /// </summary>
    public static SpinnerStyle Bounce2 { get; } = S("Bounce2", 80, "◦", "●", "◦", "○");

    /// <summary>
    /// Gets the <c>Hearts</c> spinner style.
    /// </summary>
    public static SpinnerStyle Hearts { get; } = S("Hearts", 120, "♡", "♥");

    // Multi-frame (same cell width) spinners.
    /// <summary>
    /// Gets the <c>DotsBounce</c> spinner style.
    /// </summary>
    public static SpinnerStyle DotsBounce { get; } = S("DotsBounce", 80, "⠁  ", " ⠁ ", "  ⠁", " ⠁ ");

    /// <summary>
    /// Gets the <c>DotsBounce2</c> spinner style.
    /// </summary>
    public static SpinnerStyle DotsBounce2 { get; } = S("DotsBounce2", 80, "⠂  ", " ⠂ ", "  ⠂", " ⠂ ");

    /// <summary>
    /// Gets the <c>DotsEllipsis</c> spinner style.
    /// </summary>
    public static SpinnerStyle DotsEllipsis { get; } = S("DotsEllipsis", 120, ".  ", ".. ", "...");

    /// <summary>
    /// Gets the <c>DotsEllipsis2</c> spinner style.
    /// </summary>
    public static SpinnerStyle DotsEllipsis2 { get; } = S("DotsEllipsis2", 120, "   ", ".  ", ".. ", "...");

    /// <summary>
    /// Gets the <c>DotSlide</c> spinner style.
    /// </summary>
    public static SpinnerStyle DotSlide { get; } = S("DotSlide", 70, ".    ", " .   ", "  .  ", "   . ", "    .", "   . ", "  .  ", " .   ");

    /// <summary>
    /// Gets the <c>DotSlide2</c> spinner style.
    /// </summary>
    public static SpinnerStyle DotSlide2 { get; } = S("DotSlide2", 70, "●    ", " ●   ", "  ●  ", "   ● ", "    ●", "   ● ", "  ●  ", " ●   ");

    /// <summary>
    /// Gets the <c>DotCrawl</c> spinner style.
    /// </summary>
    public static SpinnerStyle DotCrawl { get; } = S("DotCrawl", 70, "o    ", "oo   ", "ooo  ", "oooo ", "ooooo", " oooo", "  ooo", "   oo", "    o");

    /// <summary>
    /// Gets the <c>ArrowSlide</c> spinner style.
    /// </summary>
    public static SpinnerStyle ArrowSlide { get; } = S("ArrowSlide", 70, "<    ", " <   ", "  <  ", "   < ", "    <", "   < ", "  <  ", " <   ");

    /// <summary>
    /// Gets the <c>ArrowSlide2</c> spinner style.
    /// </summary>
    public static SpinnerStyle ArrowSlide2 { get; } = S("ArrowSlide2", 70, ">    ", " >   ", "  >  ", "   > ", "    >", "   > ", "  >  ", " >   ");

    /// <summary>
    /// Gets the <c>Chevron</c> spinner style.
    /// </summary>
    public static SpinnerStyle Chevron { get; } = S("Chevron", 80, "<<<< ", " <<< ", "  << ", "   < ", "    <", "   < ", "  << ", " <<< ");

    /// <summary>
    /// Gets the <c>BoxSlide</c> spinner style.
    /// </summary>
    public static SpinnerStyle BoxSlide { get; } = S("BoxSlide", 80, "[■    ]", "[ ■   ]", "[  ■  ]", "[   ■ ]", "[    ■]", "[   ■ ]", "[  ■  ]", "[ ■   ]");

    /// <summary>
    /// Gets the <c>BoxSlide2</c> spinner style.
    /// </summary>
    public static SpinnerStyle BoxSlide2 { get; } = S("BoxSlide2", 80, "(■     )", "( ■    )", "(  ■   )", "(   ■  )", "(    ■ )", "(     ■)");

    /// <summary>
    /// Gets the <c>BouncingBar</c> spinner style.
    /// </summary>
    public static SpinnerStyle BouncingBar { get; } = S("BouncingBar", 90, "⟦█     ⟧", "⟦ █    ⟧", "⟦  █   ⟧", "⟦   █  ⟧", "⟦    █ ⟧", "⟦     █⟧", "⟦    █ ⟧", "⟦   █  ⟧", "⟦  █   ⟧", "⟦ █    ⟧");

    /// <summary>
    /// Gets the <c>BouncingBar2</c> spinner style.
    /// </summary>
    public static SpinnerStyle BouncingBar2 { get; } = S("BouncingBar2", 90, "⟦▄     ⟧", "⟦ ▄    ⟧", "⟦  ▄   ⟧", "⟦   ▄  ⟧", "⟦    ▄ ⟧", "⟦     ▄⟧", "⟦    ▄ ⟧", "⟦   ▄  ⟧", "⟦  ▄   ⟧", "⟦ ▄    ⟧");

    /// <summary>
    /// Gets the <c>BouncingBall</c> spinner style.
    /// </summary>
    public static SpinnerStyle BouncingBall { get; } = S("BouncingBall", 80, "(●     )", "( ●    )", "(  ●   )", "(   ●  )", "(    ● )", "(     ●)");

    /// <summary>
    /// Gets the <c>Orbit</c> spinner style.
    /// </summary>
    public static SpinnerStyle Orbit { get; } = S("Orbit", 90, "(●     )", "(○●    )", "( ○●   )", "(  ○●  )", "(   ○● )", "(    ○●)", "(     ●)", "(    ●○)", "(   ●○ )", "(  ●○  )", "( ●○   )", "(●○    )");

    /// <summary>
    /// Gets the <c>Runner</c> spinner style.
    /// </summary>
    public static SpinnerStyle Runner { get; } = S("Runner", 90, "🏃    ", " 🏃   ", "  🏃  ", "   🏃 ", "    🏃", "   🏃 ", "  🏃  ", " 🏃   ");

    /// <summary>
    /// Gets the <c>Rocket</c> spinner style.
    /// </summary>
    public static SpinnerStyle Rocket { get; } = S("Rocket", 90, "🚀    ", " 🚀   ", "  🚀  ", "   🚀 ", "    🚀", "   🚀 ", "  🚀  ", " 🚀   ");

    /// <summary>
    /// Gets the <c>Fish</c> spinner style.
    /// </summary>
    public static SpinnerStyle Fish { get; } = S("Fish", 90, "🐟    ", " 🐟   ", "  🐟  ", "   🐟 ", "    🐟", "   🐟 ", "  🐟  ", " 🐟   ");

    /// <summary>
    /// Gets the <c>Turtle</c> spinner style.
    /// </summary>
    public static SpinnerStyle Turtle { get; } = S("Turtle", 120, "🐢    ", " 🐢   ", "  🐢  ", "   🐢 ", "    🐢", "   🐢 ", "  🐢  ", " 🐢   ");

    /// <summary>
    /// Gets the <c>Earth</c> spinner style.
    /// </summary>
    public static SpinnerStyle Earth { get; } = S("Earth", 120, "🌍", "🌎", "🌏");

    /// <summary>
    /// Gets the <c>MoonEmoji</c> spinner style.
    /// </summary>
    public static SpinnerStyle MoonEmoji { get; } = S("MoonEmoji", 120, "🌑", "🌒", "🌓", "🌔", "🌕", "🌖", "🌗", "🌘");

    /// <summary>
    /// Gets the <c>Clock</c> spinner style.
    /// </summary>
    public static SpinnerStyle Clock { get; } = S("Clock", 120, "🕛", "🕐", "🕑", "🕒", "🕓", "🕔", "🕕", "🕖", "🕗", "🕘", "🕙", "🕚");

    /// <summary>
    /// Gets the <c>Weather</c> spinner style.
    /// </summary>
    public static SpinnerStyle Weather { get; } = S("Weather", 160, "SUN", "CLD", "RAN", "STO", "SNW", "WND");

    /// <summary>
    /// Gets the <c>Sparkle</c> spinner style.
    /// </summary>
    public static SpinnerStyle Sparkle { get; } = S("Sparkle", 120, "✨  ", " ✨ ", "  ✨", " ✨ ");

    /// <summary>
    /// Gets the <c>Wave</c> spinner style.
    /// </summary>
    public static SpinnerStyle Wave { get; } = S("Wave", 70,
        "\u2581\u2582\u2583\u2584\u2585\u2586\u2587\u2588",
        "\u2582\u2583\u2584\u2585\u2586\u2587\u2588\u2587",
        "\u2583\u2584\u2585\u2586\u2587\u2588\u2587\u2586",
        "\u2584\u2585\u2586\u2587\u2588\u2587\u2586\u2585",
        "\u2585\u2586\u2587\u2588\u2587\u2586\u2585\u2584",
        "\u2586\u2587\u2588\u2587\u2586\u2585\u2584\u2583",
        "\u2587\u2588\u2587\u2586\u2585\u2584\u2583\u2582",
        "\u2588\u2587\u2586\u2585\u2584\u2583\u2582\u2581");

    /// <summary>
    /// Gets the <c>BinaryDance</c> spinner style.
    /// </summary>
    public static SpinnerStyle BinaryDance { get; } = S("BinaryDance", 90,
        "010010",
        "001001",
        "100100",
        "010010",
        "001001",
        "100100");

    /// <summary>
    /// Gets all built-in spinner styles.
    /// </summary>
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
        DotsEllipsis,
        DotsEllipsis2,
        DotSlide,
        DotSlide2,
        DotCrawl,
        ArrowSlide,
        ArrowSlide2,
        Chevron,
        BoxSlide,
        BoxSlide2,
        BouncingBar,
        BouncingBar2,
        BouncingBall,
        Orbit,
        Runner,
        Rocket,
        Fish,
        Turtle,
        Earth,
        MoonEmoji,
        Clock,
        Weather,
        Sparkle,
        Wave,
        BinaryDance,
    };
}
