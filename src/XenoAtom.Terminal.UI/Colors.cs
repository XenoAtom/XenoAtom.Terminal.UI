// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI;

/// <summary>
/// Provides a set of predefined color constants for use in terminal and graphical applications, including ANSI basic
/// colors and standard web (CSS/SVG/X11) named colors.
/// </summary>
/// <remarks>The Colors class offers convenient access to commonly used colors by name, including the 16 ANSI
/// basic colors and a comprehensive set of web-standard named colors. These constants can be used to specify foreground
/// or background colors in terminal output, UI elements, or any context that accepts a Color value. The color values
/// correspond to widely recognized standards, ensuring compatibility across platforms and systems.</remarks>
public static class Colors
{
    /// <summary>
    /// The terminal default color (SGR 39/49).
    /// </summary>
    public static Color TerminalDefault => Color.Default;

    /// <summary>Basic color index 0 (black).</summary>
    public static Color TerminalBlack => Color.Basic16(0);

    /// <summary>Basic color index 1 (red).</summary>
    public static Color TerminalRed => Color.Basic16(1);

    /// <summary>Basic color index 2 (green).</summary>
    public static Color TerminalGreen => Color.Basic16(2);

    /// <summary>Basic color index 3 (yellow).</summary>
    public static Color TerminalYellow => Color.Basic16(3);

    /// <summary>Basic color index 4 (blue).</summary>
    public static Color TerminalBlue => Color.Basic16(4);

    /// <summary>Basic color index 5 (magenta).</summary>
    public static Color TerminalMagenta => Color.Basic16(5);

    /// <summary>Basic color index 6 (cyan).</summary>
    public static Color TerminalCyan => Color.Basic16(6);

    /// <summary>Basic color index 7 (white).</summary>
    public static Color TerminalWhite => Color.Basic16(7);

    /// <summary>Basic color index 8 (bright black / "gray").</summary>
    public static Color TerminalBrightBlack => Color.Basic16(8);

    /// <summary>Basic color index 9 (bright red).</summary>
    public static Color TerminalBrightRed => Color.Basic16(9);

    /// <summary>Basic color index 10 (bright green).</summary>
    public static Color TerminalBrightGreen => Color.Basic16(10);

    /// <summary>Basic color index 11 (bright yellow).</summary>
    public static Color TerminalBrightYellow => Color.Basic16(11);

    /// <summary>Basic color index 12 (bright blue).</summary>
    public static Color TerminalBrightBlue => Color.Basic16(12);

    /// <summary>Basic color index 13 (bright magenta).</summary>
    public static Color TerminalBrightMagenta => Color.Basic16(13);

    /// <summary>Basic color index 14 (bright cyan).</summary>
    public static Color TerminalBrightCyan => Color.Basic16(14);

    /// <summary>Basic color index 15 (bright white).</summary>
    public static Color TerminalBrightWhite => Color.Basic16(15);

    /// <summary>Web (CSS/SVG/X11) named color <c>AliceBlue</c> (#F0F8FF).</summary>
    public static Color AliceBlue => Color.Rgb(240, 248, 255);

    /// <summary>Web (CSS/SVG/X11) named color <c>AntiqueWhite</c> (#FAEBD7).</summary>
    public static Color AntiqueWhite => Color.Rgb(250, 235, 215);

    /// <summary>Web (CSS/SVG/X11) named color <c>Aqua</c> (#00FFFF).</summary>
    public static Color Aqua => Color.Rgb(0, 255, 255);

    /// <summary>Web (CSS/SVG/X11) named color <c>Aquamarine</c> (#7FFFD4).</summary>
    public static Color Aquamarine => Color.Rgb(127, 255, 212);

    /// <summary>Web (CSS/SVG/X11) named color <c>Azure</c> (#F0FFFF).</summary>
    public static Color Azure => Color.Rgb(240, 255, 255);

    /// <summary>Web (CSS/SVG/X11) named color <c>Beige</c> (#F5F5DC).</summary>
    public static Color Beige => Color.Rgb(245, 245, 220);

    /// <summary>Web (CSS/SVG/X11) named color <c>Bisque</c> (#FFE4C4).</summary>
    public static Color Bisque => Color.Rgb(255, 228, 196);

