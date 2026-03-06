using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace NerdFontCodeGenApp;

internal static class Program
{
    private const string SourceUrl = "https://raw.githubusercontent.com/ryanoasis/nerd-fonts/master/glyphnames.json";

    public static int Main(string[] args)
    {
        try
        {
            return Run(args);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            return 1;
        }
    }

    private static int Run(string[] args)
    {
        _ = args;

        var repoRoot = FindRepoRoot(Environment.CurrentDirectory);
        var artifactsDir = Path.Combine(repoRoot, "artifacts", "nerd-font");
        var jsonPath = Path.Combine(artifactsDir, "glyphnames.json");
        var outputDir = Path.Combine(repoRoot, "src", "XenoAtom.Terminal.UI", "Icons");

        Directory.CreateDirectory(artifactsDir);
        Directory.CreateDirectory(outputDir);

        using var http = CreateHttpClient();
        DownloadGlyphNames(http, jsonPath);

        var model = ParseGlyphNames(jsonPath);
        GenerateFiles(outputDir, model);

        Console.WriteLine($"Generated {model.Entries.Count} Nerd Font glyph properties across {model.Families.Count} family file(s).");
        return 0;
    }

    private static HttpClient CreateHttpClient()
    {
        var http = new HttpClient();
        http.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("XenoAtom.Terminal.UI", "1.0"));
        http.Timeout = TimeSpan.FromSeconds(60);
        return http;
    }

    private static void DownloadGlyphNames(HttpClient http, string jsonPath)
    {
        Console.WriteLine($"Downloading {SourceUrl}");
        using var response = http.GetAsync(SourceUrl, HttpCompletionOption.ResponseHeadersRead).GetAwaiter().GetResult();
        response.EnsureSuccessStatusCode();

        var bytes = response.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult();
        File.WriteAllBytes(jsonPath, bytes);
    }

    private static NerdFontModel ParseGlyphNames(string jsonPath)
    {
        using var stream = File.OpenRead(jsonPath);
        using var document = JsonDocument.Parse(stream);

        var root = document.RootElement;
        var metadata = root.GetProperty("METADATA");
        var version = metadata.TryGetProperty("version", out var versionElement) ? versionElement.GetString() ?? string.Empty : string.Empty;
        var date = metadata.TryGetProperty("date", out var dateElement) ? dateElement.GetString() ?? string.Empty : string.Empty;

        var entries = new List<GlyphEntry>();
        var usedPropertyNames = new HashSet<string>(StringComparer.Ordinal);
        var familyNames = new SortedSet<string>(StringComparer.Ordinal);

        foreach (var property in root.EnumerateObject())
        {
            if (property.NameEquals("METADATA"))
            {
                continue;
            }

            var originalName = property.Name;
            var dashIndex = originalName.IndexOf('-');
            var familyKey = dashIndex >= 0 ? originalName[..dashIndex] : "misc";
            var familyName = ToPascalIdentifier(familyKey);

            var glyph = property.Value;
            var codeHex = glyph.TryGetProperty("code", out var codeElement)
                ? codeElement.GetString() ?? throw new InvalidOperationException($"Missing code for glyph '{originalName}'.")
                : throw new InvalidOperationException($"Missing code for glyph '{originalName}'.");
            var codePoint = int.Parse(codeHex, NumberStyles.HexNumber, CultureInfo.InvariantCulture);

            var propertyName = ToPascalIdentifier(originalName);
            if (string.IsNullOrEmpty(propertyName))
            {
                propertyName = familyName + "Icon";
            }

            if (!usedPropertyNames.Add(propertyName))
            {
                propertyName += "U" + codeHex.ToUpperInvariant();
                if (!usedPropertyNames.Add(propertyName))
                {
                    throw new InvalidOperationException($"Duplicate generated property name '{propertyName}' for glyph '{originalName}'.");
                }
            }

            familyNames.Add(familyName);
            entries.Add(new GlyphEntry(originalName, familyName, propertyName, codePoint));
        }

        entries.Sort(static (a, b) =>
        {
            var cmp = string.Compare(a.FamilyName, b.FamilyName, StringComparison.Ordinal);
            return cmp != 0 ? cmp : string.Compare(a.OriginalName, b.OriginalName, StringComparison.Ordinal);
        });

        return new NerdFontModel(version, date, entries, familyNames.ToList());
    }

    private static void GenerateFiles(string outputDir, NerdFontModel model)
    {
        foreach (var file in Directory.GetFiles(outputDir, "NerdFont*.gen.cs", SearchOption.TopDirectoryOnly))
        {
            File.Delete(file);
        }

        File.WriteAllText(Path.Combine(outputDir, "NerdFont.gen.cs"), GenerateBaseFile(model), Utf8NoBom);

        foreach (var group in model.Entries.GroupBy(static x => x.FamilyName, StringComparer.Ordinal))
        {
            var fileName = $"NerdFont.{group.Key}.gen.cs";
            File.WriteAllText(Path.Combine(outputDir, fileName), GenerateFamilyFile(group.Key, group), Utf8NoBom);
        }
    }

    private static string GenerateBaseFile(NerdFontModel model)
    {
        var sb = new StringBuilder();
        AppendHeader(sb);
        sb.AppendLine("using System.Text;");
        sb.AppendLine();
        sb.AppendLine("namespace XenoAtom.Terminal.UI;");
        sb.AppendLine();
        sb.AppendLine("/// <summary>");
        sb.AppendLine("/// Provides generated Nerd Font glyph runes grouped across multiple partial files.");
        sb.AppendLine("/// </summary>");
        sb.AppendLine("/// <remarks>");
        sb.AppendLine($"/// Generated from {SourceUrl}.");
        if (!string.IsNullOrEmpty(model.Version))
        {
            sb.AppendLine($"/// Source version: {EscapeXml(model.Version)}.");
        }
        if (!string.IsNullOrEmpty(model.Date))
        {
            sb.AppendLine($"/// Source date: {EscapeXml(model.Date)}.");
        }
        sb.AppendLine("/// These properties return <see cref=\"Rune\"/> values so they can be used directly in string interpolation and text rendering APIs.");
        sb.AppendLine("/// </remarks>");
        sb.AppendLine("public static partial class NerdFont");
        sb.AppendLine("{");
        sb.AppendLine("}");
        return sb.ToString();
    }

    private static string GenerateFamilyFile(string familyName, IEnumerable<GlyphEntry> entries)
    {
        var sb = new StringBuilder();
        AppendHeader(sb);
        sb.AppendLine("using System.Text;");
        sb.AppendLine();
        sb.AppendLine("namespace XenoAtom.Terminal.UI;");
        sb.AppendLine();
        sb.AppendLine("public static partial class NerdFont");
        sb.AppendLine("{");

        foreach (var entry in entries)
        {
            sb.AppendLine("    /// <summary>");
            sb.Append("    /// ");
            sb.Append(EscapeXml(entry.OriginalName));
            sb.AppendLine(".");
            sb.AppendLine("    /// </summary>");
            sb.Append("    public static Rune ");
            sb.Append(entry.PropertyName);
            sb.Append(" => new(0x");
            sb.Append(entry.CodePoint.ToString("X", CultureInfo.InvariantCulture));
            sb.AppendLine(");");
            sb.AppendLine();
        }

        sb.AppendLine("}");
        return sb.ToString();
    }

    private static void AppendHeader(StringBuilder sb)
    {
        sb.AppendLine("// <auto-generated />");
        sb.AppendLine();
    }

    private static string EscapeXml(string text)
        => text
            .Replace("&", "&amp;", StringComparison.Ordinal)
            .Replace("<", "&lt;", StringComparison.Ordinal)
            .Replace(">", "&gt;", StringComparison.Ordinal);

    private static string ToPascalIdentifier(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var sb = new StringBuilder(value.Length + 8);
        var capitalize = true;

        for (var i = 0; i < value.Length; i++)
        {
            var ch = value[i];
            if (!char.IsLetterOrDigit(ch))
            {
                capitalize = true;
                continue;
            }

            if (sb.Length == 0 && char.IsDigit(ch))
            {
                sb.Append('N');
                capitalize = true;
            }

            if (capitalize)
            {
                sb.Append(char.ToUpperInvariant(ch));
                capitalize = false;
            }
            else
            {
                sb.Append(ch);
            }
        }

        return sb.ToString();
    }

    private static string FindRepoRoot(string startDir)
    {
        var dir = new DirectoryInfo(startDir);
        while (dir is not null)
        {
            var slnx = Path.Combine(dir.FullName, "src", "XenoAtom.Terminal.UI.slnx");
            if (File.Exists(slnx))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Unable to locate repo root (expected src/XenoAtom.Terminal.UI.slnx).");
    }

    private static readonly UTF8Encoding Utf8NoBom = new(false);

    private sealed record NerdFontModel(string Version, string Date, List<GlyphEntry> Entries, List<string> Families);

    private sealed record GlyphEntry(string OriginalName, string FamilyName, string PropertyName, int CodePoint);
}
