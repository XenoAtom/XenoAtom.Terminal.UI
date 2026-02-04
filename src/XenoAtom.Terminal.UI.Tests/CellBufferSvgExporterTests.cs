// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Rendering;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class CellBufferSvgExporterTests
{
    [TestMethod]
    public void SvgExporter_Emits_Svg_With_Text()
    {
        var root = new Border(new TextBlock("Hello"))
            .Style(BorderStyle.Single)
            .Padding(1);

        root.SetStyle(Theme.Default);

        var buffer = VisualSnapshotRenderer.Render(root, width: 20, maxHeight: 10);
        var svg = CellBufferSvgExporter.Export(buffer, new CellBufferSvgExportOptions
        {
            AutoCrop = true,
            Padding = new Geometry.Thickness(1),
        });

        StringAssert.Contains(svg, "<svg");
        StringAssert.Contains(svg, "Hello");
        StringAssert.Contains(svg, "</svg>");
    }

    [TestMethod]
    public void SvgExporter_AutoCrop_Reduces_Svg_Size()
    {
        var buffer = new CellBuffer(40, 10);
        buffer.Clear(Style.None);
        buffer.WriteText(10, 4, "X", Style.None.WithForeground(Color.Rgb(255, 0, 0)));

        var full = CellBufferSvgExporter.Export(buffer, new CellBufferSvgExportOptions { AutoCrop = false, FillBackground = false });
        var cropped = CellBufferSvgExporter.Export(buffer, new CellBufferSvgExportOptions { AutoCrop = true, FillBackground = false });

        // Crude sanity check: cropped output should be smaller than the full output.
        // NOTE: MSTest's Assert.IsLessThan signature is (upperBound, value) meaning `value < upperBound`.
        Assert.IsLessThan(full.Length, cropped.Length, $"Expected cropped SVG to be smaller. full={full.Length} cropped={cropped.Length}");
    }

    [TestMethod]
    public void SvgExporter_Resolves_Basic16_Foreground_To_Rgb()
    {
        var buffer = new CellBuffer(3, 1);
        buffer.Clear(Style.None);

        var fg = Color.Basic16(9); // Bright red in the basic palette.
        buffer.WriteText(0, 0, "X", Style.None.WithForeground(fg));

        var svg = CellBufferSvgExporter.Export(buffer, new CellBufferSvgExportOptions { AutoCrop = true, FillBackground = false });

        var rgb = fg.ToRgb();
        var expected = $"fill=\"rgb({rgb.R},{rgb.G},{rgb.B})\"";
        StringAssert.Contains(svg, expected, "Expected the SVG to include an RGB fill color for a Basic16 foreground.");
    }
}
