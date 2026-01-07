// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Text;

namespace XenoAtom.Terminal.UI.Styling;

public static class SpinnerStyles
{
    private static Rune R(int codePoint) => new(codePoint);

    public static SpinnerStyle Line { get; } = new()
    {
        Name = "Line",
        Interval = TimeSpan.FromMilliseconds(80),
        Frames = [R('|'), R('/'), R('-'), R('\\')],
    };

    public static SpinnerStyle Line2 { get; } = new()
    {
        Name = "Line2",
        Interval = TimeSpan.FromMilliseconds(80),
        Frames = [R(0x23BA), R(0x23BB), R(0x23BC), R(0x23BD)],
    };

    public static SpinnerStyle Line3 { get; } = new()
    {
        Name = "Line3",
        Interval = TimeSpan.FromMilliseconds(80),
        Frames = [R(0x2500), R(0x2571), R(0x2502), R(0x2572)],
    };

    public static SpinnerStyle Dots { get; } = new()
    {
        Name = "Dots",
        Interval = TimeSpan.FromMilliseconds(80),
        Frames = [R(0x280B), R(0x2819), R(0x2839), R(0x2838), R(0x283C), R(0x2834), R(0x2826), R(0x2827), R(0x2807), R(0x280F)],
    };

    public static SpinnerStyle Dots2 { get; } = new()
    {
        Name = "Dots2",
        Interval = TimeSpan.FromMilliseconds(80),
        Frames = [R(0x28FE), R(0x28FD), R(0x28FB), R(0x28BF), R(0x287F), R(0x28DF), R(0x28EF), R(0x28F7)],
    };

    public static SpinnerStyle Dots3 { get; } = new()
    {
        Name = "Dots3",
        Interval = TimeSpan.FromMilliseconds(80),
        Frames = [R(0x280B), R(0x2819), R(0x281A), R(0x281E), R(0x2816), R(0x2826), R(0x2834), R(0x2832), R(0x2833), R(0x2813)],
    };

    public static SpinnerStyle Dots4 { get; } = new()
    {
        Name = "Dots4",
        Interval = TimeSpan.FromMilliseconds(80),
        Frames = [R(0x2804), R(0x2806), R(0x2807), R(0x280B), R(0x2819), R(0x2838), R(0x2830), R(0x2820)],
    };

    public static SpinnerStyle Dots5 { get; } = new()
    {
        Name = "Dots5",
        Interval = TimeSpan.FromMilliseconds(80),
        Frames = [R(0x280B), R(0x2819), R(0x2838), R(0x2834), R(0x2826), R(0x2807)],
    };

    public static SpinnerStyle Dots6 { get; } = new()
    {
        Name = "Dots6",
        Interval = TimeSpan.FromMilliseconds(80),
        Frames = [R(0x280B), R(0x2819), R(0x281A), R(0x2813), R(0x2812), R(0x2802)],
    };

    public static SpinnerStyle Dots7 { get; } = new()
    {
        Name = "Dots7",
        Interval = TimeSpan.FromMilliseconds(80),
        Frames = [R(0x2808), R(0x2809), R(0x280B), R(0x2813), R(0x2812), R(0x2810)],
    };

    public static SpinnerStyle Dots8 { get; } = new()
    {
        Name = "Dots8",
        Interval = TimeSpan.FromMilliseconds(80),
        Frames = [R(0x2801), R(0x2809), R(0x2819), R(0x281A), R(0x2812), R(0x2802)],
    };

    public static SpinnerStyle Dots9 { get; } = new()
    {
        Name = "Dots9",
        Interval = TimeSpan.FromMilliseconds(80),
        Frames = [R(0x28B9), R(0x28BA), R(0x28BC), R(0x28F8), R(0x28C7), R(0x2867), R(0x2857), R(0x284F)],
    };

    public static SpinnerStyle Dots10 { get; } = new()
    {
        Name = "Dots10",
        Interval = TimeSpan.FromMilliseconds(80),
        Frames = [R(0x2884), R(0x2882), R(0x2881), R(0x2841), R(0x2848), R(0x2850), R(0x2860)],
    };

    public static SpinnerStyle Dots11 { get; } = new()
    {
        Name = "Dots11",
        Interval = TimeSpan.FromMilliseconds(80),
        Frames = [R(0x2801), R(0x2802), R(0x2804), R(0x2840), R(0x2880), R(0x2820), R(0x2810), R(0x2808)],
    };

