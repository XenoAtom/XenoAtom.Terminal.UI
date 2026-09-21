// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class StyleDefaultForegroundTests
{
    [TestMethod]
    public void Dialog_Surface_Resolves_Foreground_Without_Overriding_Custom_Colors()
    {
        var customColor = Color.Rgb(12, 34, 56);
        var custom = Styling.DialogStyle.Default with { SurfaceStyle = Style.None.WithForeground(customColor) };
        Assert.AreEqual(customColor, custom.ResolveSurfaceStyle(Styling.Theme.Terminal).GetForegroundOrDefault());
        var inherited = Styling.DialogStyle.Default with { SurfaceStyle = Style.None.WithBackground(Color.Basic16(0)) };
        Assert.AreEqual(Styling.Theme.Default.Foreground, inherited.ResolveSurfaceStyle(Styling.Theme.Default).GetForegroundOrDefault());
    }

    [TestMethod]
    public void Explicit_Default_Foreground_Overrides_Underlay_And_Composed_Styles()
    {
        var underlay = Style.None.WithForeground(Color.Basic16(8)).WithBackground(Color.Basic16(4));
        var reset = Style.None.WithForeground(Color.Default);

        Assert.IsTrue(reset.TryGetForeground(out var foreground));
        Assert.AreEqual(Color.Default, foreground);
        Assert.AreNotEqual(Style.None, reset);
        Assert.AreEqual(Color.Default, reset.GetForegroundOrDefault());

        var merged = reset.MergeUnspecified(underlay);
        Assert.AreEqual(Color.Default, merged.GetForegroundOrDefault());
        Assert.AreEqual(Color.Basic16(4), merged.GetBackgroundOrDefault());
        Assert.AreEqual(Color.Default, (underlay | reset).GetForegroundOrDefault());
        Assert.AreEqual(Color.Basic16(8), (reset | underlay).GetForegroundOrDefault());
        Assert.AreEqual(Color.Default, (reset | TextStyle.Bold).GetForegroundOrDefault());

        var cleared = reset.ClearForeground();
        Assert.IsFalse(cleared.TryGetForeground(out _));
        Assert.AreEqual(Color.Basic16(8), cleared.MergeUnspecified(underlay).GetForegroundOrDefault());
        Assert.AreEqual(Style.None, cleared);
    }
}
