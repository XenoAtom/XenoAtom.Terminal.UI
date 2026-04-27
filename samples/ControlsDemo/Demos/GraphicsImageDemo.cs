using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using SkiaSharp;
using XenoAtom.Terminal.Graphics;
using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Styling;
using ImageControl = XenoAtom.Terminal.UI.Graphics.Image;
using TerminalGraphicsSupportState = XenoAtom.Terminal.TerminalGraphicsSupportState;
using TerminalHost = XenoAtom.Terminal.Terminal;

namespace XenoAtom.Terminal.UI.ControlsDemo.Demos;

[Demo("Images & Graphics", "Visualization", Description = "Terminal image rendering with a static photo and a live SkiaSharp-generated frame stream.")]
public sealed class GraphicsImageDemo : ControlsDemoBase
{
    private const int PhotoCellWidth = 28;
    private const int PhotoCellHeight = 10;
    private const int AnimationCellWidth = 36;
    private const int AnimationCellHeight = 10;
    private const int AnimationFallbackCellPixelWidth = 8;
    private const int AnimationFallbackCellPixelHeight = 16;

    public GraphicsImageDemo() : base(DemoSource.Get())
    {
    }

    public override Visual Build(DemoContext context)
    {
        var showStaticImage = new State<bool>(true);
        var showDynamicImage = new State<bool>(true);
        var graphics = context.Graphics;
        var sixelDithering = new State<bool>(graphics?.SixelOptions.EnableDithering ?? false);
        var sixelRunLengthEncoding = new State<bool>(graphics?.SixelOptions.UseRunLengthEncoding ?? true);
        var sixelPaletteIndex = new State<int>(graphics?.SixelOptions.PaletteMode == TerminalSixelPaletteMode.FixedRgb332 ? 1 : 0);
        var appliedDithering = sixelDithering.Value;
        var appliedRunLengthEncoding = sixelRunLengthEncoding.Value;
        var appliedPaletteMode = ToPaletteMode(sixelPaletteIndex.Value);
        var animationSource = new AnimatedSkiaImageSource();
        var photoSource = TerminalImageSource.FromFile(ResolveSnowPhotoPath());

        var animationImage = new ImageControl(animationSource)
        {
            CellWidth = AnimationCellWidth,
            CellHeight = AnimationCellHeight,
            ScaleMode = ImageScaleMode.Stretch,
            PreserveAspectRatio = false,
            AccessibilityText = "Live SkiaSharp animation with bouncing balls and rotating triangles",
            FallbackContent = BuildAnimationFallback(AnimationCellWidth, AnimationCellHeight),
        };
        animationImage.Update(_ =>
        {
            if (showDynamicImage.Value)
            {
                animationSource.Advance(context.Runtime.Frame.Value);
            }
        });

        var photoImage = new ImageControl(photoSource)
        {
            CellWidth = PhotoCellWidth,
            CellHeight = PhotoCellHeight,
            ScaleMode = ImageScaleMode.Fit,
            PreserveAspectRatio = true,
            AccessibilityText = "Snow photo rendered through terminal graphics",
            FallbackContent = BuildSnowPhotoFallback(PhotoCellWidth, PhotoCellHeight),
        };

        var staticPanel = new VStack(
                DemoUi.Title("Static photo"),
                new Border(photoImage).Padding(1),
                new TextBlock("Assets/snow_photo.jpg · ImageScaleMode.Fit"))
            .Spacing(1)
            .IsVisible(showStaticImage);

        var dynamicPanel = new VStack(
                DemoUi.Title("Live generated raster"),
                new Border(animationImage).Padding(1),
                new TextBlock("SkiaSharp frame stream · driven by the UI dependency system"))
            .Spacing(1)
            .IsVisible(showDynamicImage);

        var sixelOptionsPanel = new VStack(
                DemoUi.Title("Sixel encoder options"),
                DemoUi.Hint("These switches affect Sixel output only. Kitty/iTerm2 can pass through or send RGB data and do not use the Sixel palette quantizer."),
                new HStack(
                        new CheckBox("Floyd-Steinberg dither").IsChecked(sixelDithering).IsEnabled(graphics is not null),
                        new CheckBox("Run-length encode").IsChecked(sixelRunLengthEncoding).IsEnabled(graphics is not null),
                        new Select<string>()
                            .Items(["Adaptive palette", "Fixed RGB332 palette"])
                            .SelectedIndex(sixelPaletteIndex)
                            .IsEnabled(graphics is not null))
                    .Spacing(3),
                new TextBlock(() => GetSixelOptionsText(context.Graphics, ToPaletteMode(sixelPaletteIndex.Value), sixelDithering.Value, sixelRunLengthEncoding.Value)).Wrap(true))
            .Spacing(1);

        var root = new VStack(
                DemoUi.Hint("Images are rendered by the optional graphics presenter. Static images use files/encoded bytes; real-time sources notify the Image control when a newer frame is available. Toggle the F12 debug overlay to inspect Gfx/GfxImg command, encode, payload, and presentation metrics. The cell-art previews below appear only when terminal graphics are unavailable, including deterministic website screenshots."),
                new TextBlock(GetGraphicsDiagnostics).Wrap(true),
                new HStack(
                        new CheckBox("Show static photo").IsChecked(showStaticImage),
                        new CheckBox("Show live raster").IsChecked(showDynamicImage))
                    .Spacing(3),
                sixelOptionsPanel,
                new HStack(
                        staticPanel,
                        dynamicPanel)
                    .Spacing(2),
                new Paragraph("The animated source renders raw RGBA frames with SkiaSharp, then Terminal.UI.Graphics encodes only the latest frame for Kitty, iTerm2, or Sixel according to terminal capabilities. The animation is driven by the same bindable UI frame state as the rest of the demo, while the graphics presenter coalesces slow terminal graphics redraws."))
            .Spacing(1);

        root.Update(v =>
        {
            if (context.Graphics is not { } currentGraphics)
            {
                return;
            }

            var paletteMode = ToPaletteMode(sixelPaletteIndex.Value);
            var dithering = sixelDithering.Value;
            var runLengthEncoding = sixelRunLengthEncoding.Value;
            if (paletteMode == appliedPaletteMode && dithering == appliedDithering && runLengthEncoding == appliedRunLengthEncoding)
            {
                return;
            }

            currentGraphics.SixelOptions.PaletteMode = paletteMode;
            currentGraphics.SixelOptions.EnableDithering = dithering;
            currentGraphics.SixelOptions.UseRunLengthEncoding = runLengthEncoding;
            currentGraphics.Presenter.Reset();
            currentGraphics.Presenter.Metrics.Reset();
            v.App?.RequestGraphicsRender();
            appliedPaletteMode = paletteMode;
            appliedDithering = dithering;
            appliedRunLengthEncoding = runLengthEncoding;
        });

        return root;
    }

