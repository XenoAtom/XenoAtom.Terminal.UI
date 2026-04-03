using SkiaSharp;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Extensions.Screenshot;
using XenoAtom.Terminal.UI.Rendering;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class CellBufferImageExporterTests
{
    [TestMethod]
    public void ImageExporter_Emits_Png_With_Content()
    {
        var root = new Border(new TextBlock("Hello"))
            .Style(BorderStyle.Single)
            .Padding(1);

        root.SetStyle(Theme.Default);

        var buffer = VisualSnapshotRenderer.Render(root, width: 20, maxHeight: 10);
        var bytes = CellBufferImageExporter.Export(buffer, ScreenshotImageFormat.Png, new CellBufferImageExportOptions
        {
            AutoCrop = true,
            Padding = new Geometry.Thickness(1),
        });

        CollectionAssert.AreEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47 }, bytes[..4]);

        using var bitmap = SKBitmap.Decode(bytes);
        Assert.IsNotNull(bitmap);
        Assert.IsTrue(bitmap.Width > 0);
        Assert.IsTrue(bitmap.Height > 0);
    }

    [TestMethod]
    public void ImageExporter_AutoCrop_Reduces_Image_Size()
    {
        var buffer = new CellBuffer(40, 10);
        buffer.Clear(Style.None);
        buffer.WriteText(10, 4, "X", Style.None.WithForeground(Color.Rgb(255, 0, 0)));

        var full = CellBufferImageExporter.Export(buffer, ScreenshotImageFormat.Png, new CellBufferImageExportOptions
        {
            AutoCrop = false,
            FillBackground = false,
        });

        var cropped = CellBufferImageExporter.Export(buffer, ScreenshotImageFormat.Png, new CellBufferImageExportOptions
        {
            AutoCrop = true,
            FillBackground = false,
        });

        using var fullBitmap = SKBitmap.Decode(full);
        using var croppedBitmap = SKBitmap.Decode(cropped);

        Assert.IsNotNull(fullBitmap);
        Assert.IsNotNull(croppedBitmap);
        Assert.IsTrue(croppedBitmap.Width < fullBitmap.Width);
        Assert.IsTrue(croppedBitmap.Height < fullBitmap.Height);
    }

    [TestMethod]
    public void ImageExporter_Uses_Configured_Cell_Size()
    {
        var buffer = new CellBuffer(2, 1);
        buffer.Clear(Style.None);
        buffer.WriteText(0, 0, "OK", Style.None.WithForeground(Color.Rgb(255, 255, 255)));

        var bytes = CellBufferImageExporter.Export(buffer, ScreenshotImageFormat.Png, new CellBufferImageExportOptions
        {
            AutoCrop = false,
            FillBackground = false,
            Font = new ScreenshotFontOptions
            {
                SizePx = 16,
                CellWidthPx = 7,
                CellHeightPx = 13,
            },
        });

        using var bitmap = SKBitmap.Decode(bytes);
        Assert.IsNotNull(bitmap);
        Assert.AreEqual(14, bitmap.Width);
        Assert.AreEqual(13, bitmap.Height);
    }

    [TestMethod]
    public void SnapshotImageRenderer_Renders_Png_For_App_Dependent_Path()
    {
        var root = new Dialog
        {
            Title = "Export",
            Content = new TextBlock("Hello"),
        }
        .Width(20)
        .Height(6);

        var bytes = TerminalAppSnapshotImageRenderer.Render(
            root,
            ScreenshotImageFormat.Png,
            width: 30,
            height: 12,
            theme: Theme.Default,
            options: new CellBufferImageExportOptions
            {
                AutoCrop = true,
                Padding = new Geometry.Thickness(1),
            });

        using var bitmap = SKBitmap.Decode(bytes);
        Assert.IsNotNull(bitmap);
        Assert.IsTrue(bitmap.Width > 0);
        Assert.IsTrue(bitmap.Height > 0);
    }

    [TestMethod]
    public void ImageExporter_FallbackCodepoint_Skips_Emoji_Modifier_Codepoints()
    {
        Assert.AreEqual(0x1F5C3, CellBufferImageExporter.GetFallbackCodepointForText("🗃️"));
        Assert.AreEqual(0x1F3C3, CellBufferImageExporter.GetFallbackCodepointForText("🏃‍♀️"));
        Assert.AreEqual('A', CellBufferImageExporter.GetFallbackCodepointForText("A"));
    }
}
