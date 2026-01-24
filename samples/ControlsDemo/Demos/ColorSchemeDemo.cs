using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Figlet;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.ControlsDemo.Demos;

[Demo("Color scheme (RootLoops)", "Patterns", Description = "Interactive RootLoops palette generator and local theme preview.", Order = -900)]
public sealed class ColorSchemeDemo : ControlsDemoBase
{
    public ColorSchemeDemo() : base(DemoSource.Get())
    {
    }

    public override Visual Build(DemoContext context)
    {
        _ = context;

        var sugar = new State<int>(7);
        var colors = new State<int>(9);
        var sogginess = new State<int>(4);

        var flavor = new State<RootLoopsFlavor>(RootLoopsFlavor.Classic);
        var fruit = new State<RootLoopsFruit>(RootLoopsFruit.Elderberry);
        var milk = new State<RootLoopsMilkAmount>(RootLoopsMilkAmount.Splash);

        ColorScheme BuildScheme()
            => ColorScheme.Generate(
                sugar.Value,
                colors.Value,
                sogginess.Value,
                flavor.Value,
                fruit.Value,
                milk.Value,
                name: "RootLoops (custom)");

        var banner = new TextFiglet("RootLoops")
            .Font(FigletPredefinedFont.Slant)
            .LetterSpacing(1)
            .TextAlignment(TextAlignment.Left);

        var intro = new Markup("""
            [bold]RootLoops[/] is the palette generator behind the built-in color schemes.
            Experiment with the parameters below to generate a new [bold]ColorScheme[/] and preview a local [bold]Theme[/].
            """).Wrap(true);

        var link = new Link("https://rootloops.sh/", "https://rootloops.sh/").Trimming(TextTrimming.EndEllipsis);

        var recipeGrid = new Grid()
            .Rows(
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = GridLength.Auto })
            .Columns(
                new ColumnDefinition { Width = GridLength.Auto },
                new ColumnDefinition { Width = GridLength.Star(1) },
                new ColumnDefinition { Width = GridLength.Auto });

        recipeGrid
            .Cell("Sugar:", 0, 0)
            .Cell(new Slider<int>().Minimum(0).Maximum(10).Value(sugar).HorizontalAlignment(Align.Stretch), 0, 1)
            .Cell(new TextBlock(() => sugar.Value.ToString()), 0, 2)

            .Cell("Colors:", 1, 0)
            .Cell(new Slider<int>().Minimum(0).Maximum(10).Value(colors).HorizontalAlignment(Align.Stretch), 1, 1)
            .Cell(new TextBlock(() => colors.Value.ToString()), 1, 2)

            .Cell("Sogginess:", 2, 0)
            .Cell(new Slider<int>().Minimum(0).Maximum(10).Value(sogginess).HorizontalAlignment(Align.Stretch), 2, 1)
            .Cell(new TextBlock(() => sogginess.Value.ToString()), 2, 2)

            .Cell("Flavor:", 3, 0)
            .Cell(new EnumSelect<RootLoopsFlavor>().Value(flavor).HorizontalAlignment(Align.Stretch), 3, 1)

            .Cell("Fruit:", 4, 0)
            .Cell(new EnumSelect<RootLoopsFruit>().Value(fruit).HorizontalAlignment(Align.Stretch), 4, 1)

            .Cell("Milk:", 5, 0)
            .Cell(new EnumSelect<RootLoopsMilkAmount>().Value(milk).HorizontalAlignment(Align.Stretch), 5, 1);

        var recipe = new Group()
            .TopLeftText("Recipe")
            .Padding(1)
            .Content(recipeGrid);

        var previewControls = new VStack(
                DemoUi.Title("Preview"),
                new HStack(
                        new Button("Default"),
                        new Button("Primary").Tone(ControlTone.Primary),
                        new Button("Success").Tone(ControlTone.Success),
                        new Button("Warning").Tone(ControlTone.Warning),
                        new Button("Error").Tone(ControlTone.Error))
                    .Spacing(1),
                new Rule(),
                new TextBox("TextBox (editable)").HorizontalAlignment(Align.Stretch),
                new Select<string>().Items("Alpha", "Beta", "Gamma").HorizontalAlignment(Align.Stretch),
                new HStack(
                        new CheckBox().IsChecked(true),
                        "Enabled")
                    .Spacing(1),
                new HStack(
                        new ProgressBar().Value(0.66).HorizontalAlignment(Align.Stretch),
                        new TextBlock("66%"))
                    .Spacing(1))
            .Spacing(1);

        var preview = new Group()
            .TopLeftText("Local theme preview")
            .Padding(1)
            .Content(previewControls);

        var schemePanel = new ComputedVisual(() => BuildSchemePanel(BuildScheme()));

        var themedContent = new HStack(
                new VStack(recipe, preview).Spacing(1),
                schemePanel)
            .Spacing(1);

        var themedArea = new ZStack(
                new Backdrop().Style(BackdropStyle.ThemeBackground),
                new Padder(themedContent).Padding(1))
            .HorizontalAlignment(Align.Stretch);

        // Apply the generated theme locally so it doesn't affect the ControlsDemo UI chrome.
        themedArea.Update(v => v.Style(Theme.FromScheme(BuildScheme())));

        return new VStack(
                banner,
                intro,
                link,
                new Rule(),
                themedArea)
            .Spacing(1);
    }

    private static Visual BuildSchemePanel(ColorScheme scheme)
    {
        var table = new Table()
            .Headers("Name", "Sample", "Value");

        void Add(string name, Color? color)
        {
            table.AddRow(
                new TextBlock(name),
                CreateSwatch(color),
                new Markup($"[dim]{FormatColor(color)}[/]") { Wrap = false });
        }

        Add("Background", scheme.Background);
        Add("Foreground", scheme.Foreground);
        Add("Black", scheme.Black);
        Add("Red", scheme.Red);
        Add("Green", scheme.Green);
        Add("Yellow", scheme.Yellow);
        Add("Blue", scheme.Blue);
        Add("Purple", scheme.Purple);
        Add("Cyan", scheme.Cyan);
        Add("White", scheme.White);
        Add("BrightBlack", scheme.BrightBlack);
        Add("BrightRed", scheme.BrightRed);
        Add("BrightGreen", scheme.BrightGreen);
        Add("BrightYellow", scheme.BrightYellow);
        Add("BrightBlue", scheme.BrightBlue);
        Add("BrightPurple", scheme.BrightPurple);
        Add("BrightCyan", scheme.BrightCyan);
        Add("BrightWhite", scheme.BrightWhite);

        return new Group()
            .TopLeftText($"Scheme: {scheme.Name}")
            .Padding(1)
            .Content(table);
    }

    private static Visual CreateSwatch(Color? color)
    {
        const int width = 10;
        return new TextBlock(new string(' ', width)).Style(TextBlockStyle.Default with { Background = color });
    }

    private static string FormatColor(Color? color)
    {
        if (color is null)
        {
            return "Default";
        }

        var c = color.Value;
        return c.Kind switch
        {
            ColorKind.Default => "Default",
            ColorKind.Basic16 => $"Basic16({c.Index}) {c.ToHexString()}",
            ColorKind.Indexed256 => $"Indexed256({c.Index}) {c.ToHexString()}",
            ColorKind.Rgb => c.ToHexString(),
            ColorKind.RgbA => c.ToHexString(includeAlpha: true),
            _ => c.ToHexString(),
        };
    }
}
