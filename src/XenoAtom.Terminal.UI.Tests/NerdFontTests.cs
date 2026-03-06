using System.Text;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class NerdFontTests
{
    [TestMethod]
    public void NerdFont_RepresentativeGlyphs_ReturnExpectedRunes()
    {
        Assert.AreEqual(new Rune(0xEB99), NerdFont.CodAccount);
        Assert.AreEqual(new Rune(0xF408), NerdFont.OctMarkGithub);
        Assert.AreEqual(new Rune(0xF031B), NerdFont.MdLanguageCsharp);
        Assert.AreEqual(new Rune(0xE30D), NerdFont.WeatherDaySunny);
    }

    [TestMethod]
    public void NerdFont_Runes_Can_Be_Interpolated_Into_Text()
    {
        var text = $"{NerdFont.CodAccount} {NerdFont.PlBranch} {NerdFont.WeatherDaySunny}";

        StringAssert.Contains(text, NerdFont.CodAccount.ToString());
        StringAssert.Contains(text, NerdFont.PlBranch.ToString());
        StringAssert.Contains(text, NerdFont.WeatherDaySunny.ToString());
    }
}