    private static TerminalSixelPaletteMode ToPaletteMode(int selectedIndex)
        => selectedIndex == 1 ? TerminalSixelPaletteMode.FixedRgb332 : TerminalSixelPaletteMode.Adaptive;

    private static string GetSixelOptionsText(DemoGraphicsOptions? graphics, TerminalSixelPaletteMode paletteMode, bool dithering, bool runLengthEncoding)
    {
        var options = $"Sixel preview: palette={paletteMode}, dither={(dithering ? "on" : "off")}, RLE={(runLengthEncoding ? "on" : "off")}.";
        if (graphics is null)
        {
            return options + " No live graphics presenter is attached in this host, so the switches are disabled.";
        }

        var metrics = graphics.Presenter.Metrics;
        return options + $" Cache h/m/s={metrics.CacheHitCount}/{metrics.CacheMissCount}/{metrics.CacheStoreCount}; last h/m={metrics.LastCacheHitCount}/{metrics.LastCacheMissCount}.";
    }

    private static Visual BuildSnowPhotoFallback(int width, int height)
        => new Canvas(PaintSnowPhotoFallback)
            .MinWidth(width)
            .MaxWidth(width)
            .MinHeight(height)
            .MaxHeight(height);

    private static void PaintSnowPhotoFallback(CanvasContext ctx)
    {
        var width = Math.Max(1, ctx.Size.Width);
        var height = Math.Max(1, ctx.Size.Height);
        var empty = new Rune(' ');

        for (var y = 0; y < height; y++)
        {
            var fy = height <= 1 ? 0f : y / (float)(height - 1);
            for (var x = 0; x < width; x++)
            {
                var fx = width <= 1 ? 0f : x / (float)(width - 1);
                var color = LerpColor(Color.Rgb(0x8D, 0xC5, 0xF4), Color.Rgb(0xD7, 0xEC, 0xFF), fy);

                if (InsideTriangle(fx, fy, 0.02f, 1.0f, 0.46f, 0.22f, 0.88f, 1.0f))
                {
                    color = LerpColor(Color.Rgb(0x56, 0x69, 0x8F), Color.Rgb(0xD8, 0xE8, 0xF8), fy);
                }

                if (InsideTriangle(fx, fy, 0.18f, 1.0f, 0.66f, 0.34f, 1.15f, 1.0f))
                {
                    color = LerpColor(Color.Rgb(0xEC, 0xF5, 0xFF), Color.Rgb(0xB5, 0xD1, 0xE8), fy);
                }

                if (fy > 0.78f)
                {
                    color = LerpColor(Color.Rgb(0xF7, 0xFB, 0xFF), Color.Rgb(0xC7, 0xDB, 0xEC), (fy - 0.78f) / 0.22f);
                }

                ctx.SetPixel(x, y, empty, Style.None.WithBackground(color));
            }
        }

        var tree = Style.None.WithForeground(Color.Rgb(0x0F, 0x3D, 0x2E));
        for (var x = 4; x < width - 2; x += 7)
        {
            var baseY = height - 3;
            ctx.DrawLine(x, baseY, x - 2, baseY + 2, new Rune('▲'), tree);
            ctx.DrawLine(x, baseY, x + 2, baseY + 2, new Rune('▲'), tree);
            ctx.SetPixel(x, baseY + 2, new Rune('│'), tree);
        }

        ctx.WriteText(2, height - 1, "snow_photo.jpg", Style.None.WithForeground(Color.Rgb(0x0F, 0x17, 0x2A)).WithBackground(Color.Rgb(0xE8, 0xF2, 0xFF)));
    }

