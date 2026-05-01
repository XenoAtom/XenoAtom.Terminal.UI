using System.Linq;
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

    [TestMethod]
    public void NerdFont_TryGetRune_Returns_Rune_For_Original_Name()
    {
        Assert.IsTrue(NerdFont.TryGetRune("cod-account", out var rune));
        Assert.AreEqual(NerdFont.CodAccount, rune);

        Assert.IsTrue(NerdFont.TryGetRune("md-language_csharp", out rune));
        Assert.AreEqual(NerdFont.MdLanguageCsharp, rune);
    }

    [TestMethod]
    public void NerdFont_TryGetRune_Returns_False_For_Unknown_Name()
    {
        Assert.IsFalse(NerdFont.TryGetRune("CodAccount", out var rune));
        Assert.AreEqual(default, rune);

        Assert.IsFalse(NerdFont.TryGetRune("missing-glyph", out rune));
        Assert.AreEqual(default, rune);
    }

    [TestMethod]
    public void NerdFont_Names_Returns_Original_Names_For_Lookup()
    {
        Assert.IsTrue(NerdFont.Names.Contains("cod-account"));
        Assert.IsTrue(NerdFont.Names.Contains("md-language_csharp"));
        Assert.IsFalse(NerdFont.Names.Contains("CodAccount"));
    }
}