    /// <summary>Web (CSS/SVG/X11) named color <c>Black</c> (#000000).</summary>
    public static Color Black => Color.Rgb(0, 0, 0);

    /// <summary>Web (CSS/SVG/X11) named color <c>BlanchedAlmond</c> (#FFEBCD).</summary>
    public static Color BlanchedAlmond => Color.Rgb(255, 235, 205);

    /// <summary>Web (CSS/SVG/X11) named color <c>Blue</c> (#0000FF).</summary>
    public static Color Blue => Color.Rgb(0, 0, 255);

    /// <summary>Web (CSS/SVG/X11) named color <c>BlueViolet</c> (#8A2BE2).</summary>
    public static Color BlueViolet => Color.Rgb(138, 43, 226);

    /// <summary>Web (CSS/SVG/X11) named color <c>Brown</c> (#A52A2A).</summary>
    public static Color Brown => Color.Rgb(165, 42, 42);

    /// <summary>Web (CSS/SVG/X11) named color <c>BurlyWood</c> (#DEB887).</summary>
    public static Color BurlyWood => Color.Rgb(222, 184, 135);

    /// <summary>Web (CSS/SVG/X11) named color <c>CadetBlue</c> (#5F9EA0).</summary>
    public static Color CadetBlue => Color.Rgb(95, 158, 160);

    /// <summary>Web (CSS/SVG/X11) named color <c>Chartreuse</c> (#7FFF00).</summary>
    public static Color Chartreuse => Color.Rgb(127, 255, 0);

    /// <summary>Web (CSS/SVG/X11) named color <c>Chocolate</c> (#D2691E).</summary>
    public static Color Chocolate => Color.Rgb(210, 105, 30);

    /// <summary>Web (CSS/SVG/X11) named color <c>Coral</c> (#FF7F50).</summary>
    public static Color Coral => Color.Rgb(255, 127, 80);

    /// <summary>Web (CSS/SVG/X11) named color <c>CornflowerBlue</c> (#6495ED).</summary>
    public static Color CornflowerBlue => Color.Rgb(100, 149, 237);

    /// <summary>Web (CSS/SVG/X11) named color <c>Cornsilk</c> (#FFF8DC).</summary>
    public static Color Cornsilk => Color.Rgb(255, 248, 220);

    /// <summary>Web (CSS/SVG/X11) named color <c>Crimson</c> (#DC143C).</summary>
    public static Color Crimson => Color.Rgb(220, 20, 60);

    /// <summary>Web (CSS/SVG/X11) named color <c>Cyan</c> (#00FFFF).</summary>
    public static Color Cyan => Color.Rgb(0, 255, 255);

    /// <summary>Web (CSS/SVG/X11) named color <c>DarkBlue</c> (#00008B).</summary>
    public static Color DarkBlue => Color.Rgb(0, 0, 139);

    /// <summary>Web (CSS/SVG/X11) named color <c>DarkCyan</c> (#008B8B).</summary>
    public static Color DarkCyan => Color.Rgb(0, 139, 139);

    /// <summary>Web (CSS/SVG/X11) named color <c>DarkGoldenrod</c> (#B8860B).</summary>
    public static Color DarkGoldenrod => Color.Rgb(184, 134, 11);

    /// <summary>Web (CSS/SVG/X11) named color <c>DarkGray</c> (#A9A9A9).</summary>
    public static Color DarkGray => Color.Rgb(169, 169, 169);

    /// <summary>Web (CSS/SVG/X11) named color <c>DarkGreen</c> (#006400).</summary>
    public static Color DarkGreen => Color.Rgb(0, 100, 0);

    /// <summary>Web (CSS/SVG/X11) named color <c>DarkKhaki</c> (#BDB76B).</summary>
    public static Color DarkKhaki => Color.Rgb(189, 183, 107);

    /// <summary>Web (CSS/SVG/X11) named color <c>DarkMagenta</c> (#8B008B).</summary>
    public static Color DarkMagenta => Color.Rgb(139, 0, 139);

    /// <summary>Web (CSS/SVG/X11) named color <c>DarkOliveGreen</c> (#556B2F).</summary>
    public static Color DarkOliveGreen => Color.Rgb(85, 107, 47);

