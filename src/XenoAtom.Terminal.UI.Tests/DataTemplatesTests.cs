// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Templating;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class DataTemplatesTests
{
    private interface IBaseMarker
    {
    }

    private interface IChildMarker : IBaseMarker
    {
    }

    private sealed class DerivedMarker : IChildMarker
    {
    }

    private abstract class BaseClass
    {
    }

    private sealed class DerivedClass : BaseClass
    {
    }

    [TestMethod]
    public void TryResolveForValue_Prefers_ExactMatch()
    {
        var templates = DataTemplates.Default.Derive(builder => builder
            .Register<string>(DataTemplateRole.Display, new((string _, in DataTemplateContext _) => new TextBlock("exact")))
            .Register<object>(DataTemplateRole.Display, new((object _, in DataTemplateContext _) => new TextBlock("object")))
        );

        Assert.IsTrue(templates.TryResolveForValue("hello", DataTemplateRole.Display, out var template, out var resolvedType));
        Assert.AreEqual(typeof(string), resolvedType);

        var visual = template.Create!("hello", new DataTemplateContext(new VStack(), DataTemplateRole.Display, -1, DataTemplateItemState.None));
        Assert.IsInstanceOfType<TextBlock>(visual);
        Assert.AreEqual("exact", ((TextBlock)visual).Text);
    }

    [TestMethod]
    public void TryResolveForValue_Prefers_BaseClass_Over_Object()
    {
        var templates = DataTemplates.Default.Derive(builder => builder
            .Register<BaseClass>(DataTemplateRole.Display, new((BaseClass _, in DataTemplateContext _) => new TextBlock("base")))
            .Register<object>(DataTemplateRole.Display, new((object _, in DataTemplateContext _) => new TextBlock("object")))
        );

        var value = new DerivedClass();
        Assert.IsTrue(templates.TryResolveForValue(value, DataTemplateRole.Display, out var template, out var resolvedType));
        Assert.AreEqual(typeof(BaseClass), resolvedType);

        var visual = template.Create!(value, new DataTemplateContext(new VStack(), DataTemplateRole.Display, -1, DataTemplateItemState.None));
        Assert.IsInstanceOfType<TextBlock>(visual);
        Assert.AreEqual("base", ((TextBlock)visual).Text);
    }

    [TestMethod]
    public void TryResolveForValue_Prefers_MostSpecific_Interface()
    {
        var templates = DataTemplates.Default.Derive(builder => builder
            .Register<IBaseMarker>(DataTemplateRole.Display, new((IBaseMarker _, in DataTemplateContext _) => new TextBlock("base")))
            .Register<IChildMarker>(DataTemplateRole.Display, new((IChildMarker _, in DataTemplateContext _) => new TextBlock("child")))
        );

        var value = new DerivedMarker();
        Assert.IsTrue(templates.TryResolveForValue(value, DataTemplateRole.Display, out var template, out var resolvedType));
        Assert.AreEqual(typeof(IChildMarker), resolvedType);

        var visual = template.Create!(value, new DataTemplateContext(new VStack(), DataTemplateRole.Display, -1, DataTemplateItemState.None));
        Assert.IsInstanceOfType<TextBlock>(visual);
        Assert.AreEqual("child", ((TextBlock)visual).Text);
    }
}
