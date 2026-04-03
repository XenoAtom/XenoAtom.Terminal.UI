using System.Buffers;
using System.Reflection;
using System.Text;
using SkiaSharp;
using SkiaSharp.HarfBuzz;
using XenoAtom.Ansi;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Rendering;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.Extensions.Screenshot;

/// <summary>
/// Exports a <see cref="CellBuffer"/> to raster image formats such as PNG and JPEG.
/// </summary>
public static class CellBufferImageExporter
{
    private const string DefaultFontResourceName = "XenoAtom.Terminal.UI.Extensions.Screenshot.Assets.Fonts.CaskaydiaCoveNerdFont-Regular.ttf";
    private static readonly SearchValues<char> EmojiSequenceSkippableCharacters = SearchValues.Create("\u200D\uFE0E\uFE0F\u20E3");

    private static readonly Lazy<byte[]> DefaultFontBytes = new(LoadDefaultFontBytes);

    /// <summary>
    /// Exports the specified <paramref name="buffer"/> to an encoded image payload.
    /// </summary>
    /// <param name="buffer">The source buffer.</param>
    /// <param name="format">The image format to encode.</param>
    /// <param name="options">The export options.</param>
    /// <returns>The encoded image bytes.</returns>
    public static byte[] Export(CellBuffer buffer, ScreenshotImageFormat format = ScreenshotImageFormat.Png, CellBufferImageExportOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        options ??= CellBufferImageExportOptions.Default;

        using var image = RenderImage(buffer, options);
        using var data = EncodeImage(image, format, options);
        return data.ToArray();
    }

    /// <summary>
    /// Exports the specified <paramref name="buffer"/> to an encoded image and writes it to <paramref name="output"/>.
    /// </summary>
    /// <param name="buffer">The source buffer.</param>
    /// <param name="output">The destination stream.</param>
    /// <param name="format">The image format to encode.</param>
    /// <param name="options">The export options.</param>
    public static void Export(CellBuffer buffer, Stream output, ScreenshotImageFormat format = ScreenshotImageFormat.Png, CellBufferImageExportOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        ArgumentNullException.ThrowIfNull(output);
        options ??= CellBufferImageExportOptions.Default;

        using var image = RenderImage(buffer, options);
        using var data = EncodeImage(image, format, options);
        data.SaveTo(output);
    }

    /// <summary>
    /// Exports the specified <paramref name="buffer"/> to an image file.
    /// </summary>
    /// <param name="buffer">The source buffer.</param>
    /// <param name="path">The destination image path.</param>
    /// <param name="options">The export options.</param>
    public static void Export(CellBuffer buffer, string path, CellBufferImageExportOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        ArgumentException.ThrowIfNullOrEmpty(path);

        var format = InferFormatFromPath(path);
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        using var stream = File.Create(path);
        Export(buffer, stream, format, options);
    }

    internal static int? GetFallbackCodepointForText(string text)
        => ScreenshotTypefaceResolver.GetFallbackCodepoint(text);

