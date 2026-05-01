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

        return new NerdFontModel(version, date, entries, familyNames.ToList(), BuildRanges(entries));
    }

    private static void GenerateFiles(string outputDir, NerdFontModel model)
    {
        foreach (var file in Directory.GetFiles(outputDir, "NerdFont*.gen.cs", SearchOption.TopDirectoryOnly))
        {
            File.Delete(file);
        }

        File.WriteAllText(Path.Combine(outputDir, "NerdFont.gen.cs"), GenerateBaseFile(model), Utf8NoBom);
        File.WriteAllText(Path.Combine(outputDir, "NerdFont.Lookup.gen.cs"), GenerateLookupFile(model), Utf8NoBom);
        File.WriteAllText(Path.Combine(outputDir, "NerdFont.Width.gen.cs"), GenerateWidthSupportFile(model), Utf8NoBom);

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
        sb.AppendLine("using System.Collections.Generic;");
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
        sb.AppendLine("    /// <summary>");
        sb.AppendLine("    /// Gets the original Nerd Fonts glyph names available for lookup.");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine("    public static IReadOnlyList<string> Names => NerdFontLookup.Names;");
        sb.AppendLine();
        sb.AppendLine("    /// <summary>");
        sb.AppendLine("    /// Gets the glyph rune for an original Nerd Fonts glyph name.");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine("    /// <param name=\"name\">The original Nerd Fonts glyph name, for example <c>cod-account</c>.</param>");
        sb.AppendLine("    /// <param name=\"rune\">When this method returns, contains the glyph rune if <paramref name=\"name\"/> was found; otherwise, the default value.</param>");
        sb.AppendLine("    /// <returns><c>true</c> if <paramref name=\"name\"/> was found; otherwise, <c>false</c>.</returns>");
        sb.AppendLine("    public static bool TryGetRune(string name, out Rune rune)");
        sb.AppendLine("    {");
        sb.AppendLine("        if (name is null)");
        sb.AppendLine("        {");
        sb.AppendLine("            rune = default;");
        sb.AppendLine("            return false;");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        return NerdFontLookup.TryGetRune(name, out rune);");
        sb.AppendLine("    }");
        sb.AppendLine("}");
        return sb.ToString();
    }

    private static string GenerateLookupFile(NerdFontModel model)
    {
        var sb = new StringBuilder();
        AppendHeader(sb);
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("using System.Text;");
        sb.AppendLine();
        sb.AppendLine("namespace XenoAtom.Terminal.UI;");
        sb.AppendLine();
        sb.AppendLine("internal static class NerdFontLookup");
        sb.AppendLine("{");
        sb.AppendLine("    internal static IReadOnlyList<string> Names => NamesHolder.Names;");
        sb.AppendLine();
        sb.AppendLine("    internal static bool TryGetRune(string name, out Rune rune)");
        sb.AppendLine("        => RunesByNameHolder.RunesByName.TryGetValue(name, out rune);");
        sb.AppendLine();
        sb.AppendLine("    private static class NamesHolder");
        sb.AppendLine("    {");
        sb.AppendLine("        internal static readonly IReadOnlyList<string> Names = Array.AsReadOnly<string>(");
        sb.AppendLine("        [");

        foreach (var entry in model.Entries)
        {
            sb.Append("            ");
            sb.Append(ToCSharpStringLiteral(entry.OriginalName));
            sb.AppendLine(",");
        }

        sb.AppendLine("        ]);");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    private static class RunesByNameHolder");
        sb.AppendLine("    {");
        sb.Append("        internal static readonly Dictionary<string, Rune> RunesByName = new(");
        sb.Append(model.Entries.Count.ToString(CultureInfo.InvariantCulture));
        sb.AppendLine(", StringComparer.Ordinal)");
        sb.AppendLine("        {");

        foreach (var entry in model.Entries)
        {
            sb.Append("            [");
            sb.Append(ToCSharpStringLiteral(entry.OriginalName));
            sb.Append("] = new(0x");
            sb.Append(entry.CodePoint.ToString("X", CultureInfo.InvariantCulture));
            sb.AppendLine("),");
        }

        sb.AppendLine("        };");
        sb.AppendLine("    }");
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

    private static string GenerateWidthSupportFile(NerdFontModel model)
    {
        var sb = new StringBuilder();
        AppendHeader(sb);
        sb.AppendLine("using System.Text;");
        sb.AppendLine();
        sb.AppendLine("namespace XenoAtom.Terminal.UI;");
        sb.AppendLine();
        sb.AppendLine("public static partial class NerdFont");
        sb.AppendLine("{");
        sb.AppendLine("    internal static bool IsWideRuneCandidate(Rune rune)");
        sb.AppendLine("    {");
        sb.AppendLine("        var value = (uint)rune.Value;");

        foreach (var bucket in model.Ranges.GroupBy(static x => GetRangeBucket(x.Start)))
        {
            sb.Append("        if (value < 0x");
            sb.Append(GetBucketLimit(bucket.Key).ToString("X", CultureInfo.InvariantCulture));
            sb.AppendLine("u)");
            sb.AppendLine("        {");
            sb.Append("            return value is ");
            AppendRangePattern(sb, bucket.ToList());
            sb.AppendLine(";");
            sb.AppendLine("        }");
        }

        sb.AppendLine();
        sb.AppendLine("        return false;");
        sb.AppendLine("    }");
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

    private static string ToCSharpStringLiteral(string text)
    {
        var sb = new StringBuilder(text.Length + 2);
        sb.Append('\"');

        foreach (var ch in text)
        {
            switch (ch)
            {
                case '\\':
                    sb.Append("\\\\");
                    break;
                case '\"':
                    sb.Append("\\\"");
                    break;
                case '\0':
                    sb.Append("\\0");
                    break;
                case '\a':
                    sb.Append("\\a");
                    break;
                case '\b':
                    sb.Append("\\b");
                    break;
                case '\f':
                    sb.Append("\\f");
                    break;
                case '\n':
                    sb.Append("\\n");
                    break;
                case '\r':
                    sb.Append("\\r");
                    break;
                case '\t':
                    sb.Append("\\t");
                    break;
                case '\v':
                    sb.Append("\\v");
                    break;
                default:
                    if (char.IsControl(ch))
                    {
                        sb.Append("\\u");
                        sb.Append(((int)ch).ToString("X4", CultureInfo.InvariantCulture));
                    }
                    else
                    {
                        sb.Append(ch);
                    }
                    break;
            }
        }

        sb.Append('\"');
        return sb.ToString();
    }

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

    private static List<CodePointRange> BuildRanges(List<GlyphEntry> entries)
    {
        var codePoints = entries
            .Select(static x => x.CodePoint)
            .Distinct()
            .OrderBy(static x => x)
            .ToList();

        var ranges = new List<CodePointRange>();
        if (codePoints.Count == 0)
        {
            return ranges;
        }

        var start = codePoints[0];
        var end = start;

        for (var i = 1; i < codePoints.Count; i++)
        {
            var codePoint = codePoints[i];
            if (codePoint == end + 1)
            {
                end = codePoint;
                continue;
            }

            ranges.Add(new CodePointRange(start, end));
            start = end = codePoint;
        }

        ranges.Add(new CodePointRange(start, end));
        return ranges;
    }

    private static int GetRangeBucket(int codePoint)
        => codePoint switch
        {
            < 0xE000 => 0,
            < 0xF000 => 1,
            < 0x10000 => 2,
            _ => 3,
        };

    private static int GetBucketLimit(int bucket)
        => bucket switch
        {
            0 => 0xE000,
            1 => 0xF000,
            2 => 0x10000,
            _ => 0x110000,
        };

    private static void AppendRangePattern(StringBuilder sb, List<CodePointRange> ranges)
    {
        for (var i = 0; i < ranges.Count; i++)
        {
            if (i > 0)
            {
                sb.Append(" or ");
            }

            var range = ranges[i];
            if (range.Start == range.End)
            {
                sb.Append("0x");
                sb.Append(range.Start.ToString("X", CultureInfo.InvariantCulture));
                sb.Append('u');
            }
            else
            {
                sb.Append(">= 0x");
                sb.Append(range.Start.ToString("X", CultureInfo.InvariantCulture));
                sb.Append("u and <= 0x");
                sb.Append(range.End.ToString("X", CultureInfo.InvariantCulture));
                sb.Append('u');
            }
        }
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

    private sealed record NerdFontModel(string Version, string Date, List<GlyphEntry> Entries, List<string> Families, List<CodePointRange> Ranges);

    private sealed record GlyphEntry(string OriginalName, string FamilyName, string PropertyName, int CodePoint);

    private sealed record CodePointRange(int Start, int End);
}
