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
    public void CellBuffer_DefaultResolver_Treats_NerdFont_Glyph_As_Wide()
    {
        var buffer = new CellBuffer(4, 1);
        buffer.WriteText(0, 0, $"{NerdFont.CodAccount}A".AsSpan(), Style.None);

        var scalars = buffer.UnsafeScalars;
        var cells = buffer.UnsafeCells;

        Assert.AreEqual(NerdFont.CodAccount.Value, scalars[0]);
        Assert.IsTrue(cells[1].IsContinuation);
        Assert.AreEqual('A', scalars[2]);
    }

    [TestMethod]
    public void CellBuffer_Captures_Current_WideRuneResolver()
    {
        CellBuffer buffer;
        using (TerminalTextUtility.PushWideRuneResolver(TerminalWideRuneResolvers.NerdFontMono))
        {
            buffer = new CellBuffer(4, 1);
        }

        buffer.WriteText(0, 0, $"{NerdFont.CodAccount}A".AsSpan(), Style.None);

        var scalars = buffer.UnsafeScalars;
        var cells = buffer.UnsafeCells;

        Assert.AreEqual(NerdFont.CodAccount.Value, scalars[0]);
        Assert.IsFalse(cells[1].IsContinuation);
        Assert.AreEqual('A', scalars[1]);
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
        Assert.AreEqual(3, defaultBlock.DesiredSize.Width);

        var monoBlock = new TextBlock(text);
        using (var monoDriver = new TerminalAppTestDriver(
                   monoBlock,
                   TerminalHostKind.Fullscreen,
                   new TerminalSize(10, 3),
                   new TerminalAppOptions
                   {
                       WideRuneResolver = TerminalWideRuneResolvers.NerdFontMono,
                   }))
        {
            monoDriver.Tick();
        }
        Assert.AreEqual(2, monoBlock.DesiredSize.Width);
    }

    [TestMethod]
    public void TerminalWideRuneResolvers_Expose_NerdFont_Modes()
    {
        Assert.IsTrue(TerminalWideRuneResolvers.Default(NerdFont.CodAccount));
        Assert.IsTrue(TerminalWideRuneResolvers.NerdFontDoubleWidth(NerdFont.CodAccount));
        Assert.IsFalse(TerminalWideRuneResolvers.NerdFontMono(NerdFont.CodAccount));
        Assert.IsTrue(TerminalWideRuneResolvers.NerdFontMono(new Rune(0x1F603)));
    }
}