    private static SKImage RenderImage(CellBuffer buffer, CellBufferImageExportOptions options)
    {
        var crop = ResolveCrop(buffer, options);
        if (crop.Width <= 0 || crop.Height <= 0)
        {
            crop = new Rectangle(0, 0, Math.Max(1, buffer.Width), Math.Max(1, buffer.Height));
        }

        using var typeface = LoadTypeface(options.Font);
        using var typefaceResolver = new ScreenshotTypefaceResolver(typeface);
        using var baseFont = CreateBaseFont(typeface, options.Font);
        var fontMetrics = baseFont.Metrics;
        var cellWidth = Math.Max(1f, options.Font.CellWidthPx ?? MeasureCellWidth(baseFont));
        var cellHeight = Math.Max(1f, options.Font.CellHeightPx ?? MeasureCellHeight(fontMetrics, options.Font.SizePx));

        var bitmapWidth = Math.Max(1, (int)Math.Ceiling(crop.Width * cellWidth));
        var bitmapHeight = Math.Max(1, (int)Math.Ceiling(crop.Height * cellHeight));

        using var surface = SKSurface.Create(new SKImageInfo(bitmapWidth, bitmapHeight, SKColorType.Rgba8888, SKAlphaType.Premul));
        if (surface is null)
        {
            throw new InvalidOperationException("Unable to create a Skia surface for screenshot export.");
        }

        var canvas = surface.Canvas;
        canvas.Clear(SKColors.Transparent);

        using var backgroundPaint = new SKPaint
        {
            IsAntialias = false,
            Style = SKPaintStyle.Fill,
        };

        using var decorationPaint = new SKPaint
        {
            IsAntialias = false,
            Style = SKPaintStyle.Fill,
        };

        var baseStyle = ResolveBaseStyle(buffer, crop, options);
        var hasBaseBackground = TryGetBackgroundColor(baseStyle, out var baseBackground);
        if (options.FillBackground && hasBaseBackground)
        {
            backgroundPaint.Color = baseBackground;
            canvas.DrawRect(SKRect.Create(0, 0, bitmapWidth, bitmapHeight), backgroundPaint);
        }

        var scalars = buffer.UnsafeScalars;
        var cells = buffer.UnsafeCells;
        var baselineOffset = -fontMetrics.Ascent;
        var underlineOffset = Math.Max(1f, cellHeight - Math.Max(1f, cellHeight * 0.18f));
        var decorationThickness = Math.Max(1f, cellHeight * 0.08f);
        var strikeY = baselineOffset + ((fontMetrics.Ascent + fontMetrics.Descent) * 0.35f);

        for (var row = 0; row < crop.Height; row++)
        {
            var bufferY = crop.Y + row;
            if ((uint)bufferY >= (uint)buffer.Height)
            {
                continue;
            }

            var rowBase = bufferY * buffer.Width;
            for (var col = 0; col < crop.Width; col++)
            {
                var bufferX = crop.X + col;
                if ((uint)bufferX >= (uint)buffer.Width)
                {
                    continue;
                }

                var index = rowBase + bufferX;
                var cell = cells[index];
                if (cell.IsContinuation)
                {
                    continue;
                }

                var style = cell.WithoutContinuation();
                var scalar = scalars[index];
                string text;
                int width;
                bool hasInk;

                if (scalar < 0 && buffer.TryGetTextElement(scalar, out var textElement, out var elementWidth))
                {
                    text = textElement;
                    width = Math.Max(1, elementWidth);
                    hasInk = textElement.Length > 0 && !string.IsNullOrWhiteSpace(textElement);
                }
                else
                {
                    var value = scalar == 0 ? ' ' : scalar;
                    text = new Rune(value).ToString();
                    width = Math.Max(1, buffer.GetTextWidth(text));
                    hasInk = value != ' ';
                }

                var x = col * cellWidth;
                var y = row * cellHeight;
                var widthPx = width * cellWidth;

                var hasBackground = TryGetBackgroundColor(style, out var backgroundColor);
                var shouldDrawBackground = hasBackground && (!options.FillBackground || !hasBaseBackground || backgroundColor != baseBackground);
                if (shouldDrawBackground)
                {
                    backgroundPaint.Color = backgroundColor;
                    canvas.DrawRect(SKRect.Create(x, y, widthPx, cellHeight), backgroundPaint);
                }

                if (!hasInk || (style.TextStyle & TextStyle.Hidden) != 0)
                {
                    continue;
                }

                var foreground = TryGetForegroundColor(style, out var fgColor)
                    ? fgColor
                    : ToSkColor(options.DefaultForeground);

                var glyphTypeface = typefaceResolver.ResolveTypeface(text, baseFont);
                using var glyphFont = CreateGlyphFont(glyphTypeface, baseFont, style.TextStyle, options.Font);
                using var glyphPaint = CreateGlyphPaint(foreground, style.TextStyle, options.Font);
                // Terminal glyphs can legally overhang their nominal cell box. Clip to the full row instead of the
                // per-cell box so checkbox/emoji glyphs keep their horizontal bleed while still staying within the row.
                canvas.Save();
                canvas.ClipRect(SKRect.Create(0, y, bitmapWidth, cellHeight));
                canvas.DrawShapedText(text, x, y + baselineOffset, SKTextAlign.Left, glyphFont, glyphPaint);
                canvas.Restore();

                if ((style.TextStyle & TextStyle.Underline) != 0)
                {
                    decorationPaint.Color = glyphPaint.Color;
                    canvas.DrawRect(SKRect.Create(x, y + underlineOffset, widthPx, decorationThickness), decorationPaint);
                }

                if ((style.TextStyle & TextStyle.Strikethrough) != 0)
                {
                    decorationPaint.Color = glyphPaint.Color;
                    canvas.DrawRect(SKRect.Create(x, y + strikeY, widthPx, decorationThickness), decorationPaint);
                }
            }
        }

        return surface.Snapshot();
    }

