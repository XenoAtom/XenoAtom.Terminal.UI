using System.Globalization;
using System.Text;
using XenoAtom.Terminal.UI.ControlsDemo;
using XenoAtom.Terminal.UI.Rendering;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.ControlsDemo;

internal static class ScreenshotExport
{
    public static int ExportAll(string outputDirectory, int width, int maxHeight, ColorScheme scheme)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxHeight);
        ArgumentNullException.ThrowIfNull(scheme);

        var theme = Theme.FromScheme(scheme);
        var demos = DemoRegistry.Load();

        var schemeSlug = Slugify(scheme.Name ?? "theme");
        var outputRoot = Path.Combine(outputDirectory, schemeSlug);
        Directory.CreateDirectory(outputRoot);

        var runtime = new DemoRuntime();

        var taken = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var written = 0;

        for (var i = 0; i < demos.Count; i++)
        {
            var demo = demos[i];
            var id = demo.Metadata.Id;
            if (string.IsNullOrWhiteSpace(id))
            {
                continue;
            }

            // Skip internal pages that don't represent a control screenshot.
            var typeName = GetTypeNameFromId(id);
            if (string.Equals(typeName, "WelcomeDemo", StringComparison.Ordinal) ||
                string.Equals(typeName, "ControlsDemoApp", StringComparison.Ordinal))
            {
                continue;
            }

            var slug = Slugify(RemoveDemoSuffix(typeName));
            slug = EnsureUniqueSlug(slug, taken);

            var demoContext = new DemoContext
            {
                Log = _ => { },
                NavigateToDemoId = _ => { },
                Runtime = runtime,
                Theme = theme,
                ToastHost = null,
            };

            // Render a stable snapshot.
            runtime.Advance();
            var root = demo.Build(demoContext);

            var buffer = VisualSnapshotRenderer.Render(root, width: width, maxHeight: maxHeight, theme: theme);
            var svg = CellBufferSvgExporter.Export(buffer, new CellBufferSvgExportOptions
            {
                AutoCrop = true,
                Padding = new Geometry.Thickness(1),
                FillBackground = true,
                CellWidthPx = 9,
                CellHeightPx = 18,
            });

            var path = Path.Combine(outputRoot, slug + ".svg");
            File.WriteAllText(path, svg, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            written++;
        }

        return written;
    }

    private static string EnsureUniqueSlug(string slug, HashSet<string> taken)
    {
        if (taken.Add(slug))
        {
            return slug;
        }

        for (var i = 2; i < 10_000; i++)
        {
            var next = $"{slug}-{i}";
            if (taken.Add(next))
            {
                return next;
            }
        }

        throw new InvalidOperationException("Unable to generate a unique screenshot slug.");
    }

    private static string RemoveDemoSuffix(string typeName)
        => typeName.EndsWith("Demo", StringComparison.Ordinal) ? typeName[..^"Demo".Length] : typeName;

    private static string GetTypeNameFromId(string id)
    {
        var lastDot = id.LastIndexOf('.');
        return lastDot >= 0 ? id[(lastDot + 1)..] : id;
    }

    private static string Slugify(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return "demo";
        }

        // Convert PascalCase / words to kebab-case, keep ASCII for file names.
        var sb = new StringBuilder(text.Length + 8);

        var prevWasDash = false;
        for (var i = 0; i < text.Length; i++)
        {
            var ch = text[i];

            if (ch is ' ' or '_' or '-' or '.' or '/')
            {
                if (!prevWasDash)
                {
                    sb.Append('-');
                    prevWasDash = true;
                }
                continue;
            }

            if (ch is >= 'A' and <= 'Z')
            {
                if (i > 0 && !prevWasDash)
                {
                    var prev = text[i - 1];
                    if (prev is >= 'a' and <= 'z' or >= '0' and <= '9')
                    {
                        sb.Append('-');
                    }
                }
                sb.Append((char)(ch + 32));
                prevWasDash = false;
                continue;
            }

            if (ch is >= 'a' and <= 'z' or >= '0' and <= '9')
            {
                sb.Append(ch);
                prevWasDash = false;
                continue;
            }

            // Drop non-ASCII characters to keep file names stable across shells.
        }

        var result = sb.ToString().Trim('-');
        return result.Length == 0 ? "demo" : result;
    }
}

