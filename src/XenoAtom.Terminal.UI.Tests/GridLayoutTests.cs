// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Layout;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class GridLayoutTests
{
    [TestMethod]
    public void Fixed_And_Star_Columns_Arrange_Correctly()
    {
        var grid = new Grid();
        grid.ColumnDefinitions.AddRange(
            new ColumnDefinition { Width = GridLength.Fixed(4) },
            new ColumnDefinition { Width = GridLength.Star(1) });
        grid.RowDefinitions.AddRange(new RowDefinition { Height = GridLength.Fixed(1) });

        var a = new FillVisual(new Size(1, 1));
        var b = new FillVisual(new Size(1, 1));

        grid.Cell(a, 0, 0);
        grid.Cell(b, 0, 1);

        grid.Measure(new Size(10, 1));
        grid.Arrange(new Rectangle(0, 0, 10, 1));

        Assert.AreEqual(new Rectangle(0, 0, 4, 1), a.Bounds);
        Assert.AreEqual(new Rectangle(4, 0, 6, 1), b.Bounds);
    }

    [TestMethod]
    public void Auto_Column_Uses_Child_Desired_Width()
    {
        var grid = new Grid();
        grid.ColumnDefinitions.AddRange(
            new ColumnDefinition { Width = GridLength.Auto },
            new ColumnDefinition { Width = GridLength.Star(1) });
        grid.RowDefinitions.AddRange(new RowDefinition { Height = GridLength.Fixed(1) });

        var a = new FillVisual(new Size(5, 1));
        var b = new FillVisual(new Size(1, 1));

        grid.Cell(a, 0, 0);
        grid.Cell(b, 0, 1);

        grid.Measure(new Size(20, 1));
        grid.Arrange(new Rectangle(0, 0, 20, 1));

        Assert.AreEqual(new Rectangle(0, 0, 5, 1), a.Bounds);
        Assert.AreEqual(new Rectangle(5, 0, 15, 1), b.Bounds);
    }

    [TestMethod]
    public void AutoGrow_Adds_Implicit_Columns()
    {
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star(1) });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Fixed(1) });

        var v = new FillVisual(new Size(1, 1));
        grid.Cell(v, 0, 2);

        grid.Measure(new Size(12, 1));
        grid.Arrange(new Rectangle(0, 0, 12, 1));

        Assert.AreEqual(new Rectangle(8, 0, 4, 1), v.Bounds);
    }

    private sealed class FillVisual : Visual
    {
        private readonly Size _desired;

        public FillVisual(Size desired)
        {
            _desired = desired;
            HorizontalAlignment = HorizontalAlignment.Stretch;
            VerticalAlignment = VerticalAlignment.Stretch;
        }

        protected override SizeHints MeasureCore(in LayoutConstraints constraints)
            => SizeHints.Fixed(constraints.Clamp(_desired));

        protected override void ArrangeCore(in Rectangle finalRect) { }
    }
}