    /// <summary>Web (CSS/SVG/X11) named color <c>DarkOrange</c> (#FF8C00).</summary>
    public static Color DarkOrange => Color.Rgb(255, 140, 0);

    /// <summary>Web (CSS/SVG/X11) named color <c>DarkOrchid</c> (#9932CC).</summary>
    public static Color DarkOrchid => Color.Rgb(153, 50, 204);

    /// <summary>Web (CSS/SVG/X11) named color <c>DarkRed</c> (#8B0000).</summary>
    public static Color DarkRed => Color.Rgb(139, 0, 0);

    /// <summary>Web (CSS/SVG/X11) named color <c>DarkSalmon</c> (#E9967A).</summary>
    public static Color DarkSalmon => Color.Rgb(233, 150, 122);

    /// <summary>Web (CSS/SVG/X11) named color <c>DarkSeaGreen</c> (#8FBC8F).</summary>
    public static Color DarkSeaGreen => Color.Rgb(143, 188, 143);

    /// <summary>Web (CSS/SVG/X11) named color <c>DarkSlateBlue</c> (#483D8B).</summary>
    public static Color DarkSlateBlue => Color.Rgb(72, 61, 139);

    /// <summary>Web (CSS/SVG/X11) named color <c>DarkSlateGray</c> (#2F4F4F).</summary>
    public static Color DarkSlateGray => Color.Rgb(47, 79, 79);

    /// <summary>Web (CSS/SVG/X11) named color <c>DarkTurquoise</c> (#00CED1).</summary>
    public static Color DarkTurquoise => Color.Rgb(0, 206, 209);

    /// <summary>Web (CSS/SVG/X11) named color <c>DarkViolet</c> (#9400D3).</summary>
    public static Color DarkViolet => Color.Rgb(148, 0, 211);

    /// <summary>Web (CSS/SVG/X11) named color <c>DeepPink</c> (#FF1493).</summary>
    public static Color DeepPink => Color.Rgb(255, 20, 147);

    /// <summary>Web (CSS/SVG/X11) named color <c>DeepSkyBlue</c> (#00BFFF).</summary>
    public static Color DeepSkyBlue => Color.Rgb(0, 191, 255);

    /// <summary>Web (CSS/SVG/X11) named color <c>DimGray</c> (#696969).</summary>
    public static Color DimGray => Color.Rgb(105, 105, 105);

    /// <summary>Web (CSS/SVG/X11) named color <c>DodgerBlue</c> (#1E90FF).</summary>
    public static Color DodgerBlue => Color.Rgb(30, 144, 255);

    /// <summary>Web (CSS/SVG/X11) named color <c>Firebrick</c> (#B22222).</summary>
    public static Color Firebrick => Color.Rgb(178, 34, 34);

    /// <summary>Web (CSS/SVG/X11) named color <c>FloralWhite</c> (#FFFAF0).</summary>
    public static Color FloralWhite => Color.Rgb(255, 250, 240);

    /// <summary>Web (CSS/SVG/X11) named color <c>ForestGreen</c> (#228B22).</summary>
    public static Color ForestGreen => Color.Rgb(34, 139, 34);

    /// <summary>Web (CSS/SVG/X11) named color <c>Fuchsia</c> (#FF00FF).</summary>
    public static Color Fuchsia => Color.Rgb(255, 0, 255);

    /// <summary>Web (CSS/SVG/X11) named color <c>Gainsboro</c> (#DCDCDC).</summary>
    public static Color Gainsboro => Color.Rgb(220, 220, 220);

    /// <summary>Web (CSS/SVG/X11) named color <c>GhostWhite</c> (#F8F8FF).</summary>
    public static Color GhostWhite => Color.Rgb(248, 248, 255);

    /// <summary>Web (CSS/SVG/X11) named color <c>Gold</c> (#FFD700).</summary>
    public static Color Gold => Color.Rgb(255, 215, 0);

    /// <summary>Web (CSS/SVG/X11) named color <c>Goldenrod</c> (#DAA520).</summary>
    public static Color Goldenrod => Color.Rgb(218, 165, 32);

    /// <summary>Web (CSS/SVG/X11) named color <c>Gray</c> (#808080).</summary>
    public static Color Gray => Color.Rgb(128, 128, 128);

    /// <summary>Web (CSS/SVG/X11) named color <c>Green</c> (#008000).</summary>
    public static Color Green => Color.Rgb(0, 128, 0);