    private static Visual BuildAnimationFallback(int width, int height)
        => new Canvas(PaintAnimationFallback)
            .UseFinePixels(true)
            .MinWidth(width)
            .MaxWidth(width)
            .MinHeight(height)
            .MaxHeight(height)
            .Style(CanvasStyle.Default with { DefaultRune = new Rune('█') });

    private static void PaintAnimationFallback(CanvasContext ctx)
    {
        var width = Math.Max(1, ctx.Size.Width);
        var height = Math.Max(1, ctx.Size.Height);
        var empty = new Rune(' ');

        for (var y = 0; y < height; y++)
        {
            var fy = height <= 1 ? 0f : y / (float)(height - 1);
            for (var x = 0; x < width; x++)
            {
                var fx = width <= 1 ? 0f : x / (float)(width - 1);
                var color = LerpColor(Color.Rgb(0x0F, 0x17, 0x2A), Color.Rgb(0x08, 0x3A, 0x4A), (fx + fy) * 0.5f);
                ctx.SetPixel(x, y, empty, Style.None.WithBackground(color));
            }
        }

        var grid = Style.None.WithForeground(Color.Rgb(0x38, 0x4B, 0x69));
        for (var x = 0; x < width; x += 6)
        {
            ctx.DrawVLine(x, 0, height, new Rune('│'), grid);
        }

        for (var y = 0; y < height; y += 4)
        {
            ctx.DrawHLine(0, y, width, new Rune('─'), grid);
        }

        var outerTriangleRadius = Math.Max(4, Math.Min(width / 3, height));
        var innerTriangleRadius = Math.Max(3, outerTriangleRadius * 2 / 3);
        DrawTriangle(ctx, width / 2, height / 2, outerTriangleRadius, 20f, Color.Rgb(0x38, 0xBD, 0xF8));
        DrawTriangle(ctx, width / 2, height / 2, innerTriangleRadius, -28f, Color.Rgb(0xF9, 0x73, 0x16));

        var largeBallRadius = Math.Max(2, height / 5);
        var smallBallRadius = Math.Max(1, height / 6);
        FillBall(ctx, Math.Max(largeBallRadius, width / 5), Math.Max(largeBallRadius, height / 3), largeBallRadius, Color.Rgb(0x4D, 0xB6, 0xFF));
        FillBall(ctx, Math.Max(smallBallRadius, width * 2 / 5), Math.Max(smallBallRadius, height * 2 / 3), smallBallRadius, Color.Rgb(0xFF, 0xB8, 0x6B));
        FillBall(ctx, Math.Min(width - largeBallRadius - 1, width * 2 / 3), Math.Max(largeBallRadius, height / 3), largeBallRadius, Color.Rgb(0xA7, 0xF3, 0xD0));
        FillBall(ctx, Math.Min(width - smallBallRadius - 1, width * 5 / 6), Math.Max(smallBallRadius, height * 2 / 3), smallBallRadius, Color.Rgb(0xF0, 0xAB, 0xFC));

        ctx.WriteText(2, height - 1, "live SkiaSharp RGBA frames", Style.None.WithForeground(Color.Rgb(0xE0, 0xF2, 0xFE)).WithBackground(Color.Rgb(0x0F, 0x17, 0x2A)));
    }

