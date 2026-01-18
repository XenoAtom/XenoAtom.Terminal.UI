// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Reflection;
using XenoAtom.Terminal.UI.Figlet;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class FigletVerifyTests : VerifyBase
{
    [TestMethod]
    [DynamicData(nameof(Fonts), DynamicDataDisplayName = nameof(GetTestMethodDisplayName))]
    public async Task TestFont(FigletFont font)
    {
        Assert.IsNotNull(font.Info?.Name, "Font Name must not be null");
        var lines = font.RenderLines("XenoAtom", new FigletRenderOptions { LetterSpacing = 1 });
        var text = $"Font: {font.Info!.Name}, Author: {font.Info.Author}, Url: {font.Info.Url}\n{string.Join("\n", lines)}";
        await Verify(text).UseMethodName(font.Info?.Name ?? "UnknownFont").IgnoreParameters(nameof(font)).IgnoreParametersForVerified(nameof(font));
    }

    private static IEnumerable<FigletFont> Fonts
    {
        get
        {
            var test = FigletPredefinedFont.Standard;
            var props = typeof(FigletPredefinedFont).GetProperties(BindingFlags.Static | BindingFlags.Public)
                .Where(x => x.PropertyType == typeof(FigletFont));
            foreach (var prop in props)
            {
                Console.WriteLine($"Font {prop.Name}");
                var font = (FigletFont)prop.GetValue(null)!;
                yield return font;
            }
        }
    }

    public static string GetTestMethodDisplayName(MethodInfo methodInfo, object[] data)
    {
        return  $"Font-{((FigletFont)data[0]).Info?.Name ?? "UnknownFont"}";
    }

}
