using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;

namespace XenoAtom.Terminal.UI.ControlsDemo.Demos;

[Demo("Text inputs", "Input", Description = "TextBox, MaskedInput, and TextArea editing, selection, and clipboard.", Tags = ["TextBox", "MaskedInput", "TextArea", "clipboard"], Order = 0)]
public sealed class TextInputsDemo : ControlsDemoBase
{
    public TextInputsDemo() : base(DemoSource.Get())
    {
    }

    public override Visual Build(DemoContext context)
    {
        var textBox = new TextBox()
            .Placeholder("Type here…")
            .HorizontalAlignment(HorizontalAlignment.Stretch);

        var masked = new MaskedInput()
            .Text("hunter2")
            .RevealMode(MaskedInputRevealMode.WhileFocused)
            .ClipboardMode(MaskedInputClipboardMode.CopyText)
            .Placeholder("Secret…")
            .HorizontalAlignment(HorizontalAlignment.Stretch);

        var area = new TextArea()
            .Text("Line 1\nLine 2\nLine 3")
            .Placeholder("Multi-line…")
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .MinHeight(8)
            .MaxHeight(8);

        var status = new Markup(() =>
        {
            var t1 = textBox.Text ?? string.Empty;
            var t2 = masked.Text ?? string.Empty;
            var t3 = area.Text ?? string.Empty;
            return $"[dim]TextBox:[/] {t1.Length} chars  [dim]|[/]  [dim]MaskedInput:[/] {t2.Length} chars  [dim]|[/]  [dim]TextArea:[/] {t3.Split('\n').Length} lines";
        });

        return new VStack(
                new Group().TopLeftText("TextBox").Padding(1).HorizontalAlignment(HorizontalAlignment.Stretch).Content(textBox),
                new Group().TopLeftText("MaskedInput").Padding(1).HorizontalAlignment(HorizontalAlignment.Stretch).Content(masked),
                new Group().TopLeftText("TextArea").Padding(1).HorizontalAlignment(HorizontalAlignment.Stretch).Content(area),
                status)
            .Spacing(1)
            .HorizontalAlignment(HorizontalAlignment.Stretch);
    }
}