    private static void DrawTriangle(CanvasContext ctx, int centerX, int centerY, int radius, float degrees, Color color)
    {
        var points = new (int X, int Y)[3];
        var radians = degrees * MathF.PI / 180f;
        for (var i = 0; i < 3; i++)
        {
            var a = radians + i * MathF.Tau / 3f - MathF.PI / 2f;
            points[i] = ((int)MathF.Round(centerX + MathF.Cos(a) * radius), (int)MathF.Round(centerY + MathF.Sin(a) * radius * 0.45f));
        }

        var style = Style.None.WithForeground(color);
        ctx.DrawLine(points[0].X, points[0].Y, points[1].X, points[1].Y, style);
        ctx.DrawLine(points[1].X, points[1].Y, points[2].X, points[2].Y, style);
        ctx.DrawLine(points[2].X, points[2].Y, points[0].X, points[0].Y, style);
    }

    private static void FillBall(CanvasContext ctx, int centerX, int centerY, int radius, Color color)
    {
        var empty = new Rune(' ');
        var shadow = Style.None.WithBackground(Color.Rgb(0x04, 0x0A, 0x16));
        for (var y = centerY - radius; y <= centerY + radius; y++)
        {
            for (var x = centerX - radius * 2; x <= centerX + radius * 2; x++)
            {
                var dx = (x - centerX) / 2f;
                var dy = y - centerY;
                if ((dx * dx) + (dy * dy) <= radius * radius)
                {
                    ctx.SetPixel(x + 1, y + 1, empty, shadow);
                    ctx.SetPixel(x, y, empty, Style.None.WithBackground(color));
                }
            }
        }

        ctx.SetPixel(centerX - 1, centerY - 1, new Rune('•'), Style.None.WithForeground(Colors.White).WithBackground(color));
    }

    private static bool InsideTriangle(float px, float py, float ax, float ay, float bx, float by, float cx, float cy)
    {
        var d1 = Sign(px, py, ax, ay, bx, by);
        var d2 = Sign(px, py, bx, by, cx, cy);
        var d3 = Sign(px, py, cx, cy, ax, ay);
        var hasNegative = d1 < 0 || d2 < 0 || d3 < 0;
        var hasPositive = d1 > 0 || d2 > 0 || d3 > 0;
        return !(hasNegative && hasPositive);
    }

    private static float Sign(float px, float py, float ax, float ay, float bx, float by)
        => (px - bx) * (ay - by) - (ax - bx) * (py - by);

    private static Color LerpColor(Color start, Color end, float amount)
    {
        amount = Math.Clamp(amount, 0f, 1f);
        return Color.Rgb(
            Lerp(start.R, end.R, amount),
            Lerp(start.G, end.G, amount),
            Lerp(start.B, end.B, amount));
    }

    private static byte Lerp(byte start, byte end, float amount)
        => (byte)Math.Clamp((int)MathF.Round(start + (end - start) * amount), byte.MinValue, byte.MaxValue);

