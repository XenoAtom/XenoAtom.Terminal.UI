using XenoAtom.Ansi;
using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Figlet;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.ControlsDemo.Demos;

[Demo("TextFiglet", "Visualization", Description = "Big banner text rendered with FIGlet fonts.")]
public sealed class TextFigletDemo : ControlsDemoBase
{
    public TextFigletDemo() : base(DemoSource.Get())
    {
    }

    public override Visual Build(DemoContext context)
    {
        _ = context;

        var text = new State<string?>("XenoAtom");
        var spacing = new State<int>(1);
        var font = new State<FigletFont>(FigletFont.Standard);
        var fonts = FigletFont.GetPredefinedFonts();
        var selectedFontIndex = new State<int>(0);
        return new VStack(
                DemoUi.Hint("TextFiglet renders multi-line banner text. Bind Text to a State to update live."),
                new HStack(
                        new TextBlock("Text:"),
                        new TextBox().Text(text).MinWidth(20).MaxWidth(20),
                        new TextBlock("Spacing:"),
                        new Slider<int>().Minimum(1).Maximum(8).Value(spacing).MinWidth(16).MaxWidth(16),
                        new TextBlock(() => $"{spacing.Value}"),
                        new Select<FigletFont>().Items(fonts).SelectedIndex(selectedFontIndex)
                        )
                    .Spacing(1).VerticalAlignment(VerticalAlignment.Top),
                new Border(
                    new TextFiglet()
                        .Text(text)
                        .Font(() => fonts[selectedFontIndex.Value])
                        .LetterSpacing(spacing)
                        .TextAlignment(TextAlignment.Left)
                        .Style(TextFigletStyle.Default with
                        {
                            TextStyle = CellStyle.None.WithForeground(AnsiColor.Rgb(0x77, 0xB1, 0xFB)),
                        }))
                { Padding = new Thickness(1) })
            .Spacing(1);
    }
}
