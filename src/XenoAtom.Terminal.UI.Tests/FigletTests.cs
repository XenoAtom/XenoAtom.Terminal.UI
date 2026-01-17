// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Text;
using XenoAtom.Terminal;
using XenoAtom.Terminal.Backends;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Figlet;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class FigletTests
{
    [TestMethod]
    public void FigletFont_Parse_Reads_Header_And_Glyphs()
    {
        var flf = CreateMinimalFontFfl(height: 2, width: 3, hardBlank: '$', endMark: '@');

        var font = FigletFont.Parse(flf);
        Assert.AreEqual(2, font.Height);

        Assert.IsTrue(font.TryGetGlyph('A', out var glyph));
        Assert.HasCount(2, glyph);
        Assert.AreEqual("AAA", glyph[0]);
        Assert.AreEqual("AAA", glyph[1]);
    }

    [TestMethod]
    public void FigletFont_RenderLines_Produces_MultiLine_Output()
    {
        var font = FigletFont.CreateBlockFont(height: 3, width: 4);
        var lines = font.RenderLines("Hi", new FigletRenderOptions { LetterSpacing = 1 });

        Assert.HasCount(3, lines);
        StringAssert.Contains(lines[0], "HHHH");
        StringAssert.Contains(lines[0], "iiii");
    }

    [TestMethod]
    public void TextFiglet_Renders_Block_Font()
    {
        var backend = new InMemoryTerminalBackend(new TerminalSize(30, 10));
        using var session = Terminal.Open(backend, new TerminalOptions { ImplicitStartInput = true }, force: true);

        session.Instance.Write(new TextFiglet("OK")
        {
            Font = FigletFont.CreateBlockFont(height: 3, width: 3),
            MinHeight = 3,
            MaxHeight = 3,
        });

        var screen = new AnsiTestScreen(30, 10);
        screen.Apply(backend.GetOutText());
        var rendered = screen.GetText();

        StringAssert.Contains(rendered, "OOO");
        StringAssert.Contains(rendered, "KKK");
    }

    private static string CreateMinimalFontFfl(int height, int width, char hardBlank, char endMark)
    {
        var sb = new StringBuilder(1024);
        sb.Append("flf2a").Append(hardBlank).Append(' ')
            .Append(height).Append(' ')
            .Append(1).Append(' ')
            .Append(width).Append(' ')
            .Append(0).Append(' ')
            .Append(0).Append('\n');

        for (var code = 32; code <= 126; code++)
        {
            var ch = (char)code;
            var fill = ch == ' ' ? ' ' : ch;
            var line = new string(fill, width);
            for (var row = 0; row < height; row++)
            {
                // Use the common double end-mark on the last row.
                sb.Append(line).Append(endMark);
                if (row == height - 1)
                {
                    sb.Append(endMark);
                }
                sb.Append('\n');
            }
        }

        return sb.ToString();
    }
}

