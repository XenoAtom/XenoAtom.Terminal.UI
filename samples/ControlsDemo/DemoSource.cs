using System.Runtime.CompilerServices;

namespace XenoAtom.Terminal.UI.ControlsDemo;

internal static class DemoSource
{
    public static string Get([CallerFilePath] string? sourcePath = null) => sourcePath ?? string.Empty;
}