    public static SpinnerStyle Dots12 { get; } = new()
    {
        Name = "Dots12",
        Interval = TimeSpan.FromMilliseconds(80),
        Frames = [R(0x2808), R(0x2810), R(0x2820), R(0x2880), R(0x2840), R(0x2804), R(0x2802), R(0x2801)],
    };

    public static SpinnerStyle Dots13 { get; } = new()
    {
        Name = "Dots13",
        Interval = TimeSpan.FromMilliseconds(80),
        Frames = [R(0x281D), R(0x2836), R(0x2827), R(0x2807), R(0x280F)],
    };

    public static SpinnerStyle Dots14 { get; } = new()
    {
        Name = "Dots14",
        Interval = TimeSpan.FromMilliseconds(80),
        Frames = [R(0x2800), R(0x2804), R(0x2806), R(0x2807), R(0x2803), R(0x2801)],
    };

    public static SpinnerStyle Dots15 { get; } = new()
    {
        Name = "Dots15",
        Interval = TimeSpan.FromMilliseconds(80),
        Frames = [R(0x2800), R(0x2801), R(0x2803), R(0x2807), R(0x280F), R(0x281F), R(0x283F), R(0x287F), R(0x28FF), R(0x287F), R(0x283F), R(0x281F), R(0x280F), R(0x2807), R(0x2803), R(0x2801)],
    };

    public static SpinnerStyle Dots16 { get; } = new()
    {
        Name = "Dots16",
        Interval = TimeSpan.FromMilliseconds(80),
        Frames = [R(0x28F7), R(0x28EF), R(0x28DF), R(0x287F), R(0x283F), R(0x281F), R(0x280F), R(0x2807)],
    };

    public static SpinnerStyle Dots17 { get; } = new()
    {
        Name = "Dots17",
        Interval = TimeSpan.FromMilliseconds(80),
        Frames = [R(0x2842), R(0x2844), R(0x28C4), R(0x2882), R(0x2801), R(0x2809), R(0x2819), R(0x2839), R(0x2879), R(0x28F9)],
    };

    public static SpinnerStyle Dots18 { get; } = new()
    {
        Name = "Dots18",
        Interval = TimeSpan.FromMilliseconds(80),
        Frames = [R(0x28C1), R(0x28C3), R(0x28C7), R(0x28CF), R(0x28DF), R(0x28FF), R(0x28FE), R(0x28FC), R(0x28F8), R(0x28F0), R(0x28E0)],
    };

    public static SpinnerStyle Circle { get; } = new()
    {
        Name = "Circle",
        Interval = TimeSpan.FromMilliseconds(100),
        Frames = [R(0x25D0), R(0x25D3), R(0x25D1), R(0x25D2)],
    };

    public static SpinnerStyle Circle2 { get; } = new()
    {
        Name = "Circle2",
        Interval = TimeSpan.FromMilliseconds(100),
        Frames = [R(0x25F4), R(0x25F7), R(0x25F6), R(0x25F5)],
    };

    public static SpinnerStyle Circle3 { get; } = new()
    {
        Name = "Circle3",
        Interval = TimeSpan.FromMilliseconds(100),
        Frames = [R(0x25C6), R(0x25C7)],
    };

    public static SpinnerStyle Circle4 { get; } = new()
    {
        Name = "Circle4",
        Interval = TimeSpan.FromMilliseconds(100),
        Frames = [R(0x25CB), R(0x25D4), R(0x25D1), R(0x25D5), R(0x25CF)],
    };

    public static SpinnerStyle SquareCorners { get; } = new()
    {
        Name = "SquareCorners",
        Interval = TimeSpan.FromMilliseconds(100),
        Frames = [R(0x25F0), R(0x25F3), R(0x25F2), R(0x25F1)],
    };

    public static SpinnerStyle Triangles { get; } = new()
    {
        Name = "Triangles",
        Interval = TimeSpan.FromMilliseconds(80),
        Frames = [R(0x25B4), R(0x25B8), R(0x25BE), R(0x25C2)],
    };

    public static SpinnerStyle Triangles2 { get; } = new()
    {
        Name = "Triangles2",
        Interval = TimeSpan.FromMilliseconds(80),
        Frames = [R(0x25E2), R(0x25E3), R(0x25E5), R(0x25E4)],
    };

