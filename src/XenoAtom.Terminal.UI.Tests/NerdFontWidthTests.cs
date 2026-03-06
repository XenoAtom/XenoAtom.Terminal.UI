using System.Text;
using XenoAtom.Terminal;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Hosting;
using XenoAtom.Terminal.UI.Rendering;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class NerdFontWidthTests
{
    [TestMethod]
    public void CellBuffer_DefaultResolver_Leaves_NerdFont_Glyph_Narrow()
    {
        var buffer = new CellBuffer(4, 1);
        buffer.WriteText(0, 0, $"{NerdFont.CodAccount}A".AsSpan(), Style.None);

        var scalars = buffer.UnsafeScalars;
        var cells = buffer.UnsafeCells;

        Assert.AreEqual(NerdFont.CodAccount.Value, scalars[0]);
        Assert.IsFalse(cells[1].IsContinuation);
        Assert.AreEqual('A', scalars[1]);
    }

    [TestMethod]
    public void CellBuffer_NerdFontDoubleWidth_Uses_Continuation_Cell()
    {
        var buffer = new CellBuffer(4, 1, TerminalWideRuneResolvers.NerdFontDoubleWidth);

        buffer.WriteText(0, 0, $"{NerdFont.CodAccount}A".AsSpan(), Style.None);

        var scalars = buffer.UnsafeScalars;
        var cells = buffer.UnsafeCells;

        Assert.AreEqual(NerdFont.CodAccount.Value, scalars[0]);
        Assert.IsTrue(cells[1].IsContinuation);
        Assert.AreEqual('A', scalars[2]);
    }

    [TestMethod]
    public void TextBlock_Uses_AppWideRuneResolver_When_Configured()
    {
        var text = $"{NerdFont.CodAccount}A";

        var defaultBlock = new TextBlock(text);
        using (var defaultDriver = new TerminalAppTestDriver(defaultBlock, TerminalHostKind.Fullscreen, new TerminalSize(10, 3)))
        {
            defaultDriver.Tick();
        }
        Assert.AreEqual(2, defaultBlock.DesiredSize.Width);

        var wideBlock = new TextBlock(text);
        using (var wideDriver = new TerminalAppTestDriver(
                   wideBlock,
                   TerminalHostKind.Fullscreen,
                   new TerminalSize(10, 3),
                   new TerminalAppOptions
                   {
                       WideRuneResolver = TerminalWideRuneResolvers.NerdFontDoubleWidth,
                   }))
        {
            wideDriver.Tick();
        }
        Assert.AreEqual(3, wideBlock.DesiredSize.Width);
    }

    [TestMethod]
    public void TerminalWideRuneResolvers_Expose_NerdFont_Modes()
    {
        Assert.IsFalse(TerminalWideRuneResolvers.Default(NerdFont.CodAccount));
        Assert.IsFalse(TerminalWideRuneResolvers.EmojiOnly(NerdFont.CodAccount));
        Assert.IsTrue(TerminalWideRuneResolvers.NerdFontDoubleWidth(NerdFont.CodAccount));
        Assert.IsFalse(TerminalWideRuneResolvers.NerdFontMono(NerdFont.CodAccount));
        Assert.IsTrue(TerminalWideRuneResolvers.Default(new Rune(0x1F603)));
        Assert.IsTrue(TerminalWideRuneResolvers.NerdFontMono(new Rune(0x1F603)));
    }
}
