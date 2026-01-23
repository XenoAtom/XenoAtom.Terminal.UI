using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;

namespace XenoAtom.Terminal.UI.ControlsDemo.Demos;

[Demo("ColorPicker", "Input", Description = "Pick colors using sliders, hex input, and palettes.")]
public sealed class ColorPickerDemo : ControlsDemoBase
{
    public ColorPickerDemo() : base(DemoSource.Get())
    {
    }

    public override Visual Build(DemoContext context)
    {
        var rgba = new State<Color>(Color.RgbA(0x50, 0x9A, 0xF6, 0x88));
        var rgb = new State<Color>(Color.Rgb(0xCA, 0x64, 0xF3));

        return new VStack(
                DemoUi.Hint("Edit channels, type a hex value, or click a palette swatch."),
                new HStack(
                        new Group()
                            .TopLeftText("RGBA")
                            .Content(
                                new ColorPicker()
                                    .AllowAlpha(true)
                                    .ShowPalette(true)
                                    .Value(rgba)
                            ),
                        new Group()
                            .TopLeftText("RGB (no alpha)")
                            .Content(
                                new ColorPicker()
                                    .AllowAlpha(false)
                                    .ShowPalette(true)
                                    .Value(rgb)
                            )
                    )
                    .Spacing(2),
                new TextBlock(() =>
                {
                    var c = rgba.Value;
                    var rgbValue = c.ToRgb();
                    return $"RGBA: #{rgbValue.R:X2}{rgbValue.G:X2}{rgbValue.B:X2}{c.A:X2}";
                })
            )
            .Spacing(1);
    }
}