    public static SpinnerStyle Arrows { get; } = new()
    {
        Name = "Arrows",
        Interval = TimeSpan.FromMilliseconds(80),
        Frames = [R(0x2190), R(0x2196), R(0x2191), R(0x2197), R(0x2192), R(0x2198), R(0x2193), R(0x2199)],
    };

    public static SpinnerStyle Arrows2 { get; } = new()
    {
        Name = "Arrows2",
        Interval = TimeSpan.FromMilliseconds(80),
        Frames = [R(0x21E6), R(0x21E7), R(0x21E8), R(0x21E9)],
    };

    public static SpinnerStyle Arrows3 { get; } = new()
    {
        Name = "Arrows3",
        Interval = TimeSpan.FromMilliseconds(80),
        Frames = [R(0x2B05), R(0x2B06), R(0x27A1), R(0x2B07)],
    };

    public static SpinnerStyle Arrows4 { get; } = new()
    {
        Name = "Arrows4",
        Interval = TimeSpan.FromMilliseconds(80),
        Frames = [R(0x25C0), R(0x25B2), R(0x25B6), R(0x25BC)],
    };

    public static SpinnerStyle Caret { get; } = new()
    {
        Name = "Caret",
        Interval = TimeSpan.FromMilliseconds(80),
        Frames = [R('^'), R('>'), R('v'), R('<')],
    };

    public static SpinnerStyle Bars { get; } = new()
    {
        Name = "Bars",
        Interval = TimeSpan.FromMilliseconds(80),
        Frames = [R(0x2581), R(0x2582), R(0x2583), R(0x2584), R(0x2585), R(0x2586), R(0x2587), R(0x2588), R(0x2587), R(0x2586), R(0x2585), R(0x2584), R(0x2583), R(0x2582), R(0x2581)],
    };

    public static SpinnerStyle Bars2 { get; } = new()
    {
        Name = "Bars2",
        Interval = TimeSpan.FromMilliseconds(80),
        Frames = [R(0x258F), R(0x258E), R(0x258D), R(0x258C), R(0x258B), R(0x258A), R(0x2589), R(0x2588)],
    };

    public static SpinnerStyle Bars3 { get; } = new()
    {
        Name = "Bars3",
        Interval = TimeSpan.FromMilliseconds(80),
        Frames = [R(0x25BE), R(0x25C2), R(0x25B4), R(0x25B8)],
    };

    public static SpinnerStyle Bars4 { get; } = new()
    {
        Name = "Bars4",
        Interval = TimeSpan.FromMilliseconds(80),
        Frames = [R(0x258F), R(0x258E), R(0x258D), R(0x258C), R(0x258B), R(0x258A), R(0x2589), R(0x2588), R(0x2589), R(0x258A), R(0x258B), R(0x258C), R(0x258D), R(0x258E), R(0x258F)],
    };

    public static SpinnerStyle Bars5 { get; } = new()
    {
        Name = "Bars5",
        Interval = TimeSpan.FromMilliseconds(80),
        Frames = [R(0x2581), R(0x2582), R(0x2583), R(0x2584), R(0x2585), R(0x2586), R(0x2587), R(0x2588)],
    };

    public static SpinnerStyle Pulse { get; } = new()
    {
        Name = "Pulse",
        Interval = TimeSpan.FromMilliseconds(80),
        Frames = [R(0x2591), R(0x2592), R(0x2593), R(0x2588), R(0x2593), R(0x2592)],
    };

    public static SpinnerStyle Pulse2 { get; } = new()
    {
        Name = "Pulse2",
        Interval = TimeSpan.FromMilliseconds(90),
        Frames = [R('.'), R(':'), R('*'), R(':'), R('.')],
    };

    public static SpinnerStyle Pulse3 { get; } = new()
    {
        Name = "Pulse3",
        Interval = TimeSpan.FromMilliseconds(90),
        Frames = [R('.'), R(0x00B7), R(0x2219), R(0x2022), R(0x25CF), R(0x2022), R(0x2219), R(0x00B7)],
    };

    public static SpinnerStyle Moon { get; } = new()
    {
        Name = "Moon",
        Interval = TimeSpan.FromMilliseconds(100),
        Frames = [R(0x25D0), R(0x25D1), R(0x25D2), R(0x25D3)],
    };

    public static SpinnerStyle Moon2 { get; } = new()
    {
        Name = "Moon2",
        Interval = TimeSpan.FromMilliseconds(100),
        Frames = [R(0x25CB), R(0x25D4), R(0x25D1), R(0x25D5), R(0x25CF), R(0x25D5), R(0x25D1), R(0x25D4)],
    };

