using XenoAtom.Ansi;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.ControlsDemo;

internal static class DemoThemes
{
    public static Theme Dark { get; } = Theme.Default;

    public static Theme Light { get; } = new Theme
    {
        Foreground = AnsiColor.Rgb(0x0F, 0x17, 0x2A), // slate-900
        Background = null,
        Surface = AnsiColor.Rgb(0xF1, 0xF5, 0xF9), // slate-100
        SurfaceAlt = AnsiColor.Rgb(0xE2, 0xE8, 0xF0), // slate-200
        Border = AnsiColor.Rgb(0x94, 0xA3, 0xB8), // slate-400
        FocusBorder = AnsiColor.Rgb(0x25, 0x63, 0xEB), // blue-600
        Accent = AnsiColor.Rgb(0x7C, 0x3A, 0xED), // violet-600
        Selection = AnsiColor.Rgb(0x93, 0xC5, 0xFD), // blue-300
        Disabled = AnsiColor.Rgb(0x64, 0x74, 0x8B), // slate-500
        Primary = AnsiColor.Rgb(0x7C, 0x3A, 0xED),
        Success = AnsiColor.Rgb(0x05, 0x9A, 0x69),
        Warning = AnsiColor.Rgb(0xD9, 0x77, 0x06),
        Error = AnsiColor.Rgb(0xE1, 0x1D, 0x48),
        Muted = AnsiColor.Rgb(0x47, 0x55, 0x69),
        Lines = LineGlyphs.Single,
        ScrollBars = ScrollBarGlyphs.Default,
    };
}