    /// <summary>Web (CSS/SVG/X11) named color <c>GreenYellow</c> (#ADFF2F).</summary>
    public static Color GreenYellow => Color.Rgb(173, 255, 47);

    /// <summary>Web (CSS/SVG/X11) named color <c>Honeydew</c> (#F0FFF0).</summary>
    public static Color Honeydew => Color.Rgb(240, 255, 240);

    /// <summary>Web (CSS/SVG/X11) named color <c>HotPink</c> (#FF69B4).</summary>
    public static Color HotPink => Color.Rgb(255, 105, 180);

    /// <summary>Web (CSS/SVG/X11) named color <c>IndianRed</c> (#CD5C5C).</summary>
    public static Color IndianRed => Color.Rgb(205, 92, 92);

    /// <summary>Web (CSS/SVG/X11) named color <c>Indigo</c> (#4B0082).</summary>
    public static Color Indigo => Color.Rgb(75, 0, 130);

    /// <summary>Web (CSS/SVG/X11) named color <c>Ivory</c> (#FFFFF0).</summary>
    public static Color Ivory => Color.Rgb(255, 255, 240);

    /// <summary>Web (CSS/SVG/X11) named color <c>Khaki</c> (#F0E68C).</summary>
    public static Color Khaki => Color.Rgb(240, 230, 140);

    /// <summary>Web (CSS/SVG/X11) named color <c>Lavender</c> (#E6E6FA).</summary>
    public static Color Lavender => Color.Rgb(230, 230, 250);

    /// <summary>Web (CSS/SVG/X11) named color <c>LavenderBlush</c> (#FFF0F5).</summary>
    public static Color LavenderBlush => Color.Rgb(255, 240, 245);

    /// <summary>Web (CSS/SVG/X11) named color <c>LawnGreen</c> (#7CFC00).</summary>
    public static Color LawnGreen => Color.Rgb(124, 252, 0);

    /// <summary>Web (CSS/SVG/X11) named color <c>LemonChiffon</c> (#FFFACD).</summary>
    public static Color LemonChiffon => Color.Rgb(255, 250, 205);

    /// <summary>Web (CSS/SVG/X11) named color <c>LightBlue</c> (#ADD8E6).</summary>
    public static Color LightBlue => Color.Rgb(173, 216, 230);

    /// <summary>Web (CSS/SVG/X11) named color <c>LightCoral</c> (#F08080).</summary>
    public static Color LightCoral => Color.Rgb(240, 128, 128);

    /// <summary>Web (CSS/SVG/X11) named color <c>LightCyan</c> (#E0FFFF).</summary>
    public static Color LightCyan => Color.Rgb(224, 255, 255);

    /// <summary>Web (CSS/SVG/X11) named color <c>LightGoldenrodYellow</c> (#FAFAD2).</summary>
    public static Color LightGoldenrodYellow => Color.Rgb(250, 250, 210);

    /// <summary>Web (CSS/SVG/X11) named color <c>LightGray</c> (#D3D3D3).</summary>
    public static Color LightGray => Color.Rgb(211, 211, 211);

    /// <summary>Web (CSS/SVG/X11) named color <c>LightGreen</c> (#90EE90).</summary>
    public static Color LightGreen => Color.Rgb(144, 238, 144);

    /// <summary>Web (CSS/SVG/X11) named color <c>LightPink</c> (#FFB6C1).</summary>
    public static Color LightPink => Color.Rgb(255, 182, 193);

    /// <summary>Web (CSS/SVG/X11) named color <c>LightSalmon</c> (#FFA07A).</summary>
    public static Color LightSalmon => Color.Rgb(255, 160, 122);

    /// <summary>Web (CSS/SVG/X11) named color <c>LightSeaGreen</c> (#20B2AA).</summary>
    public static Color LightSeaGreen => Color.Rgb(32, 178, 170);

    /// <summary>Web (CSS/SVG/X11) named color <c>LightSkyBlue</c> (#87CEFA).</summary>
    public static Color LightSkyBlue => Color.Rgb(135, 206, 250);

    /// <summary>Web (CSS/SVG/X11) named color <c>LightSlateGray</c> (#778899).</summary>
    public static Color LightSlateGray => Color.Rgb(119, 136, 153);