    public static SpinnerStyle QuarterBlocks { get; } = new()
    {
        Name = "QuarterBlocks",
        Interval = TimeSpan.FromMilliseconds(90),
        Frames = [R(0x2596), R(0x2598), R(0x259D), R(0x2597)],
    };

    public static SpinnerStyle Blocks { get; } = new()
    {
        Name = "Blocks",
        Interval = TimeSpan.FromMilliseconds(90),
        Frames = [R(0x2598), R(0x259D), R(0x2597), R(0x2596)],
    };

    public static SpinnerStyle Blocks2 { get; } = new()
    {
        Name = "Blocks2",
        Interval = TimeSpan.FromMilliseconds(90),
        Frames = [R(0x259B), R(0x259C), R(0x2599), R(0x259F)],
    };

    public static SpinnerStyle Blocks3 { get; } = new()
    {
        Name = "Blocks3",
        Interval = TimeSpan.FromMilliseconds(90),
        Frames = [R(0x2580), R(0x2590), R(0x2584), R(0x258C)],
    };

    public static SpinnerStyle Blocks4 { get; } = new()
    {
        Name = "Blocks4",
        Interval = TimeSpan.FromMilliseconds(80),
        Frames = [R(0x259A), R(0x259E)],
    };

    public static SpinnerStyle Blocks5 { get; } = new()
    {
        Name = "Blocks5",
        Interval = TimeSpan.FromMilliseconds(80),
        Frames = [R(0x258C), R(0x2580), R(0x2590), R(0x2584)],
    };

    public static SpinnerStyle Arc { get; } = new()
    {
        Name = "Arc",
        Interval = TimeSpan.FromMilliseconds(90),
        Frames = [R(0x25DC), R(0x25E0), R(0x25DD), R(0x25DE), R(0x25E1), R(0x25DF)],
    };

    public static SpinnerStyle Arc2 { get; } = new()
    {
        Name = "Arc2",
        Interval = TimeSpan.FromMilliseconds(90),
        Frames = [R(0x25DC), R(0x25DD), R(0x25DE), R(0x25DF)],
    };

    public static SpinnerStyle Star { get; } = new()
    {
        Name = "Star",
        Interval = TimeSpan.FromMilliseconds(90),
        Frames = [R(0x2736), R(0x2737), R(0x2738), R(0x2739), R(0x273A), R(0x2739), R(0x2738), R(0x2737)],
    };

    public static SpinnerStyle Star2 { get; } = new()
    {
        Name = "Star2",
        Interval = TimeSpan.FromMilliseconds(90),
        Frames = [R('+'), R('x'), R('*'), R('x')],
    };

    public static SpinnerStyle Asterisk { get; } = new()
    {
        Name = "Asterisk",
        Interval = TimeSpan.FromMilliseconds(90),
        Frames = [R(0x2736), R(0x2738), R(0x2739), R(0x273A), R(0x2739), R(0x2738)],
    };

    public static SpinnerStyle Pong { get; } = new()
    {
        Name = "Pong",
        Interval = TimeSpan.FromMilliseconds(80),
        Frames = [R(0x2503), R(0x2501), R(0x2503), R(0x2501)],
    };

    public static SpinnerStyle Pipe { get; } = new()
    {
        Name = "Pipe",
        Interval = TimeSpan.FromMilliseconds(80),
        Frames = [R(0x2524), R(0x2518), R(0x2534), R(0x2514), R(0x251C), R(0x250C), R(0x252C), R(0x2510)],
    };

    public static SpinnerStyle Corners { get; } = new()
    {
        Name = "Corners",
        Interval = TimeSpan.FromMilliseconds(80),
        Frames = [R(0x231C), R(0x231D), R(0x231F), R(0x231E)],
    };

    public static SpinnerStyle Snake { get; } = new()
    {
        Name = "Snake",
        Interval = TimeSpan.FromMilliseconds(70),
        Frames = [R(0x25CB), R(0x25D4), R(0x25D1), R(0x25D5), R(0x25CF), R(0x25D5), R(0x25D1), R(0x25D4)],
    };

    public static SpinnerStyle Toggle { get; } = new()
    {
        Name = "Toggle",
        Interval = TimeSpan.FromMilliseconds(90),
        Frames = [R(0x25A1), R(0x25A3), R(0x25A9), R(0x25A6)],
    };

