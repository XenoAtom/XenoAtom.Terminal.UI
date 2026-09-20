// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Templating;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class DataTemplatesTests
{
    [TestMethod]
    public void Boolean_Display_Recycling_Replaces_Bindings_And_Supports_Literals()
    {
        Assert.IsTrue(DataTemplates.Default.TryResolve<bool>(DataTemplateRole.Display, out var template));
        var context = new DataTemplateContext(new TextBlock(), DataTemplateRole.Display, 0, DataTemplateItemState.None);
        var first = new State<bool>(true);
        var second = new State<bool>(false);
        var checkBox = (CheckBox)template.Display!(new DataTemplateValue<bool>((Binding<bool>)first), context);
        Assert.IsTrue(checkBox.IsChecked);
        Assert.IsNotNull(template.TryUpdate);
        Assert.IsTrue(template.TryUpdate(checkBox, new DataTemplateValue<bool>((Binding<bool>)second), context));
        Assert.IsFalse(checkBox.IsChecked);
        first.Value = false;
        first.Value = true;
        Assert.IsFalse(checkBox.IsChecked, "Recycling must unsubscribe from the previous row.");
        second.Value = true;
        Assert.IsTrue(checkBox.IsChecked);

        Assert.IsTrue(template.TryUpdate(checkBox, new DataTemplateValue<bool>(false), context));
        second.Value = false;
        second.Value = true;
        Assert.IsFalse(checkBox.IsChecked, "A literal value must remove any previous binding.");
        Assert.IsFalse(checkBox.IsEnabled);
        Assert.IsFalse(template.TryUpdate(new TextBlock(), new DataTemplateValue<bool>(true), context));
        Assert.IsTrue(((CheckBox)template.Display(new DataTemplateValue<bool>(true), context)).IsChecked);
    }
}