    /// <summary>Web (CSS/SVG/X11) named color <c>LightSteelBlue</c> (#B0C4DE).</summary>
    public static Color LightSteelBlue => Color.Rgb(176, 196, 222);

    /// <summary>Web (CSS/SVG/X11) named color <c>LightYellow</c> (#FFFFE0).</summary>
    public static Color LightYellow => Color.Rgb(255, 255, 224);

    /// <summary>Web (CSS/SVG/X11) named color <c>Lime</c> (#00FF00).</summary>
    public static Color Lime => Color.Rgb(0, 255, 0);

    /// <summary>Web (CSS/SVG/X11) named color <c>LimeGreen</c> (#32CD32).</summary>
    public static Color LimeGreen => Color.Rgb(50, 205, 50);

    /// <summary>Web (CSS/SVG/X11) named color <c>Linen</c> (#FAF0E6).</summary>
    public static Color Linen => Color.Rgb(250, 240, 230);

    /// <summary>Web (CSS/SVG/X11) named color <c>Magenta</c> (#FF00FF).</summary>
    public static Color Magenta => Color.Rgb(255, 0, 255);

    /// <summary>Web (CSS/SVG/X11) named color <c>Maroon</c> (#800000).</summary>
    public static Color Maroon => Color.Rgb(128, 0, 0);

    /// <summary>Web (CSS/SVG/X11) named color <c>MediumAquamarine</c> (#66CDAA).</summary>
    public static Color MediumAquamarine => Color.Rgb(102, 205, 170);

    /// <summary>Web (CSS/SVG/X11) named color <c>MediumBlue</c> (#0000CD).</summary>
    public static Color MediumBlue => Color.Rgb(0, 0, 205);

    /// <summary>Web (CSS/SVG/X11) named color <c>MediumOrchid</c> (#BA55D3).</summary>
    public static Color MediumOrchid => Color.Rgb(186, 85, 211);

    /// <summary>Web (CSS/SVG/X11) named color <c>MediumPurple</c> (#9370DB).</summary>
    public static Color MediumPurple => Color.Rgb(147, 112, 219);

    /// <summary>Web (CSS/SVG/X11) named color <c>MediumSeaGreen</c> (#3CB371).</summary>
    public static Color MediumSeaGreen => Color.Rgb(60, 179, 113);

    /// <summary>Web (CSS/SVG/X11) named color <c>MediumSlateBlue</c> (#7B68EE).</summary>
    public static Color MediumSlateBlue => Color.Rgb(123, 104, 238);

    /// <summary>Web (CSS/SVG/X11) named color <c>MediumSpringGreen</c> (#00FA9A).</summary>
    public static Color MediumSpringGreen => Color.Rgb(0, 250, 154);

    /// <summary>Web (CSS/SVG/X11) named color <c>MediumTurquoise</c> (#48D1CC).</summary>
    public static Color MediumTurquoise => Color.Rgb(72, 209, 204);

    /// <summary>Web (CSS/SVG/X11) named color <c>MediumVioletRed</c> (#C71585).</summary>
    public static Color MediumVioletRed => Color.Rgb(199, 21, 133);

    /// <summary>Web (CSS/SVG/X11) named color <c>MidnightBlue</c> (#191970).</summary>
    public static Color MidnightBlue => Color.Rgb(25, 25, 112);

    /// <summary>Web (CSS/SVG/X11) named color <c>MintCream</c> (#F5FFFA).</summary>
    public static Color MintCream => Color.Rgb(245, 255, 250);

    /// <summary>Web (CSS/SVG/X11) named color <c>MistyRose</c> (#FFE4E1).</summary>
    public static Color MistyRose => Color.Rgb(255, 228, 225);

    /// <summary>Web (CSS/SVG/X11) named color <c>Moccasin</c> (#FFE4B5).</summary>
    public static Color Moccasin => Color.Rgb(255, 228, 181);

    /// <summary>Web (CSS/SVG/X11) named color <c>NavajoWhite</c> (#FFDEAD).</summary>
    public static Color NavajoWhite => Color.Rgb(255, 222, 173);

    /// <summary>Web (CSS/SVG/X11) named color <c>Navy</c> (#000080).</summary>
    public static Color Navy => Color.Rgb(0, 0, 128);

