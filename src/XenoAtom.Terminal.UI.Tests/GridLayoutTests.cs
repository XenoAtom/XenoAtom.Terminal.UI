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

    [TestMethod]
    public void Unbounded_Measure_Treats_Star_Columns_As_Intrinsic()
    {
        var grid = new Grid().ColumnGap(1);
        grid.ColumnDefinitions.AddRange(
            new ColumnDefinition { Width = GridLength.Auto },
            new ColumnDefinition { Width = GridLength.Star(1) });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var auto = new FillVisual(new Size(3, 1));
        var star = new FillVisual(new Size(10, 1));

        grid.Cell(auto, 0, 0);
        grid.Cell(star, 0, 1);

        grid.Measure(LayoutConstraints.Unbounded);

        // Auto (3) + gap (1) + star intrinsic (10)
        Assert.AreEqual(14, grid.DesiredSize.Width);
    }

    [TestMethod]
    public void Auto_Column_Shrinks_To_Preserve_Star_MinWidth()
    {
        var grid = new Grid();
        grid.ColumnDefinitions.AddRange(
            new ColumnDefinition { Width = GridLength.Auto },
            new ColumnDefinition { Width = GridLength.Star(1) });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Fixed(1) });

        var auto = new ShrinkableVisual(desiredWidth: 10, minWidth: 0);
        var star = new ShrinkableVisual(desiredWidth: 3, minWidth: 3);

        grid.Cell(auto, 0, 0);
        grid.Cell(star, 0, 1);

        grid.Measure(new Size(10, 1));
        grid.Arrange(new Rectangle(0, 0, 10, 1));

        Assert.AreEqual(new Rectangle(0, 0, 7, 1), auto.Bounds);
        Assert.AreEqual(new Rectangle(7, 0, 3, 1), star.Bounds);
    }

    [TestMethod]
    public void Star_Text_Column_Shrinks_Before_Auto_Button_Column()
    {
        var grid = new Grid();
        grid.ColumnDefinitions.AddRange(
            new ColumnDefinition { Width = GridLength.Star(1) },
            new ColumnDefinition { Width = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var text = new TextBlock("HelloWorld")
        {
            Wrap = false,
            Trimming = TextTrimming.EndEllipsis,
        };
        var button = new Button("X");

        grid.Cell(text, 0, 0);
        grid.Cell(button, 0, 1);

        grid.Measure(new Size(8, 1));
        grid.Arrange(new Rectangle(0, 0, 8, 1));

        Assert.AreEqual(3, text.Bounds.Width);
        Assert.AreEqual(new Rectangle(3, 0, 5, 1), button.Bounds);
    }

    private sealed class FillVisual : Visual
    {
        private readonly Size _desired;

        public FillVisual(Size desired)
        {
            _desired = desired;
            HorizontalAlignment = Align.Stretch;
            VerticalAlignment = Align.Stretch;
        }

        protected override SizeHints MeasureCore(in LayoutConstraints constraints)
            => SizeHints.Fixed(constraints.Clamp(_desired));

        protected override void ArrangeCore(in Rectangle finalRect) { }
    }

    private sealed class ShrinkableVisual : Visual
    {
        private readonly int _desiredWidth;
        private readonly int _minWidth;

        public ShrinkableVisual(int desiredWidth, int minWidth)
        {
            _desiredWidth = desiredWidth;
            _minWidth = minWidth;
            HorizontalAlignment = Align.Stretch;
            VerticalAlignment = Align.Stretch;
        }

        protected override SizeHints MeasureCore(in LayoutConstraints constraints)
        {
            var min = new Size(Math.Max(0, _minWidth), 1);
            var natural = new Size(Math.Max(min.Width, _desiredWidth), 1);
            var max = new Size(int.MaxValue, 1);
            return SizeHints.Flex(min, natural, max, growX: 0, growY: 0, shrinkX: 1, shrinkY: 0);
        }

        protected override void ArrangeCore(in Rectangle finalRect) { }
    }
}