    private static string ResolveSnowPhotoPath()
    {
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "Assets", "snow_photo.jpg"),
            Path.Combine(Directory.GetCurrentDirectory(), "Assets", "snow_photo.jpg"),
            Path.Combine(Directory.GetCurrentDirectory(), "samples", "ControlsDemo", "Assets", "snow_photo.jpg"),
        };

        for (var i = 0; i < candidates.Length; i++)
        {
            if (File.Exists(candidates[i]))
            {
                return candidates[i];
            }
        }

        return candidates[0];
    }

    private static string GetGraphicsDiagnostics()
    {
        if (!TerminalHost.IsInitialized)
        {
            return "Terminal graphics: screenshot/fallback mode.";
        }

        var capabilities = TerminalHost.Graphics.Capabilities;
        return $"Terminal graphics: {capabilities.PreferredProtocol} ({FormatGraphicsSupport(capabilities.SupportState, capabilities.DetectionSource)})";
    }

    private static string FormatGraphicsSupport(TerminalGraphicsSupportState supportState, string detectionSource)
        => supportState switch
        {
            TerminalGraphicsSupportState.Heuristic => "Auto-detected",
            TerminalGraphicsSupportState.Confirmed => "Confirmed by probe",
            TerminalGraphicsSupportState.Forced => $"Forced{FormatDetectionSourceSuffix(detectionSource)}",
            TerminalGraphicsSupportState.Disabled => $"Disabled{FormatDetectionSourceSuffix(detectionSource)}",
            TerminalGraphicsSupportState.Unsupported => $"Unsupported{FormatDetectionSourceSuffix(detectionSource)}",
            _ => supportState.ToString(),
        };

    private static string FormatDetectionSourceSuffix(string detectionSource)
        => string.IsNullOrWhiteSpace(detectionSource) || string.Equals(detectionSource, "none", StringComparison.OrdinalIgnoreCase)
            ? string.Empty
            : $" by {FormatDetectionSource(detectionSource)}";

    private static string FormatDetectionSource(string detectionSource)
        => detectionSource switch
        {
            "backend" => "terminal backend",
            "environment" => "environment override",
            "heuristic" => "auto-detection",
            "heuristic-disabled" => "auto-detection policy",
            "multiplexer" => "terminal multiplexer policy",
            "options" => "explicit options",
            _ => detectionSource.Replace('-', ' '),
        };

    private sealed class AnimatedSkiaImageSource : TerminalImageSource, ITerminalRealtimeImageSource
    {
        private const int PixelWidth = AnimationCellWidth * AnimationFallbackCellPixelWidth;
        private const int PixelHeight = AnimationCellHeight * AnimationFallbackCellPixelHeight;
        private static readonly TimeSpan FrameInterval = TimeSpan.FromMilliseconds(16);
        private readonly object _sync = new();
        private readonly Stopwatch _clock = Stopwatch.StartNew();
        private readonly Ball[] _balls =
        [
            new(43, 40, 90, 73, 15, new SKColor(0x4D, 0xB6, 0xFF)),
            new(102, 93, -72, 90, 12, new SKColor(0xFF, 0xB8, 0x6B)),
            new(192, 50, 60, -97, 17, new SKColor(0xA7, 0xF3, 0xD0)),
            new(240, 112, -96, -60, 13, new SKColor(0xF0, 0xAB, 0xFC)),
        ];
        private long _lastUiFrame = -1;
        private TimeSpan _lastStep;
        private TimeSpan _frameTimestamp;
        private long _version;

        public event EventHandler<TerminalImageFrameAvailableEventArgs>? FrameAvailable;

        public TimeSpan MinimumFrameInterval => FrameInterval;

        public long Version => Interlocked.Read(ref _version);

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;

        public void Advance(long uiFrame)
        {
            EventHandler<TerminalImageFrameAvailableEventArgs>? handler;
            TimeSpan now;
            long version;
            lock (_sync)
            {
                if (uiFrame == _lastUiFrame)
                {
                    return;
                }

                _lastUiFrame = uiFrame;
                now = _clock.Elapsed;
                if (_lastStep == TimeSpan.Zero)
                {
                    _lastStep = now;
                }

                var delta = now - _lastStep;
                _lastStep = now;
                var dt = Math.Clamp(delta.TotalSeconds, 0.0, 0.05);
                StepBalls((float)dt);

                _frameTimestamp = now;
                version = Interlocked.Increment(ref _version);
                handler = FrameAvailable;
            }

            handler?.Invoke(this, new TerminalImageFrameAvailableEventArgs(version, now));
        }

        public override ValueTask<TerminalImageFrame?> GetFrameAsync(TerminalImageFrameRequest request, CancellationToken cancellationToken = default)
            => GetLatestFrameAsync(request, cancellationToken);

        public ValueTask<TerminalImageFrame?> GetLatestFrameAsync(TerminalImageFrameRequest request, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            byte[] bytes;
            TimeSpan timestamp;
            long version;
            lock (_sync)
            {
                timestamp = _frameTimestamp == TimeSpan.Zero ? request.Timestamp ?? _clock.Elapsed : _frameTimestamp;
                bytes = RenderFrame(timestamp);
                version = Version;
            }

            return ValueTask.FromResult<TerminalImageFrame?>(new TerminalImageFrame
            {
                Format = TerminalImageFormat.RawRgba32,
                Data = bytes,
                PixelWidth = PixelWidth,
                PixelHeight = PixelHeight,
                SourceId = "controls-demo-skia-animation",
                Version = version,
                Timestamp = timestamp,
            });
        }

        private void StepBalls(float dt)
        {
            if (dt <= 0f)
            {
                return;
            }

            for (var i = 0; i < _balls.Length; i++)
            {
                var ball = _balls[i];
                ball.X += ball.Vx * dt;
                ball.Y += ball.Vy * dt;

                if (ball.X - ball.Radius < 0f)
                {
                    ball.X = ball.Radius;
                    ball.Vx = Math.Abs(ball.Vx);
                }
                else if (ball.X + ball.Radius > PixelWidth)
                {
                    ball.X = PixelWidth - ball.Radius;
                    ball.Vx = -Math.Abs(ball.Vx);
                }

                if (ball.Y - ball.Radius < 0f)
                {
                    ball.Y = ball.Radius;
                    ball.Vy = Math.Abs(ball.Vy);
                }
                else if (ball.Y + ball.Radius > PixelHeight)
                {
                    ball.Y = PixelHeight - ball.Radius;
                    ball.Vy = -Math.Abs(ball.Vy);
                }

                _balls[i] = ball;
            }
        }

        private byte[] RenderFrame(TimeSpan timestamp)
        {
            using var bitmap = new SKBitmap(new SKImageInfo(PixelWidth, PixelHeight, SKColorType.Rgba8888, SKAlphaType.Opaque));
            using var canvas = new SKCanvas(bitmap);
            DrawBackground(canvas);
            DrawRotatingTriangles(canvas, timestamp);
            DrawBalls(canvas);
            DrawHud(canvas, timestamp);
            canvas.Flush();
            return CopyTightRgba(bitmap);
        }

        private static void DrawBackground(SKCanvas canvas)
        {
            using var paint = new SKPaint { IsAntialias = true };
            using var shader = SKShader.CreateLinearGradient(
                new SKPoint(0, 0),
                new SKPoint(PixelWidth, PixelHeight),
                [new SKColor(0x0F, 0x17, 0x2A), new SKColor(0x1E, 0x1B, 0x4B), new SKColor(0x08, 0x3A, 0x4A)],
                [0f, 0.58f, 1f],
                SKShaderTileMode.Clamp);
            paint.Shader = shader;
            canvas.DrawRect(0, 0, PixelWidth, PixelHeight, paint);

            paint.Shader = null;
            paint.Color = new SKColor(255, 255, 255, 24);
            paint.StrokeWidth = 1f;
            paint.Style = SKPaintStyle.Stroke;
            for (var x = 0; x <= PixelWidth; x += 32)
            {
                canvas.DrawLine(x, 0, x, PixelHeight, paint);
            }

            for (var y = 0; y <= PixelHeight; y += 32)
            {
                canvas.DrawLine(0, y, PixelWidth, y, paint);
            }
        }

        private static void DrawRotatingTriangles(SKCanvas canvas, TimeSpan timestamp)
        {
            var seconds = (float)timestamp.TotalSeconds;
            DrawTriangle(canvas, new SKPoint(PixelWidth * 0.50f, PixelHeight * 0.50f), 54f, seconds * 54f, new SKColor(0x38, 0xBD, 0xF8));
            DrawTriangle(canvas, new SKPoint(PixelWidth * 0.50f, PixelHeight * 0.50f), 32f, -seconds * 88f, new SKColor(0xF9, 0x73, 0x16));

            for (var i = 0; i < 3; i++)
            {
                var phase = seconds * 1.2f + i * MathF.Tau / 3f;
                var center = new SKPoint(PixelWidth * 0.5f + MathF.Cos(phase) * 86f, PixelHeight * 0.5f + MathF.Sin(phase) * 43f);
                DrawTriangle(canvas, center, 15f, seconds * 120f + i * 45f, new SKColor((byte)(130 + i * 40), (byte)(210 - i * 30), 255));
            }
        }

        private static void DrawTriangle(SKCanvas canvas, SKPoint center, float radius, float degrees, SKColor color)
        {
            using var path = new SKPath();
            var radians = degrees * MathF.PI / 180f;
            for (var i = 0; i < 3; i++)
            {
                var a = radians + i * MathF.Tau / 3f - MathF.PI / 2f;
                var point = new SKPoint(center.X + MathF.Cos(a) * radius, center.Y + MathF.Sin(a) * radius);
                if (i == 0)
                {
                    path.MoveTo(point);
                }
                else
                {
                    path.LineTo(point);
                }
            }
            path.Close();

            using var fill = new SKPaint { IsAntialias = true, Color = color.WithAlpha(70), Style = SKPaintStyle.Fill };
            canvas.DrawPath(path, fill);
            using var stroke = new SKPaint { IsAntialias = true, Color = color.WithAlpha(210), StrokeWidth = 4f, Style = SKPaintStyle.Stroke };
            canvas.DrawPath(path, stroke);
        }

        private void DrawBalls(SKCanvas canvas)
        {
            using var paint = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Fill };
            for (var i = 0; i < _balls.Length; i++)
            {
                var ball = _balls[i];
                paint.Color = new SKColor(0, 0, 0, 70);
                canvas.DrawCircle(ball.X + 5f, ball.Y + 7f, ball.Radius, paint);
                paint.Color = ball.Color;
                canvas.DrawCircle(ball.X, ball.Y, ball.Radius, paint);
                paint.Color = SKColors.White.WithAlpha(155);
                canvas.DrawCircle(ball.X - ball.Radius * 0.35f, ball.Y - ball.Radius * 0.35f, ball.Radius * 0.28f, paint);
            }
        }

        private static void DrawHud(SKCanvas canvas, TimeSpan timestamp)
        {
            using var paint = new SKPaint
            {
                IsAntialias = true,
                Color = new SKColor(255, 255, 255, 190),
            };
            using var font = new SKFont(SKTypeface.Default, 14f);
            canvas.DrawText($"raw RGBA frames · {timestamp.TotalSeconds,5:0.0}s", 12, PixelHeight - 14, SKTextAlign.Left, font, paint);
        }

        private static byte[] CopyTightRgba(SKBitmap bitmap)
        {
            var stride = PixelWidth * 4;
            var bytes = new byte[stride * PixelHeight];
            var pixels = bitmap.GetPixels();
            if (pixels == IntPtr.Zero)
            {
                return bytes;
            }

            if (bitmap.RowBytes == stride)
            {
                Marshal.Copy(pixels, bytes, 0, bytes.Length);
                return bytes;
            }

            for (var y = 0; y < PixelHeight; y++)
            {
                Marshal.Copy(IntPtr.Add(pixels, y * bitmap.RowBytes), bytes, y * stride, stride);
            }

            return bytes;
        }

        private struct Ball(float x, float y, float vx, float vy, float radius, SKColor color)
        {
            public float X = x;
            public float Y = y;
            public float Vx = vx;
            public float Vy = vy;
            public float Radius = radius;
            public SKColor Color = color;
        }
    }
}
