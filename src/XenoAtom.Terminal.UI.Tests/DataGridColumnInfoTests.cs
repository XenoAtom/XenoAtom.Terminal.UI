// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.DataGrid;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class DataGridColumnInfoTests
{
    [TestMethod]
    public void TypedColumnInfo_FromReadOnlyAccessor_PropagatesReadOnlyAndType()
    {
        static int Getter(object _) => 123;

        var accessor = new BindingAccessor<int>("Value", Getter, setter: null);

        DataGridColumnInfo<int> typed = accessor;
        DataGridColumnInfo untyped = typed;

        Assert.AreEqual("Value", typed.Key);
        Assert.AreEqual("Value", typed.HeaderText);
        Assert.IsTrue(typed.ReadOnly);
        Assert.AreSame(accessor, typed.Accessor);

        Assert.AreEqual("Value", untyped.Key);
        Assert.AreEqual("Value", untyped.HeaderText);
        Assert.AreEqual(typeof(int), untyped.ValueType);
        Assert.IsTrue(untyped.ReadOnly);
        Assert.AreSame(accessor, untyped.Accessor);
    }

    [TestMethod]
    public void ListDocument_AddColumn_FromAccessor_UsesNameDefaults()
    {
        static int Getter(object _) => 0;

        var doc = new DataGridListDocument<object>();
        doc.AddColumn(new BindingAccessor<int>("Count", Getter, (o, v) => { }));

        Assert.HasCount(1, doc.Columns);
        Assert.AreEqual("Count", doc.Columns[0].Key);
        Assert.AreEqual("Count", doc.Columns[0].HeaderText);
        Assert.AreEqual(typeof(int), doc.Columns[0].ValueType);
    }
}