    public static SpinnerStyle Toggle2 { get; } = new()
    {
        Name = "Toggle2",
        Interval = TimeSpan.FromMilliseconds(90),
        Frames = [R(0x25A1), R(0x25A0)],
    };

    public static SpinnerStyle Toggle3 { get; } = new()
    {
        Name = "Toggle3",
        Interval = TimeSpan.FromMilliseconds(100),
        Frames = [R(0x25A2), R(0x25A3)],
    };

    public static SpinnerStyle Grow { get; } = new()
    {
        Name = "Grow",
        Interval = TimeSpan.FromMilliseconds(90),
        Frames = [R('.'), R('o'), R('O'), R('o')],
    };

    public static SpinnerStyle Grow2 { get; } = new()
    {
        Name = "Grow2",
        Interval = TimeSpan.FromMilliseconds(90),
        Frames = [R(0x00B7), R(0x2219), R(0x2022), R(0x25CF), R(0x2022), R(0x2219)],
    };

    public static SpinnerStyle Grow3 { get; } = new()
    {
        Name = "Grow3",
        Interval = TimeSpan.FromMilliseconds(100),
        Frames = [R(0x25E6), R(0x2022), R(0x25CF), R(0x2022)],
    };

    public static SpinnerStyle Flip { get; } = new()
    {
        Name = "Flip",
        Interval = TimeSpan.FromMilliseconds(80),
        Frames = [R(0x005F), R(0x203E)],
    };

    public static SpinnerStyle Flip2 { get; } = new()
    {
        Name = "Flip2",
        Interval = TimeSpan.FromMilliseconds(80),
        Frames = [R(0x005F), R(0x203E), R(0x005F), R(0x203E)],
    };

    public static SpinnerStyle ChasingDots { get; } = new()
    {
        Name = "ChasingDots",
        Interval = TimeSpan.FromMilliseconds(80),
        Frames = [R(0x2022), R(0x2219), R(0x00B7), R(0x2219)],
    };

    public static SpinnerStyle ChasingDots2 { get; } = new()
    {
        Name = "ChasingDots2",
        Interval = TimeSpan.FromMilliseconds(80),
        Frames = [R(0x25CF), R(0x25CB)],
    };

    public static SpinnerStyle Noise { get; } = new()
    {
        Name = "Noise",
        Interval = TimeSpan.FromMilliseconds(60),
        Frames = [R(0x2591), R(0x2592), R(0x2593), R(0x2592)],
    };

    public static SpinnerStyle Noise2 { get; } = new()
    {
        Name = "Noise2",
        Interval = TimeSpan.FromMilliseconds(60),
        Frames = [R(0x2591), R(0x2592), R(0x2593), R(0x2588), R(0x2593), R(0x2592)],
    };

    public static SpinnerStyle Bounce { get; } = new()
    {
        Name = "Bounce",
        Interval = TimeSpan.FromMilliseconds(80),
        Frames = [R(0x25CB), R(0x25CF)],
    };

    public static SpinnerStyle Bounce2 { get; } = new()
    {
        Name = "Bounce2",
        Interval = TimeSpan.FromMilliseconds(80),
        Frames = [R(0x25E6), R(0x25CF), R(0x25E6), R(0x25CB)],
    };

    public static SpinnerStyle Bounce3 { get; } = new()
    {
        Name = "Bounce3",
        Interval = TimeSpan.FromMilliseconds(90),
        Frames = [R('.'), R('o'), R('O'), R('o')],
    };

    public static SpinnerStyle Hearts { get; } = new()
    {
        Name = "Hearts",
        Interval = TimeSpan.FromMilliseconds(120),
        Frames = [R(0x2661), R(0x2665)],
    };

    public static SpinnerStyle Balloon { get; } = new()
    {
        Name = "Balloon",
        Interval = TimeSpan.FromMilliseconds(90),
        Frames = [R('.'), R('o'), R('O'), R('@'), R('*')],
    };

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
        Dots13,
        Dots14,
        Dots15,
        Dots16,
        Dots17,
        Dots18,
        Circle,
        Circle2,
        Circle3,
        Circle4,
        SquareCorners,
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
        Bars5,
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
        Blocks5,
        Arc,
        Arc2,
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
        Bounce3,
        Hearts,
        Balloon,
    };
}