    /// <summary>Web (CSS/SVG/X11) named color <c>OldLace</c> (#FDF5E6).</summary>
    public static Color OldLace => Color.Rgb(253, 245, 230);

    /// <summary>Web (CSS/SVG/X11) named color <c>Olive</c> (#808000).</summary>
    public static Color Olive => Color.Rgb(128, 128, 0);

    /// <summary>Web (CSS/SVG/X11) named color <c>OliveDrab</c> (#6B8E23).</summary>
    public static Color OliveDrab => Color.Rgb(107, 142, 35);

    /// <summary>Web (CSS/SVG/X11) named color <c>Orange</c> (#FFA500).</summary>
    public static Color Orange => Color.Rgb(255, 165, 0);

    /// <summary>Web (CSS/SVG/X11) named color <c>OrangeRed</c> (#FF4500).</summary>
    public static Color OrangeRed => Color.Rgb(255, 69, 0);

    /// <summary>Web (CSS/SVG/X11) named color <c>Orchid</c> (#DA70D6).</summary>
    public static Color Orchid => Color.Rgb(218, 112, 214);

    /// <summary>Web (CSS/SVG/X11) named color <c>PaleGoldenrod</c> (#EEE8AA).</summary>
    public static Color PaleGoldenrod => Color.Rgb(238, 232, 170);

    /// <summary>Web (CSS/SVG/X11) named color <c>PaleGreen</c> (#98FB98).</summary>
    public static Color PaleGreen => Color.Rgb(152, 251, 152);

    /// <summary>Web (CSS/SVG/X11) named color <c>PaleTurquoise</c> (#AFEEEE).</summary>
    public static Color PaleTurquoise => Color.Rgb(175, 238, 238);

    /// <summary>Web (CSS/SVG/X11) named color <c>PaleVioletRed</c> (#DB7093).</summary>
    public static Color PaleVioletRed => Color.Rgb(219, 112, 147);

    /// <summary>Web (CSS/SVG/X11) named color <c>PapayaWhip</c> (#FFEFD5).</summary>
    public static Color PapayaWhip => Color.Rgb(255, 239, 213);

    /// <summary>Web (CSS/SVG/X11) named color <c>PeachPuff</c> (#FFDAB9).</summary>
    public static Color PeachPuff => Color.Rgb(255, 218, 185);

    /// <summary>Web (CSS/SVG/X11) named color <c>Peru</c> (#CD853F).</summary>
    public static Color Peru => Color.Rgb(205, 133, 63);

    /// <summary>Web (CSS/SVG/X11) named color <c>Pink</c> (#FFC0CB).</summary>
    public static Color Pink => Color.Rgb(255, 192, 203);

    /// <summary>Web (CSS/SVG/X11) named color <c>Plum</c> (#DDA0DD).</summary>
    public static Color Plum => Color.Rgb(221, 160, 221);

    /// <summary>Web (CSS/SVG/X11) named color <c>PowderBlue</c> (#B0E0E6).</summary>
    public static Color PowderBlue => Color.Rgb(176, 224, 230);

    /// <summary>Web (CSS/SVG/X11) named color <c>Purple</c> (#800080).</summary>
    public static Color Purple => Color.Rgb(128, 0, 128);

    /// <summary>Web (CSS/SVG/X11) named color <c>RebeccaPurple</c> (#663399).</summary>
    public static Color RebeccaPurple => Color.Rgb(102, 51, 153);

    /// <summary>Web (CSS/SVG/X11) named color <c>Red</c> (#FF0000).</summary>
    public static Color Red => Color.Rgb(255, 0, 0);

    /// <summary>Web (CSS/SVG/X11) named color <c>RosyBrown</c> (#BC8F8F).</summary>
    public static Color RosyBrown => Color.Rgb(188, 143, 143);

    /// <summary>Web (CSS/SVG/X11) named color <c>RoyalBlue</c> (#4169E1).</summary>
    public static Color RoyalBlue => Color.Rgb(65, 105, 225);

    /// <summary>Web (CSS/SVG/X11) named color <c>SaddleBrown</c> (#8B4513).</summary>
    public static Color SaddleBrown => Color.Rgb(139, 69, 19);

