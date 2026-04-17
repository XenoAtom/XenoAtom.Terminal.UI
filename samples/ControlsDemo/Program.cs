using XenoAtom.Terminal;
using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.ControlsDemo;
using XenoAtom.Terminal.UI.Extensions.Screenshot;
using XenoAtom.Terminal.UI.Styling;

var argsList = args ?? Array.Empty<string>();

if (argsList.Length > 0 && argsList[0].Equals("--export-screenshots", StringComparison.OrdinalIgnoreCase))
{
    var outputDir = Path.Combine(Directory.GetCurrentDirectory(), "site", "img", "controls");
    var width = 120;
    var maxHeight = 40;
    var scheme = ColorScheme.ElderberryDarkSoft;

    for (var i = 1; i < argsList.Length; i++)
    {
        var a = argsList[i];
        if (a.Equals("--out", StringComparison.OrdinalIgnoreCase) && i + 1 < argsList.Length)
        {
            outputDir = argsList[++i];
            continue;
        }

        if (a.Equals("--width", StringComparison.OrdinalIgnoreCase) && i + 1 < argsList.Length && int.TryParse(argsList[++i], out var w))
        {
            width = w;
            continue;
        }

        if (a.Equals("--max-height", StringComparison.OrdinalIgnoreCase) && i + 1 < argsList.Length && int.TryParse(argsList[++i], out var h))
        {
            maxHeight = h;
            continue;
        }

        if (a.Equals("--scheme", StringComparison.OrdinalIgnoreCase) && i + 1 < argsList.Length)
        {
            var name = argsList[++i];
            var schemes = ColorScheme.GetPredefinedSchemes();
            for (var s = 0; s < schemes.Count; s++)
            {
                if (string.Equals(schemes[s].Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    scheme = schemes[s];
                    break;
                }
            }
            continue;
        }
    }

    var count = ScreenshotExport.ExportAll(outputDir, width, maxHeight, scheme);
    Console.WriteLine($"Exported {count} screenshot(s) to {outputDir}");
    return;
}

using var session = Terminal.Open();
var root = ControlsDemoApp.Build(out var onUpdate);
root.RegisterClipboardScreenshotCommand();
Terminal.Run(root, onUpdate);
