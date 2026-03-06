using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Geometry;

namespace XenoAtom.Terminal.UI.ControlsDemo.Demos;

[Demo("NerdFont", "Content", Description = "Generated Nerd Font glyph helpers exposed as Rune properties.")]
public sealed class NerdFontDemo : ControlsDemoBase
{
    public NerdFontDemo() : base(DemoSource.Get())
    {
    }

    public override Visual Build(DemoContext context)
    {
        context.AllowPageScrollViewer = true;

        return new VStack(
                DemoUi.Hint("NerdFont exposes generated Rune properties sourced from the official Nerd Fonts glyph list. Use them directly in TextBlock, Markup, and interpolated strings."),
                new Group(new TextBlock("TextBlock interpolation"),
                        new TextBlock($"{NerdFont.CodAccount}  Account    {NerdFont.DevDotnet}  .NET    {NerdFont.FaGithub}  GitHub    {NerdFont.WeatherDaySunny}  Sunny"))
                    .Padding(new Thickness(1)),
                new Group(new TextBlock("Markup colors"),
                        new Markup($"[primary]{NerdFont.MdHome}[/] [bold]Dashboard[/]    [accent]{NerdFont.PlBranch}[/] main    [success]{NerdFont.WeatherDaySunny}[/] clear sky    [warning]{NerdFont.OctAlert}[/] alerts")
                            .Wrap(true))
                    .Padding(new Thickness(1)),
                DemoUi.Title("Families"),
                new WrapHStack(
                        CreateFamilyCard("Cod", $"[accent]{NerdFont.CodAccount}[/] account  [accent]{NerdFont.CodDebug}[/] debug\n[warning]{NerdFont.CodArchive}[/] archive"),
                        CreateFamilyCard("Custom", $"[primary]{NerdFont.CustomFolder}[/] folder  [primary]{NerdFont.CustomFolderOpen}[/] open\n[accent]{NerdFont.CustomFolderGithub}[/] github"),
                        CreateFamilyCard("Dev", $"[primary]{NerdFont.DevDotnet}[/] .NET  [accent]{NerdFont.DevDocker}[/] docker\n[dim]{NerdFont.DevGithub}[/] github"),
                        CreateFamilyCard("Extra", $"[success]{NerdFont.ExtraProgressFullLeft}{NerdFont.ExtraProgressFullMid}{NerdFont.ExtraProgressFullMid}{NerdFont.ExtraProgressFullRight}[/] progress\n[dim]{NerdFont.ExtraProgressEmptyLeft}{NerdFont.ExtraProgressEmptyMid}{NerdFont.ExtraProgressEmptyMid}{NerdFont.ExtraProgressEmptyRight}[/] empty"),
                        CreateFamilyCard("Fa", $"[accent]{NerdFont.FaGithub}[/] github  [primary]{NerdFont.FaTerminal}[/] term\n[warning]{NerdFont.FaFolderOpen}[/] folder"),
                        CreateFamilyCard("Fae", $"[error]{NerdFont.FaeAppleFruit}[/] apple  [primary]{NerdFont.FaeAtom}[/] atom\n[accent]{NerdFont.FaeCherry}[/] cherry"),
                        CreateFamilyCard("Iec", $"[success]{NerdFont.IecPower}[/] power  [warning]{NerdFont.IecTogglePower}[/] toggle\n[dim]{NerdFont.IecSleepMode}[/] sleep"),
                        CreateFamilyCard("Indent", $"[primary]{NerdFont.IndentDottedGuide}[/] dotted guide\n[accent]{NerdFont.IndentLine}[/] line"),
                        CreateFamilyCard("Indentation", $"[primary]{NerdFont.IndentationLine}[/] indentation guide"),
                        CreateFamilyCard("Linux", $"[warning]{NerdFont.LinuxUbuntu}[/] ubuntu  [primary]{NerdFont.LinuxArchlinux}[/] arch\n[accent]{NerdFont.LinuxDocker}[/] docker"),
                        CreateFamilyCard("Md", $"[primary]{NerdFont.MdHome}[/] home  [accent]{NerdFont.MdLanguageCsharp}[/] csharp\n[warning]{NerdFont.MdAlert}[/] alert"),
                        CreateFamilyCard("Oct", $"[dim]{NerdFont.OctMarkGithub}[/] github  [warning]{NerdFont.OctAlert}[/] alert\n[primary]{NerdFont.OctPackage}[/] package"),
                        CreateFamilyCard("Pl", $"[accent]{NerdFont.PlBranch}[/] branch\n[dim]{NerdFont.PlLeftHardDivider}{NerdFont.PlRightHardDivider}[/] powerline"),
                        CreateFamilyCard("Ple", $"[warning]{NerdFont.PleFlameThick}[/] flame  [primary]{NerdFont.PleBackslashSeparator}[/] split\n[accent]{NerdFont.PleBackslashSeparatorRedundant}[/] redundant"),
                        CreateFamilyCard("Pom", $"[success]{NerdFont.PomCleanCode}[/] clean  [accent]{NerdFont.PomPairProgramming}[/] pair\n[warning]{NerdFont.PomLongPause}[/] pause"),
                        CreateFamilyCard("Seti", $"[primary]{NerdFont.SetiFolder}[/] folder  [accent]{NerdFont.SetiGit}[/] git\n[success]{NerdFont.SetiPython}[/] python  [warning]{NerdFont.SetiReact}[/] react"),
                        CreateFamilyCard("Weather", $"[warning]{NerdFont.WeatherDaySunny}[/] sunny  [primary]{NerdFont.WeatherCloud}[/] cloud\n[accent]{NerdFont.WeatherRain}[/] rain"))
                    .Spacing(1))
            .Spacing(1);
    }

    private static Visual CreateFamilyCard(string title, string markup)
        => new Group(new Markup($"[bold]{title}[/]"),
                new Markup(markup)
                    .Wrap(true))
            .Padding(new Thickness(1))
            .MinWidth(28)
            .MaxWidth(28)
            .HorizontalAlignment(Align.Start)
            .VerticalAlignment(Align.Start);
}
