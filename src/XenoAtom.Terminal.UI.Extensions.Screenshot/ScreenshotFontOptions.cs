namespace XenoAtom.Terminal.UI.Extensions.Screenshot;

/// <summary>
/// Configures the font used when rasterizing terminal cells.
/// </summary>
public sealed record ScreenshotFontOptions
{
    /// <summary>
    /// Gets the default font options.
    /// </summary>
    public static ScreenshotFontOptions Default { get; } = new();

    /// <summary>
    /// Gets the font size in pixels.
    /// </summary>
    public float SizePx { get; init; } = 18;

    /// <summary>
    /// Gets an optional override for the cell width in pixels.
    /// </summary>
    public float? CellWidthPx { get; init; }

    /// <summary>
    /// Gets an optional override for the cell height in pixels.
    /// </summary>
    public float? CellHeightPx { get; init; }

    /// <summary>
    /// Gets an optional path to a custom font file.
    /// </summary>
    public string? Path { get; init; }

    /// <summary>
    /// Gets an optional installed font family name to use when <see cref="Path"/> is not specified.
    /// </summary>
    public string? FamilyName { get; init; }

    /// <summary>
    /// Gets a value indicating whether glyph antialiasing should be enabled.
    /// </summary>
    public bool Antialias { get; init; } = true;

    /// <summary>
    /// Gets a value indicating whether subpixel text rendering should be enabled.
    /// </summary>
    public bool Subpixel { get; init; } = true;
}