    private static SKFont CreateBaseFont(SKTypeface typeface, ScreenshotFontOptions options)
        => new()
        {
            Typeface = typeface,
            Size = options.SizePx,
            Subpixel = options.Subpixel,
            Edging = options.Antialias ? SKFontEdging.SubpixelAntialias : SKFontEdging.Alias,
            ForceAutoHinting = true,
            Hinting = SKFontHinting.Full,
        };

    private static SKFont CreateGlyphFont(SKTypeface typeface, SKFont baseFont, TextStyle style, ScreenshotFontOptions options)
    {
        var font = new SKFont(typeface, baseFont.Size, baseFont.ScaleX, (style & TextStyle.Italic) != 0 ? -0.2f : 0)
        {
            Embolden = (style & TextStyle.Bold) != 0,
            Subpixel = options.Subpixel,
            Edging = options.Antialias ? SKFontEdging.SubpixelAntialias : SKFontEdging.Alias,
            ForceAutoHinting = true,
            EmbeddedBitmaps = true,
            Hinting = SKFontHinting.Full,
        };

        return font;
    }

    private static SKPaint CreateGlyphPaint(SKColor color, TextStyle style, ScreenshotFontOptions options)
    {
        var paint = new SKPaint
        {
            Color = color,
            IsAntialias = options.Antialias,
            Style = SKPaintStyle.Fill,
        };

        if ((style & TextStyle.Dim) != 0)
        {
            paint.Color = color.WithAlpha((byte)Math.Clamp((int)(color.Alpha * 0.75f), 0, byte.MaxValue));
        }

        return paint;
    }

    private static SKData EncodeImage(SKImage image, ScreenshotImageFormat format, CellBufferImageExportOptions options)
    {
        var encodedFormat = format switch
        {
            ScreenshotImageFormat.Png => SKEncodedImageFormat.Png,
            ScreenshotImageFormat.Jpeg => SKEncodedImageFormat.Jpeg,
            ScreenshotImageFormat.Webp => SKEncodedImageFormat.Webp,
            _ => throw new ArgumentOutOfRangeException(nameof(format)),
        };

        var data = image.Encode(encodedFormat, Math.Clamp(options.Quality, 0, 100));
        if (data is null)
        {
            throw new InvalidOperationException($"Failed to encode screenshot as {format.ToString().ToUpperInvariant()}.");
        }

        return data;
    }

