using XenoAtom.Ansi;
using XenoAtom.Terminal.UI.Geometry;

namespace XenoAtom.Terminal.UI.Extensions.Screenshot;

/// <summary>
/// Configures raster screenshot export for a <see cref="Rendering.CellBuffer"/>.
/// </summary>
public sealed record CellBufferImageExportOptions
{
    /// <summary>
    /// Gets the default export options.
    /// </summary>
    public static CellBufferImageExportOptions Default { get; } = new();

    /// <summary>
    /// Gets the font settings used for rasterization.
    /// </summary>
    public ScreenshotFontOptions Font { get; init; } = ScreenshotFontOptions.Default;

    /// <summary>
    /// Gets an optional crop region in cell coordinates.
    /// </summary>
    public Rectangle? Crop { get; init; }

    /// <summary>
    /// Gets the padding applied around the crop region in cells.
    /// </summary>
    public Thickness Padding { get; init; } = default;

    /// <summary>
    /// Gets a value indicating whether the exporter should crop to non-empty content automatically.
    /// </summary>
    public bool AutoCrop { get; init; } = true;

    /// <summary>
    /// Gets a value indicating whether the base background should be painted across the whole bitmap.
    /// </summary>
    public bool FillBackground { get; init; } = true;

    /// <summary>
    /// Gets an optional override for the base style used by auto-crop and background decisions.
    /// </summary>
    public Style? BaseStyleOverride { get; init; }

    /// <summary>
    /// Gets the fallback foreground color used when a cell does not define an explicit foreground.
    /// </summary>
    public Color DefaultForeground { get; init; } = Color.Rgb(220, 220, 220);

    /// <summary>
    /// Gets the encoder quality used by lossy formats such as JPEG or WebP.
    /// </summary>
    public int Quality { get; init; } = 100;
}
