// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Text;

namespace XenoAtom.Terminal.UI.Figlet;

partial class FigletFont
{
    /// <summary>
    /// Gets the Digital FIGlet font used for rendering text in the FIGlet format. From https://www.figlet.org/fonts/digital.flf.
    /// </summary>
    public static FigletFont Digital => DigitalFontHolder.Instance;

    /// <summary>
    /// Hold the Digital font instance for lazy initialization and NativeAOT friendliness.
    /// </summary>
    private static class DigitalFontHolder
    {
        public static readonly FigletFont Instance = FigletFont.Parse(DigitalFontData, new("Digital", "Glenn Chappell", "https://www.figlet.org/fonts/digital.flf"));

        // https://www.figlet.org/fonts/digital.flf
        private const string DigitalFontData = """
                                               flf2a 3 2 6 1 11 0 16513
                                               Digital by Glenn Chappell 1/94 -- based on Bubble
                                               Includes characters 128-255
                                               Enhanced for Latin-2,3,4 by John Cowan <cowan@ccil.org>
                                               Latin character sets supported only if your screen font does
                                               figlet release 2.2 -- November 1996
                                               Permission is hereby given to modify this font, as long as the
                                               modifier's name is placed on a comment line.
                                               
                                               Modified by Paul Burton <solution@earthlink.net> 12/96 to include new parameter
                                               supported by FIGlet and FIGWin.  May also be slightly modified for better use
                                               of new full-width/kern/smush alternatives, but default output is NOT changed.
                                                @
                                                @
                                                @@
                                                +-+@
                                                |!|@
                                                +-+@@
                                                +-+@
                                                |"|@
                                                +-+@@
                                                +-+@
                                                |#|@
                                                +-+@@
                                                +-+@
                                                |$|@
                                                +-+@@
                                                +-+@
                                                |%|@
                                                +-+@@
                                                +-+@
                                                |&|@
                                                +-+@@
                                                +-+@
                                                |'|@
                                                +-+@@
                                                +-+@
                                                |(|@
                                                +-+@@
                                                +-+@
                                                |)|@
                                                +-+@@
                                                +-+@
                                                |*|@
                                                +-+@@
                                                +-+@
                                                |+|@
                                                +-+@@
                                                +-+@
                                                |,|@
                                                +-+@@
                                                +-+@
                                                |-|@
                                                +-+@@
                                                +-+@
                                                |.|@
                                                +-+@@
                                                +-+@
                                                |/|@
                                                +-+@@
                                                +-+@
                                                |0|@
                                                +-+@@
                                                +-+@
                                                |1|@
                                                +-+@@
                                                +-+@
                                                |2|@
                                                +-+@@
                                                +-+@
                                                |3|@
                                                +-+@@
                                                +-+@
                                                |4|@
                                                +-+@@
                                                +-+@
                                                |5|@
                                                +-+@@
                                                +-+@
                                                |6|@
                                                +-+@@
                                                +-+@
                                                |7|@
                                                +-+@@
                                                +-+@
                                                |8|@
                                                +-+@@
                                                +-+@
                                                |9|@
                                                +-+@@
                                                +-+@
                                                |:|@
                                                +-+@@
                                                +-+@
                                                |;|@
                                                +-+@@
                                                +-+@
                                                |<|@
                                                +-+@@
                                                +-+@
                                                |=|@
                                                +-+@@
                                                +-+@
                                                |>|@
                                                +-+@@
                                                +-+@
                                                |?|@
                                                +-+@@
                                                +-+@
                                                |@|@
                                                +-+@@
                                                +-+@
                                                |A|@
                                                +-+@@
                                                +-+@
                                                |B|@
                                                +-+@@
                                                +-+@
                                                |C|@
                                                +-+@@
                                                +-+@
                                                |D|@
                                                +-+@@
                                                +-+@
                                                |E|@
                                                +-+@@
                                                +-+@
                                                |F|@
                                                +-+@@
                                                +-+@
                                                |G|@
                                                +-+@@
                                                +-+@
                                                |H|@
                                                +-+@@
                                                +-+@
                                                |I|@
                                                +-+@@
                                                +-+@
                                                |J|@
                                                +-+@@
                                                +-+@
                                                |K|@
                                                +-+@@
                                                +-+@
                                                |L|@
                                                +-+@@
                                                +-+@
                                                |M|@
                                                +-+@@
                                                +-+@
                                                |N|@
                                                +-+@@
                                                +-+@
                                                |O|@
                                                +-+@@
                                                +-+@
                                                |P|@
                                                +-+@@
                                                +-+@
                                                |Q|@
                                                +-+@@
                                                +-+@
                                                |R|@
                                                +-+@@
                                                +-+@
                                                |S|@
                                                +-+@@
                                                +-+@
                                                |T|@
                                                +-+@@
                                                +-+@
                                                |U|@
                                                +-+@@
                                                +-+@
                                                |V|@
                                                +-+@@
                                                +-+@
                                                |W|@
                                                +-+@@
                                                +-+@
                                                |X|@
                                                +-+@@
                                                +-+@
                                                |Y|@
                                                +-+@@
                                                +-+@
                                                |Z|@
                                                +-+@@
                                                +-+@
                                                |[|@
                                                +-+@@
                                                +-+@
                                                |\|@
                                                +-+@@
                                                +-+@
                                                |]|@
                                                +-+@@
                                                +-+@
                                                |^|@
                                                +-+@@
                                                +-+@
                                                |_|@
                                                +-+@@
                                                +-+@
                                                |`|@
                                                +-+@@
                                                +-+@
                                                |a|@
                                                +-+@@
                                                +-+@
                                                |b|@
                                                +-+@@
                                                +-+@
                                                |c|@
                                                +-+@@
                                                +-+@
                                                |d|@
                                                +-+@@
                                                +-+@
                                                |e|@
                                                +-+@@
                                                +-+@
                                                |f|@
                                                +-+@@
                                                +-+@
                                                |g|@
                                                +-+@@
                                                +-+@
                                                |h|@
                                                +-+@@
                                                +-+@
                                                |i|@
                                                +-+@@
                                                +-+@
                                                |j|@
                                                +-+@@
                                                +-+@
                                                |k|@
                                                +-+@@
                                                +-+@
                                                |l|@
                                                +-+@@
                                                +-+@
                                                |m|@
                                                +-+@@
                                                +-+@
                                                |n|@
                                                +-+@@
                                                +-+@
                                                |o|@
                                                +-+@@
                                                +-+@
                                                |p|@
                                                +-+@@
                                                +-+@
                                                |q|@
                                                +-+@@
                                                +-+@
                                                |r|@
                                                +-+@@
                                                +-+@
                                                |s|@
                                                +-+@@
                                                +-+@
                                                |t|@
                                                +-+@@
                                                +-+@
                                                |u|@
                                                +-+@@
                                                +-+@
                                                |v|@
                                                +-+@@
                                                +-+@
                                                |w|@
                                                +-+@@
                                                +-+@
                                                |x|@
                                                +-+@@
                                                +-+@
                                                |y|@
                                                +-+@@
                                                +-+@
                                                |z|@
                                                +-+@@
                                                +-+@
                                                |{|@
                                                +-+@@
                                                +-+@
                                                |||@
                                                +-+@@
                                                +-+@
                                                |}|@
                                                +-+@@
                                                +-+@
                                                |~|@
                                                +-+@@
                                                +-+@
                                                |Ä|@
                                                +-+@@
                                                +-+@
                                                |Ö|@
                                                +-+@@
                                                +-+@
                                                |Ü|@
                                                +-+@@
                                                +-+@
                                                |ä|@
                                                +-+@@
                                                +-+@
                                                |ö|@
                                                +-+@@
                                                +-+@
                                                |ü|@
                                                +-+@@
                                                +-+@
                                                |ß|@
                                                +-+@@
                                               128
                                                +-+@
                                                |€|@
                                                +-+@@
                                               129
                                                +-+@
                                                ||@
                                                +-+@@
                                               130
                                                +-+@
                                                |‚|@
                                                +-+@@
                                               131
                                                +-+@
                                                |ƒ|@
                                                +-+@@
                                               132
                                                +-+@
                                                |„|@
                                                +-+@@
                                               133
                                                +-+@
                                                |…|@
                                                +-+@@
                                               134
                                                +-+@
                                                |†|@
                                                +-+@@
                                               135
                                                +-+@
                                                |‡|@
                                                +-+@@
                                               136
                                                +-+@
                                                |ˆ|@
                                                +-+@@
                                               137
                                                +-+@
                                                |‰|@
                                                +-+@@
                                               138
                                                +-+@
                                                |Š|@
                                                +-+@@
                                               139
                                                +-+@
                                                |‹|@
                                                +-+@@
                                               140
                                                +-+@
                                                |Œ|@
                                                +-+@@
                                               141
                                                +-+@
                                                ||@
                                                +-+@@
                                               142
                                                +-+@
                                                |Ž|@
                                                +-+@@
                                               143
                                                +-+@
                                                ||@
                                                +-+@@
                                               144
                                                +-+@
                                                ||@
                                                +-+@@
                                               145
                                                +-+@
                                                |‘|@
                                                +-+@@
                                               146
                                                +-+@
                                                |’|@
                                                +-+@@
                                               147
                                                +-+@
                                                |“|@
                                                +-+@@
                                               148
                                                +-+@
                                                |”|@
                                                +-+@@
                                               149
                                                +-+@
                                                |•|@
                                                +-+@@
                                               150
                                                +-+@
                                                |–|@
                                                +-+@@
                                               151
                                                +-+@
                                                |—|@
                                                +-+@@
                                               152
                                                +-+@
                                                |˜|@
                                                +-+@@
                                               153
                                                +-+@
                                                |™|@
                                                +-+@@
                                               154
                                                +-+@
                                                |š|@
                                                +-+@@
                                               155
                                                +-+@
                                                |›|@
                                                +-+@@
                                               156
                                                +-+@
                                                |œ|@
                                                +-+@@
                                               157
                                                +-+@
                                                ||@
                                                +-+@@
                                               158
                                                +-+@
                                                |ž|@
                                                +-+@@
                                               159
                                                +-+@
                                                |Ÿ|@
                                                +-+@@
                                               160  NO-BREAK SPACE
                                                +-+@
                                                | |@
                                                +-+@@
                                               161  INVERTED EXCLAMATION MARK
                                                +-+@
                                                |¡|@
                                                +-+@@
                                               162  CENT SIGN
                                                +-+@
                                                |¢|@
                                                +-+@@
                                               163  POUND SIGN
                                                +-+@
                                                |£|@
                                                +-+@@
                                               164  CURRENCY SIGN
                                                +-+@
                                                |¤|@
                                                +-+@@
                                               165  YEN SIGN
                                                +-+@
                                                |¥|@
                                                +-+@@
                                               166  BROKEN BAR
                                                +-+@
                                                |¦|@
                                                +-+@@
                                               167  SECTION SIGN
                                                +-+@
                                                |§|@
                                                +-+@@
                                               168  DIAERESIS
                                                +-+@
                                                |¨|@
                                                +-+@@
                                               169  COPYRIGHT SIGN
                                                +-+@
                                                |©|@
                                                +-+@@
                                               170  FEMININE ORDINAL INDICATOR
                                                +-+@
                                                |ª|@
                                                +-+@@
                                               171  LEFT-POINTING DOUBLE ANGLE QUOTATION MARK
                                                +-+@
                                                |«|@
                                                +-+@@
                                               172  NOT SIGN
                                                +-+@
                                                |¬|@
                                                +-+@@
                                               173  SOFT HYPHEN
                                                +-+@
                                                |­|@
                                                +-+@@
                                               174  REGISTERED SIGN
                                                +-+@
                                                |®|@
                                                +-+@@
                                               175  MACRON
                                                +-+@
                                                |¯|@
                                                +-+@@
                                               176  DEGREE SIGN
                                                +-+@
                                                |°|@
                                                +-+@@
                                               177  PLUS-MINUS SIGN
                                                +-+@
                                                |±|@
                                                +-+@@
                                               178  SUPERSCRIPT TWO
                                                +-+@
                                                |²|@
                                                +-+@@
                                               179  SUPERSCRIPT THREE
                                                +-+@
                                                |³|@
                                                +-+@@
                                               180  ACUTE ACCENT
                                                +-+@
                                                |´|@
                                                +-+@@
                                               181  MICRO SIGN
                                                +-+@
                                                |µ|@
                                                +-+@@
                                               182  PILCROW SIGN
                                                +-+@
                                                |¶|@
                                                +-+@@
                                               183  MIDDLE DOT
                                                +-+@
                                                |·|@
                                                +-+@@
                                               184  CEDILLA
                                                +-+@
                                                |¸|@
                                                +-+@@
                                               185  SUPERSCRIPT ONE
                                                +-+@
                                                |¹|@
                                                +-+@@
                                               186  MASCULINE ORDINAL INDICATOR
                                                +-+@
                                                |º|@
                                                +-+@@
                                               187  RIGHT-POINTING DOUBLE ANGLE QUOTATION MARK
                                                +-+@
                                                |»|@
                                                +-+@@
                                               188  VULGAR FRACTION ONE QUARTER
                                                +-+@
                                                |¼|@
                                                +-+@@
                                               189  VULGAR FRACTION ONE HALF
                                                +-+@
                                                |½|@
                                                +-+@@
                                               190  VULGAR FRACTION THREE QUARTERS
                                                +-+@
                                                |¾|@
                                                +-+@@
                                               191  INVERTED QUESTION MARK
                                                +-+@
                                                |¿|@
                                                +-+@@
                                               192  LATIN CAPITAL LETTER A WITH GRAVE
                                                +-+@
                                                |À|@
                                                +-+@@
                                               193  LATIN CAPITAL LETTER A WITH ACUTE
                                                +-+@
                                                |Á|@
                                                +-+@@
                                               194  LATIN CAPITAL LETTER A WITH CIRCUMFLEX
                                                +-+@
                                                |Â|@
                                                +-+@@
                                               195  LATIN CAPITAL LETTER A WITH TILDE
                                                +-+@
                                                |Ã|@
                                                +-+@@
                                               196  LATIN CAPITAL LETTER A WITH DIAERESIS
                                                +-+@
                                                |Ä|@
                                                +-+@@
                                               197  LATIN CAPITAL LETTER A WITH RING ABOVE
                                                +-+@
                                                |Å|@
                                                +-+@@
                                               198  LATIN CAPITAL LETTER AE
                                                +-+@
                                                |Æ|@
                                                +-+@@
                                               199  LATIN CAPITAL LETTER C WITH CEDILLA
                                                +-+@
                                                |Ç|@
                                                +-+@@
                                               200  LATIN CAPITAL LETTER E WITH GRAVE
                                                +-+@
                                                |È|@
                                                +-+@@
                                               201  LATIN CAPITAL LETTER E WITH ACUTE
                                                +-+@
                                                |É|@
                                                +-+@@
                                               202  LATIN CAPITAL LETTER E WITH CIRCUMFLEX
                                                +-+@
                                                |Ê|@
                                                +-+@@
                                               203  LATIN CAPITAL LETTER E WITH DIAERESIS
                                                +-+@
                                                |Ë|@
                                                +-+@@
                                               204  LATIN CAPITAL LETTER I WITH GRAVE
                                                +-+@
                                                |Ì|@
                                                +-+@@
                                               205  LATIN CAPITAL LETTER I WITH ACUTE
                                                +-+@
                                                |Í|@
                                                +-+@@
                                               206  LATIN CAPITAL LETTER I WITH CIRCUMFLEX
                                                +-+@
                                                |Î|@
                                                +-+@@
                                               207  LATIN CAPITAL LETTER I WITH DIAERESIS
                                                +-+@
                                                |Ï|@
                                                +-+@@
                                               208  LATIN CAPITAL LETTER ETH
                                                +-+@
                                                |Ð|@
                                                +-+@@
                                               209  LATIN CAPITAL LETTER N WITH TILDE
                                                +-+@
                                                |Ñ|@
                                                +-+@@
                                               210  LATIN CAPITAL LETTER O WITH GRAVE
                                                +-+@
                                                |Ò|@
                                                +-+@@
                                               211  LATIN CAPITAL LETTER O WITH ACUTE
                                                +-+@
                                                |Ó|@
                                                +-+@@
                                               212  LATIN CAPITAL LETTER O WITH CIRCUMFLEX
                                                +-+@
                                                |Ô|@
                                                +-+@@
                                               213  LATIN CAPITAL LETTER O WITH TILDE
                                                +-+@
                                                |Õ|@
                                                +-+@@
                                               214  LATIN CAPITAL LETTER O WITH DIAERESIS
                                                +-+@
                                                |Ö|@
                                                +-+@@
                                               215  MULTIPLICATION SIGN
                                                +-+@
                                                |×|@
                                                +-+@@
                                               216  LATIN CAPITAL LETTER O WITH STROKE
                                                +-+@
                                                |Ø|@
                                                +-+@@
                                               217  LATIN CAPITAL LETTER U WITH GRAVE
                                                +-+@
                                                |Ù|@
                                                +-+@@
                                               218  LATIN CAPITAL LETTER U WITH ACUTE
                                                +-+@
                                                |Ú|@
                                                +-+@@
                                               219  LATIN CAPITAL LETTER U WITH CIRCUMFLEX
                                                +-+@
                                                |Û|@
                                                +-+@@
                                               220  LATIN CAPITAL LETTER U WITH DIAERESIS
                                                +-+@
                                                |Ü|@
                                                +-+@@
                                               221  LATIN CAPITAL LETTER Y WITH ACUTE
                                                +-+@
                                                |Ý|@
                                                +-+@@
                                               222  LATIN CAPITAL LETTER THORN
                                                +-+@
                                                |Þ|@
                                                +-+@@
                                               223  LATIN SMALL LETTER SHARP S
                                                +-+@
                                                |ß|@
                                                +-+@@
                                               224  LATIN SMALL LETTER A WITH GRAVE
                                                +-+@
                                                |à|@
                                                +-+@@
                                               225  LATIN SMALL LETTER A WITH ACUTE
                                                +-+@
                                                |á|@
                                                +-+@@
                                               226  LATIN SMALL LETTER A WITH CIRCUMFLEX
                                                +-+@
                                                |â|@
                                                +-+@@
                                               227  LATIN SMALL LETTER A WITH TILDE
                                                +-+@
                                                |ã|@
                                                +-+@@
                                               228  LATIN SMALL LETTER A WITH DIAERESIS
                                                +-+@
                                                |ä|@
                                                +-+@@
                                               229  LATIN SMALL LETTER A WITH RING ABOVE
                                                +-+@
                                                |å|@
                                                +-+@@
                                               230  LATIN SMALL LETTER AE
                                                +-+@
                                                |æ|@
                                                +-+@@
                                               231  LATIN SMALL LETTER C WITH CEDILLA
                                                +-+@
                                                |ç|@
                                                +-+@@
                                               232  LATIN SMALL LETTER E WITH GRAVE
                                                +-+@
                                                |è|@
                                                +-+@@
                                               233  LATIN SMALL LETTER E WITH ACUTE
                                                +-+@
                                                |é|@
                                                +-+@@
                                               234  LATIN SMALL LETTER E WITH CIRCUMFLEX
                                                +-+@
                                                |ê|@
                                                +-+@@
                                               235  LATIN SMALL LETTER E WITH DIAERESIS
                                                +-+@
                                                |ë|@
                                                +-+@@
                                               236  LATIN SMALL LETTER I WITH GRAVE
                                                +-+@
                                                |ì|@
                                                +-+@@
                                               237  LATIN SMALL LETTER I WITH ACUTE
                                                +-+@
                                                |í|@
                                                +-+@@
                                               238  LATIN SMALL LETTER I WITH CIRCUMFLEX
                                                +-+@
                                                |î|@
                                                +-+@@
                                               239  LATIN SMALL LETTER I WITH DIAERESIS
                                                +-+@
                                                |ï|@
                                                +-+@@
                                               240  LATIN SMALL LETTER ETH
                                                +-+@
                                                |ð|@
                                                +-+@@
                                               241  LATIN SMALL LETTER N WITH TILDE
                                                +-+@
                                                |ñ|@
                                                +-+@@
                                               242  LATIN SMALL LETTER O WITH GRAVE
                                                +-+@
                                                |ò|@
                                                +-+@@
                                               243  LATIN SMALL LETTER O WITH ACUTE
                                                +-+@
                                                |ó|@
                                                +-+@@
                                               244  LATIN SMALL LETTER O WITH CIRCUMFLEX
                                                +-+@
                                                |ô|@
                                                +-+@@
                                               245  LATIN SMALL LETTER O WITH TILDE
                                                +-+@
                                                |õ|@
                                                +-+@@
                                               246  LATIN SMALL LETTER O WITH DIAERESIS
                                                +-+@
                                                |ö|@
                                                +-+@@
                                               247  DIVISION SIGN
                                                +-+@
                                                |÷|@
                                                +-+@@
                                               248  LATIN SMALL LETTER O WITH STROKE
                                                +-+@
                                                |ø|@
                                                +-+@@
                                               249  LATIN SMALL LETTER U WITH GRAVE
                                                +-+@
                                                |ù|@
                                                +-+@@
                                               250  LATIN SMALL LETTER U WITH ACUTE
                                                +-+@
                                                |ú|@
                                                +-+@@
                                               251  LATIN SMALL LETTER U WITH CIRCUMFLEX
                                                +-+@
                                                |û|@
                                                +-+@@
                                               252  LATIN SMALL LETTER U WITH DIAERESIS
                                                +-+@
                                                |ü|@
                                                +-+@@
                                               253  LATIN SMALL LETTER Y WITH ACUTE
                                                +-+@
                                                |ý|@
                                                +-+@@
                                               254  LATIN SMALL LETTER THORN
                                                +-+@
                                                |þ|@
                                                +-+@@
                                               255  LATIN SMALL LETTER Y WITH DIAERESIS
                                                +-+@
                                                |ÿ|@
                                                +-+@@
                                               0x0100  LATIN CAPITAL LETTER A WITH MACRON
                                                +-+@
                                                |À|@
                                                +-+@@
                                               0x0101  LATIN SMALL LETTER A WITH MACRON
                                                +-+@
                                                |à|@
                                                +-+@@
                                               0x0102  LATIN CAPITAL LETTER A WITH BREVE
                                                +-+@
                                                |Ã|@
                                                +-+@@
                                               0x0103  LATIN SMALL LETTER A WITH BREVE
                                                +-+@
                                                |ã|@
                                                +-+@@
                                               0x0104  LATIN CAPITAL LETTER A WITH OGONEK
                                                +-+@
                                                |¡|@
                                                +-+@@
                                               0x0105  LATIN SMALL LETTER A WITH OGONEK
                                                +-+@
                                                |±|@
                                                +-+@@
                                               0x0106  LATIN CAPITAL LETTER C WITH ACUTE
                                                +-+@
                                                |Æ|@
                                                +-+@@
                                               0x0107  LATIN SMALL LETTER C WITH ACUTE
                                                +-+@
                                                |æ|@
                                                +-+@@
                                               0x0108  LATIN CAPITAL LETTER C WITH CIRCUMFLEX
                                                +-+@
                                                |Æ|@
                                                +-+@@
                                               0x0109  LATIN SMALL LETTER C WITH CIRCUMFLEX
                                                +-+@
                                                |æ|@
                                                +-+@@
                                               0x010A  LATIN CAPITAL LETTER C WITH DOT ABOVE
                                                +-+@
                                                |Å|@
                                                +-+@@
                                               0x010B  LATIN SMALL LETTER C WITH DOT ABOVE
                                                +-+@
                                                |å|@
                                                +-+@@
                                               0x010C  LATIN CAPITAL LETTER C WITH CARON
                                                +-+@
                                                |È|@
                                                +-+@@
                                               0x010D  LATIN SMALL LETTER C WITH CARON
                                                +-+@
                                                |è|@
                                                +-+@@
                                               0x010E  LATIN CAPITAL LETTER D WITH CARON
                                                +-+@
                                                |Ï|@
                                                +-+@@
                                               0x010F  LATIN SMALL LETTER D WITH CARON
                                                +-+@
                                                |ï|@
                                                +-+@@
                                               0x0110  LATIN CAPITAL LETTER D WITH STROKE
                                                +-+@
                                                |Ð|@
                                                +-+@@
                                               0x0111  LATIN SMALL LETTER D WITH STROKE
                                                +-+@
                                                |ð|@
                                                +-+@@
                                               0x0112  LATIN CAPITAL LETTER E WITH MACRON
                                                +-+@
                                                |ª|@
                                                +-+@@
                                               0x0113  LATIN SMALL LETTER E WITH MACRON
                                                +-+@
                                                |º|@
                                                +-+@@
                                               0x0116  LATIN CAPITAL LETTER E WITH DOT ABOVE
                                                +-+@
                                                |Ì|@
                                                +-+@@
                                               0x0117  LATIN SMALL LETTER E WITH DOT ABOVE
                                                +-+@
                                                |ì|@
                                                +-+@@
                                               0x0118  LATIN CAPITAL LETTER E WITH OGONEK
                                                +-+@
                                                |Ê|@
                                                +-+@@
                                               0x0119  LATIN SMALL LETTER E WITH OGONEK
                                                +-+@
                                                |ê|@
                                                +-+@@
                                               0x011A  LATIN CAPITAL LETTER E WITH CARON
                                                +-+@
                                                |Ì|@
                                                +-+@@
                                               0x011B  LATIN SMALL LETTER E WITH CARON
                                                +-+@
                                                |ì|@
                                                +-+@@
                                               0x011C  LATIN CAPITAL LETTER G WITH CIRCUMFLEX
                                                +-+@
                                                |Ø|@
                                                +-+@@
                                               0x011D  LATIN SMALL LETTER G WITH CIRCUMFLEX
                                                +-+@
                                                |ø|@
                                                +-+@@
                                               0x011E  LATIN CAPITAL LETTER G WITH BREVE
                                                +-+@
                                                |«|@
                                                +-+@@
                                               0x011F  LATIN SMALL LETTER G WITH BREVE
                                                +-+@
                                                |»|@
                                                +-+@@
                                               0x0120  LATIN CAPITAL LETTER G WITH DOT ABOVE
                                                +-+@
                                                |Õ|@
                                                +-+@@
                                               0x0121  LATIN SMALL LETTER G WITH DOT ABOVE
                                                +-+@
                                                |õ|@
                                                +-+@@
                                               0x0122  LATIN CAPITAL LETTER G WITH CEDILLA
                                                +-+@
                                                |«|@
                                                +-+@@
                                               0x0123  LATIN SMALL LETTER G WITH CEDILLA
                                                +-+@
                                                |»|@
                                                +-+@@
                                               0x0124  LATIN CAPITAL LETTER H WITH CIRCUMFLEX
                                                +-+@
                                                |¦|@
                                                +-+@@
                                               0x0125  LATIN SMALL LETTER H WITH CIRCUMFLEX
                                                +-+@
                                                |¶|@
                                                +-+@@
                                               0x0126  LATIN CAPITAL LETTER H WITH STROKE
                                                +-+@
                                                |¡|@
                                                +-+@@
                                               0x0127  LATIN SMALL LETTER H WITH STROKE
                                                +-+@
                                                |±|@
                                                +-+@@
                                               0x0128  LATIN CAPITAL LETTER I WITH TILDE
                                                +-+@
                                                |¥|@
                                                +-+@@
                                               0x0129  LATIN SMALL LETTER I WITH TILDE
                                                +-+@
                                                |µ|@
                                                +-+@@
                                               0x012A  LATIN CAPITAL LETTER I WITH MACRON
                                                +-+@
                                                |Ï|@
                                                +-+@@
                                               0x012B  LATIN SMALL LETTER I WITH MACRON
                                                +-+@
                                                |ï|@
                                                +-+@@
                                               0x012E  LATIN CAPITAL LETTER I WITH OGONEK
                                                +-+@
                                                |Ç|@
                                                +-+@@
                                               0x012F  LATIN SMALL LETTER I WITH OGONEK
                                                +-+@
                                                |ç|@
                                                +-+@@
                                               0x0130  LATIN CAPITAL LETTER I WITH DOT ABOVE
                                                +-+@
                                                |©|@
                                                +-+@@
                                               0x0131  LATIN SMALL LETTER DOTLESS I
                                                +-+@
                                                |¹|@
                                                +-+@@
                                               0x0134  LATIN CAPITAL LETTER J WITH CIRCUMFLEX
                                                +-+@
                                                |¬|@
                                                +-+@@
                                               0x0135  LATIN SMALL LETTER J WITH CIRCUMFLEX
                                                +-+@
                                                |¼|@
                                                +-+@@
                                               0x0136  LATIN CAPITAL LETTER K WITH CEDILLA
                                                +-+@
                                                |Ó|@
                                                +-+@@
                                               0x0137  LATIN SMALL LETTER K WITH CEDILLA
                                                +-+@
                                                |ó|@
                                                +-+@@
                                               0x0138  LATIN SMALL LETTER KRA
                                                +-+@
                                                |¢|@
                                                +-+@@
                                               0x0139  LATIN CAPITAL LETTER L WITH ACUTE
                                                +-+@
                                                |Å|@
                                                +-+@@
                                               0x013A  LATIN SMALL LETTER L WITH ACUTE
                                                +-+@
                                                |å|@
                                                +-+@@
                                               0x013B  LATIN CAPITAL LETTER L WITH CEDILLA
                                                +-+@
                                                |¦|@
                                                +-+@@
                                               0x013C  LATIN SMALL LETTER L WITH CEDILLA
                                                +-+@
                                                |¶|@
                                                +-+@@
                                               0x013D  LATIN CAPITAL LETTER L WITH CARON
                                                +-+@
                                                |¥|@
                                                +-+@@
                                               0x013E  LATIN SMALL LETTER L WITH CARON
                                                +-+@
                                                |µ|@
                                                +-+@@
                                               0x0141  LATIN CAPITAL LETTER L WITH STROKE
                                                +-+@
                                                |£|@
                                                +-+@@
                                               0x0142  LATIN SMALL LETTER L WITH STROKE
                                                +-+@
                                                |³|@
                                                +-+@@
                                               0x0143  LATIN CAPITAL LETTER N WITH ACUTE
                                                +-+@
                                                |Ñ|@
                                                +-+@@
                                               0x0144  LATIN SMALL LETTER N WITH ACUTE
                                                +-+@
                                                |ñ|@
                                                +-+@@
                                               0x0145  LATIN CAPITAL LETTER N WITH CEDILLA
                                                +-+@
                                                |Ñ|@
                                                +-+@@
                                               0x0146  LATIN SMALL LETTER N WITH CEDILLA
                                                +-+@
                                                |ñ|@
                                                +-+@@
                                               0x0147  LATIN CAPITAL LETTER N WITH CARON
                                                +-+@
                                                |Ò|@
                                                +-+@@
                                               0x0148  LATIN SMALL LETTER N WITH CARON
                                                +-+@
                                                |ò|@
                                                +-+@@
                                               0x014A  LATIN CAPITAL LETTER ENG
                                                +-+@
                                                |½|@
                                                +-+@@
                                               0x014B  LATIN SMALL LETTER ENG
                                                +-+@
                                                |¿|@
                                                +-+@@
                                               0x014C  LATIN CAPITAL LETTER O WITH MACRON
                                                +-+@
                                                |Ò|@
                                                +-+@@
                                               0x014D  LATIN SMALL LETTER O WITH MACRON
                                                +-+@
                                                |ò|@
                                                +-+@@
                                               0x0150  LATIN CAPITAL LETTER O WITH DOUBLE ACUTE
                                                +-+@
                                                |Õ|@
                                                +-+@@
                                               0x0151  LATIN SMALL LETTER O WITH DOUBLE ACUTE
                                                +-+@
                                                |õ|@
                                                +-+@@
                                               0x0154  LATIN CAPITAL LETTER R WITH ACUTE
                                                +-+@
                                                |À|@
                                                +-+@@
                                               0x0155  LATIN SMALL LETTER R WITH ACUTE
                                                +-+@
                                                |à|@
                                                +-+@@
                                               0x0156  LATIN CAPITAL LETTER R WITH CEDILLA
                                                +-+@
                                                |£|@
                                                +-+@@
                                               0x0157  LATIN SMALL LETTER R WITH CEDILLA
                                                +-+@
                                                |³|@
                                                +-+@@
                                               0x0158  LATIN CAPITAL LETTER R WITH CARON
                                                +-+@
                                                |Ø|@
                                                +-+@@
                                               0x0159  LATIN SMALL LETTER R WITH CARON
                                                +-+@
                                                |ø|@
                                                +-+@@
                                               0x015A  LATIN CAPITAL LETTER S WITH ACUTE
                                                +-+@
                                                |¦|@
                                                +-+@@
                                               0x015B  LATIN SMALL LETTER S WITH ACUTE
                                                +-+@
                                                |¶|@
                                                +-+@@
                                               0x015C  LATIN CAPITAL LETTER S WITH CIRCUMFLEX
                                                +-+@
                                                |Þ|@
                                                +-+@@
                                               0x015D  LATIN SMALL LETTER S WITH CIRCUMFLEX
                                                +-+@
                                                |þ|@
                                                +-+@@
                                               0x015E  LATIN CAPITAL LETTER S WITH CEDILLA
                                                +-+@
                                                |ª|@
                                                +-+@@
                                               0x015F  LATIN SMALL LETTER S WITH CEDILLA
                                                +-+@
                                                |º|@
                                                +-+@@
                                               0x0160  LATIN CAPITAL LETTER S WITH CARON
                                                +-+@
                                                |©|@
                                                +-+@@
                                               0x0161  LATIN SMALL LETTER S WITH CARON
                                                +-+@
                                                |¹|@
                                                +-+@@
                                               0x0162  LATIN CAPITAL LETTER T WITH CEDILLA
                                                +-+@
                                                |Þ|@
                                                +-+@@
                                               0x0163  LATIN SMALL LETTER T WITH CEDILLA
                                                +-+@
                                                |þ|@
                                                +-+@@
                                               0x0164  LATIN CAPITAL LETTER T WITH CARON
                                                +-+@
                                                |«|@
                                                +-+@@
                                               0x0165  LATIN SMALL LETTER T WITH CARON
                                                +-+@
                                                |»|@
                                                +-+@@
                                               0x0166  LATIN CAPITAL LETTER T WITH STROKE
                                                +-+@
                                                |¬|@
                                                +-+@@
                                               0x0167  LATIN SMALL LETTER T WITH STROKE
                                                +-+@
                                                |¼|@
                                                +-+@@
                                               0x0168  LATIN CAPITAL LETTER U WITH TILDE
                                                +-+@
                                                |Ý|@
                                                +-+@@
                                               0x0169  LATIN SMALL LETTER U WITH TILDE
                                                +-+@
                                                |ý|@
                                                +-+@@
                                               0x016A  LATIN CAPITAL LETTER U WITH MACRON
                                                +-+@
                                                |Þ|@
                                                +-+@@
                                               0x016B  LATIN SMALL LETTER U WITH MACRON
                                                +-+@
                                                |þ|@
                                                +-+@@
                                               0x016C  LATIN CAPITAL LETTER U WITH BREVE
                                                +-+@
                                                |Ý|@
                                                +-+@@
                                               0x016D  LATIN SMALL LETTER U WITH BREVE
                                                +-+@
                                                |ý|@
                                                +-+@@
                                               0x016E  LATIN CAPITAL LETTER U WITH RING ABOVE
                                                +-+@
                                                |Ù|@
                                                +-+@@
                                               0x016F  LATIN SMALL LETTER U WITH RING ABOVE
                                                +-+@
                                                |ù|@
                                                +-+@@
                                               0x0170  LATIN CAPITAL LETTER U WITH DOUBLE ACUTE
                                                +-+@
                                                |Û|@
                                                +-+@@
                                               0x0171  LATIN SMALL LETTER U WITH DOUBLE ACUTE
                                                +-+@
                                                |û|@
                                                +-+@@
                                               0x0172  LATIN CAPITAL LETTER U WITH OGONEK
                                                +-+@
                                                |Ù|@
                                                +-+@@
                                               0x0173  LATIN SMALL LETTER U WITH OGONEK
                                                +-+@
                                                |ù|@
                                                +-+@@
                                               0x0179  LATIN CAPITAL LETTER Z WITH ACUTE
                                                +-+@
                                                |¬|@
                                                +-+@@
                                               0x017A  LATIN SMALL LETTER Z WITH ACUTE
                                                +-+@
                                                |¼|@
                                                +-+@@
                                               0x017B  LATIN CAPITAL LETTER Z WITH DOT ABOVE
                                                +-+@
                                                |¯|@
                                                +-+@@
                                               0x017C  LATIN SMALL LETTER Z WITH DOT ABOVE
                                                +-+@
                                                |¿|@
                                                +-+@@
                                               0x017D  LATIN CAPITAL LETTER Z WITH CARON
                                                +-+@
                                                |®|@
                                                +-+@@
                                               0x017E  LATIN SMALL LETTER Z WITH CARON
                                                +-+@
                                                |¾|@
                                                +-+@@
                                               0x02C7  CARON
                                                +-+@
                                                |·|@
                                                +-+@@
                                               0x02D8  BREVE
                                                +-+@
                                                |¢|@
                                                +-+@@
                                               0x02D9  DOT ABOVE
                                                +-+@
                                                |ÿ|@
                                                +-+@@
                                               0x02DB  OGONEK
                                                +-+@
                                                |²|@
                                                +-+@@
                                               0x02DD  DOUBLE ACUTE ACCENT
                                                +-+@
                                                |½|@
                                                +-+@@
                                               """;
    }
}
