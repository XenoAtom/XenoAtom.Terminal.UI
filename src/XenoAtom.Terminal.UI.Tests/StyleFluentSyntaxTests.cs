// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class StyleFluentSyntaxTests
{
    [TestMethod]
    public void Style_Fluent_Accepts_Direct_Style_Value()
    {
        var button = new Button("Apply")
            .Style(ButtonStyle.Default with { ShowBorder = true });

        Assert.IsTrue(button.GetStyle<ButtonStyle>().ShowBorder);
        Assert.IsTrue(button.HasLocalStyle(ButtonStyle.Key));
    }

    [TestMethod]
    public void Style_Fluent_Accepts_Style_Factory()
    {
        var isDanger = new State<bool>(false);
        var button = new Button("Deploy")
            .Style(() => isDanger.Value
                ? (ButtonStyle.Default with { ShowBorder = true })
                : ButtonStyle.Default);

        Assert.IsFalse(button.GetStyle<ButtonStyle>().ShowBorder);

        isDanger.Value = true;
        Assert.IsTrue(button.GetStyle<ButtonStyle>().ShowBorder);
    }

    [TestMethod]
    public void Style_Fluent_Accepts_State_Implicit_Binding()
    {
        var styleState = new State<ButtonStyle>(ButtonStyle.Default);
        var button = new Button("Apply").Style(styleState);

        Assert.IsFalse(button.GetStyle<ButtonStyle>().ShowBorder);

        styleState.Value = ButtonStyle.Default with { ShowBorder = true };
        Assert.IsTrue(button.GetStyle<ButtonStyle>().ShowBorder);
    }
}