    private static SKTypeface LoadTypeface(ScreenshotFontOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.Path))
        {
            return SKTypeface.FromFile(options.Path);
        }

        if (!string.IsNullOrWhiteSpace(options.FamilyName))
        {
            return SKTypeface.FromFamilyName(options.FamilyName);
        }

        using var data = SKData.CreateCopy(DefaultFontBytes.Value);
        return SKTypeface.FromData(data);
    }

    private static float MeasureCellWidth(SKFont font)
    {
        var width = font.MeasureText("M");
        if (width > 0)
        {
            return width;
        }

        var metrics = font.Metrics;
        return Math.Max(1f, metrics.CapHeight > 0 ? metrics.CapHeight * 0.6f : font.Size * 0.6f);
    }

    private static float MeasureCellHeight(SKFontMetrics metrics, float fontSize)
    {
        var lineHeight = metrics.Descent - metrics.Ascent + metrics.Leading;
        return lineHeight > 0 ? lineHeight : Math.Max(1f, fontSize);
    }

    private static ScreenshotImageFormat InferFormatFromPath(string path)
    {
        var extension = Path.GetExtension(path);
        if (string.IsNullOrWhiteSpace(extension))
        {
            throw new ArgumentException("The destination path must include a file extension such as .png or .jpg.", nameof(path));
        }

        return extension.ToLowerInvariant() switch
        {
            ".png" => ScreenshotImageFormat.Png,
            ".jpg" => ScreenshotImageFormat.Jpeg,
            ".jpeg" => ScreenshotImageFormat.Jpeg,
            ".webp" => ScreenshotImageFormat.Webp,
            _ => throw new NotSupportedException($"Unsupported screenshot format `{extension}`. Supported extensions: .png, .jpg, .jpeg, .webp."),
        };
    }

    private static Rectangle ResolveCrop(CellBuffer buffer, CellBufferImageExportOptions options)
    {
        var crop = options.Crop ?? new Rectangle(0, 0, buffer.Width, buffer.Height);
        crop = Clamp(crop, buffer.Width, buffer.Height);

        if (options.AutoCrop)
        {
            var auto = AutoCrop(buffer, crop, options);
            if (auto.Width > 0 && auto.Height > 0)
            {
                crop = auto;
            }
        }

        if (options.Padding != default)
        {
            crop = Inflate(crop, options.Padding);
        }

        return crop;
    }

    private static Rectangle AutoCrop(CellBuffer buffer, Rectangle crop, CellBufferImageExportOptions options)
    {
        var scalars = buffer.UnsafeScalars;
        var cells = buffer.UnsafeCells;
        var baseStyle = ResolveBaseStyle(buffer, crop, options);

        var minX = int.MaxValue;
        var minY = int.MaxValue;
        var maxX = int.MinValue;
        var maxY = int.MinValue;

        for (var y = crop.Y; y < crop.Bottom; y++)
        {
            if ((uint)y >= (uint)buffer.Height)
            {
                continue;
            }

            var rowBase = y * buffer.Width;
            for (var x = crop.X; x < crop.Right; x++)
            {
                if ((uint)x >= (uint)buffer.Width)
                {
                    continue;
                }

                var index = rowBase + x;
                var cell = cells[index];
                if (cell.IsContinuation)
                {
                    continue;
                }

                var style = cell.WithoutContinuation();
                var scalar = scalars[index];
                var isSpace = scalar == 0 || scalar == ' ';
                if (scalar < 0)
                {
                    isSpace = false;
                }

                if (isSpace && style == baseStyle)
                {
                    continue;
                }

                minX = Math.Min(minX, x);
                minY = Math.Min(minY, y);
                maxX = Math.Max(maxX, x);
                maxY = Math.Max(maxY, y);
            }
        }

        if (minX == int.MaxValue)
        {
            return default;
        }

        return new Rectangle(minX, minY, Math.Max(1, (maxX - minX) + 1), Math.Max(1, (maxY - minY) + 1));
    }

    private static Style ResolveBaseStyle(CellBuffer buffer, Rectangle crop, CellBufferImageExportOptions options)
    {
        if (options.BaseStyleOverride is { } styleOverride)
        {
            return styleOverride.WithoutContinuation();
        }

        var scalars = buffer.UnsafeScalars;
        var cells = buffer.UnsafeCells;
        var counts = new Dictionary<Style, int>();

        for (var y = crop.Y; y < crop.Bottom; y++)
        {
            if ((uint)y >= (uint)buffer.Height)
            {
                continue;
            }

            var rowBase = y * buffer.Width;
            for (var x = crop.X; x < crop.Right; x++)
            {
                if ((uint)x >= (uint)buffer.Width)
                {
                    continue;
                }

                var index = rowBase + x;
                var cell = cells[index];
                if (cell.IsContinuation)
                {
                    continue;
                }

                var scalar = scalars[index];
                if (scalar < 0 || (scalar != 0 && scalar != ' '))
                {
                    continue;
                }

                var style = cell.WithoutContinuation();
                counts.TryGetValue(style, out var count);
                counts[style] = count + 1;
            }
        }

        if (counts.Count > 0)
        {
            var best = default(Style);
            var bestCount = -1;
            foreach (var (style, count) in counts)
            {
                if (count > bestCount)
                {
                    best = style;
                    bestCount = count;
                }
            }

            return best.WithoutContinuation();
        }

        var x0 = Math.Clamp(crop.X, 0, Math.Max(0, buffer.Width - 1));
        var y0 = Math.Clamp(crop.Y, 0, Math.Max(0, buffer.Height - 1));
        return cells[(y0 * buffer.Width) + x0].WithoutContinuation();
    }

    private static bool TryGetForegroundColor(Style style, out SKColor color)
    {
        if (style.TryGetForeground(out var fg) && fg.Kind != ColorKind.Default)
        {
            color = ToSkColor(fg);
            return true;
        }

        color = default;
        return false;
    }

    private static bool TryGetBackgroundColor(Style style, out SKColor color)
    {
        if (style.TryGetBackground(out var bg) && bg.Kind != ColorKind.Default)
        {
            color = ToSkColor(bg);
            return true;
        }

        color = default;
        return false;
    }

    private static SKColor ToSkColor(Color color)
    {
        if (color.Kind is ColorKind.Basic16 or ColorKind.Indexed256)
        {
            color = color.ToRgb();
        }

        return color.Kind == ColorKind.RgbA
            ? new SKColor(color.R, color.G, color.B, color.A)
            : new SKColor(color.R, color.G, color.B, byte.MaxValue);
    }

    private static Rectangle Inflate(Rectangle rect, Thickness padding)
        => new(
            rect.X - Math.Max(0, padding.Left),
            rect.Y - Math.Max(0, padding.Top),
            rect.Width + Math.Max(0, padding.Left) + Math.Max(0, padding.Right),
            rect.Height + Math.Max(0, padding.Top) + Math.Max(0, padding.Bottom));

    private static Rectangle Clamp(Rectangle rect, int width, int height)
    {
        var x0 = Math.Clamp(rect.X, 0, width);
        var y0 = Math.Clamp(rect.Y, 0, height);
        var x1 = Math.Clamp(rect.Right, 0, width);
        var y1 = Math.Clamp(rect.Bottom, 0, height);
        return new Rectangle(x0, y0, Math.Max(0, x1 - x0), Math.Max(0, y1 - y0));
    }

    private static byte[] LoadDefaultFontBytes()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(DefaultFontResourceName);
        if (stream is null)
        {
            throw new InvalidOperationException($"Unable to locate embedded screenshot font resource `{DefaultFontResourceName}`.");
        }

        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        return memory.ToArray();
    }

    private sealed class ScreenshotTypefaceResolver : IDisposable
    {
        private readonly SKFontManager _fontManager;
        private readonly SKTypeface _baseTypeface;
        private readonly Dictionary<string, SKTypeface> _cache;
        private bool _disposed;

        public ScreenshotTypefaceResolver(SKTypeface baseTypeface)
        {
            _baseTypeface = baseTypeface;
            _fontManager = SKFontManager.Default;
            _cache = new Dictionary<string, SKTypeface>(StringComparer.Ordinal);
        }

        public SKTypeface ResolveTypeface(string text, SKFont baseFont)
        {
            if (string.IsNullOrEmpty(text) || baseFont.ContainsGlyphs(text))
            {
                return _baseTypeface;
            }

            if (_cache.TryGetValue(text, out var cached))
            {
                return cached;
            }

            var codepoint = GetFallbackCodepoint(text);
            var matched = codepoint is null
                ? null
                : _fontManager.MatchCharacter(string.Empty, _baseTypeface.FontStyle, null, codepoint.Value)
                  ?? _fontManager.MatchCharacter(codepoint.Value);

            if (matched is null)
            {
                matched = _baseTypeface;
            }
            else
            {
            }

            _cache[text] = matched;
            return matched;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            foreach (var (_, typeface) in _cache)
            {
                if (!ReferenceEquals(typeface, _baseTypeface))
                {
                    typeface.Dispose();
                }
            }

            _cache.Clear();
            _disposed = true;
        }

        internal static int? GetFallbackCodepoint(string text)
        {
            for (var i = 0; i < text.Length;)
            {
                if (Rune.DecodeFromUtf16(text.AsSpan(i), out var rune, out var consumed) != OperationStatus.Done || consumed <= 0)
                {
                    i++;
                    continue;
                }

                i += consumed;
                if (Rune.IsControl(rune))
                {
                    continue;
                }

                if (rune.IsBmp && EmojiSequenceSkippableCharacters.Contains((char)rune.Value))
                {
                    continue;
                }

                return rune.Value;
            }

            return null;
        }
    }
}