    /// <summary>Web (CSS/SVG/X11) named color <c>Salmon</c> (#FA8072).</summary>
    public static Color Salmon => Color.Rgb(250, 128, 114);

    /// <summary>Web (CSS/SVG/X11) named color <c>SandyBrown</c> (#F4A460).</summary>
    public static Color SandyBrown => Color.Rgb(244, 164, 96);

    /// <summary>Web (CSS/SVG/X11) named color <c>SeaGreen</c> (#2E8B57).</summary>
    public static Color SeaGreen => Color.Rgb(46, 139, 87);

    /// <summary>Web (CSS/SVG/X11) named color <c>SeaShell</c> (#FFF5EE).</summary>
    public static Color SeaShell => Color.Rgb(255, 245, 238);

    /// <summary>Web (CSS/SVG/X11) named color <c>Sienna</c> (#A0522D).</summary>
    public static Color Sienna => Color.Rgb(160, 82, 45);

    /// <summary>Web (CSS/SVG/X11) named color <c>Silver</c> (#C0C0C0).</summary>
    public static Color Silver => Color.Rgb(192, 192, 192);

    /// <summary>Web (CSS/SVG/X11) named color <c>SkyBlue</c> (#87CEEB).</summary>
    public static Color SkyBlue => Color.Rgb(135, 206, 235);

    /// <summary>Web (CSS/SVG/X11) named color <c>SlateBlue</c> (#6A5ACD).</summary>
    public static Color SlateBlue => Color.Rgb(106, 90, 205);

    /// <summary>Web (CSS/SVG/X11) named color <c>SlateGray</c> (#708090).</summary>
    public static Color SlateGray => Color.Rgb(112, 128, 144);

    /// <summary>Web (CSS/SVG/X11) named color <c>Snow</c> (#FFFAFA).</summary>
    public static Color Snow => Color.Rgb(255, 250, 250);

    /// <summary>Web (CSS/SVG/X11) named color <c>SpringGreen</c> (#00FF7F).</summary>
    public static Color SpringGreen => Color.Rgb(0, 255, 127);

    /// <summary>Web (CSS/SVG/X11) named color <c>SteelBlue</c> (#4682B4).</summary>
    public static Color SteelBlue => Color.Rgb(70, 130, 180);

    /// <summary>Web (CSS/SVG/X11) named color <c>Tan</c> (#D2B48C).</summary>
    public static Color Tan => Color.Rgb(210, 180, 140);

    /// <summary>Web (CSS/SVG/X11) named color <c>Teal</c> (#008080).</summary>
    public static Color Teal => Color.Rgb(0, 128, 128);

    /// <summary>Web (CSS/SVG/X11) named color <c>Thistle</c> (#D8BFD8).</summary>
    public static Color Thistle => Color.Rgb(216, 191, 216);

    /// <summary>Web (CSS/SVG/X11) named color <c>Tomato</c> (#FF6347).</summary>
    public static Color Tomato => Color.Rgb(255, 99, 71);

    /// <summary>Web (CSS/SVG/X11) named color <c>Turquoise</c> (#40E0D0).</summary>
    public static Color Turquoise => Color.Rgb(64, 224, 208);

    /// <summary>Web (CSS/SVG/X11) named color <c>Violet</c> (#EE82EE).</summary>
    public static Color Violet => Color.Rgb(238, 130, 238);

    /// <summary>Web (CSS/SVG/X11) named color <c>Wheat</c> (#F5DEB3).</summary>
    public static Color Wheat => Color.Rgb(245, 222, 179);

    /// <summary>Web (CSS/SVG/X11) named color <c>White</c> (#FFFFFF).</summary>
    public static Color White => Color.Rgb(255, 255, 255);

    /// <summary>Web (CSS/SVG/X11) named color <c>WhiteSmoke</c> (#F5F5F5).</summary>
    public static Color WhiteSmoke => Color.Rgb(245, 245, 245);

    /// <summary>Web (CSS/SVG/X11) named color <c>Yellow</c> (#FFFF00).</summary>
    public static Color Yellow => Color.Rgb(255, 255, 0);

    /// <summary>Web (CSS/SVG/X11) named color <c>YellowGreen</c> (#9ACD32).</summary>
    public static Color YellowGreen => Color.Rgb(154, 205, 50);
}