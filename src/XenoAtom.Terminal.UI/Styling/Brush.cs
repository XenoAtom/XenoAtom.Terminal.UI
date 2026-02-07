// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Geometry;

namespace XenoAtom.Terminal.UI.Styling;

/// <summary>
/// Defines the kind of brush to sample.
/// </summary>
public enum BrushKind
{
    /// <summary>
    /// A constant color brush.
    /// </summary>
    Solid,

    /// <summary>
    /// A linear gradient brush.
    /// </summary>
    LinearGradient,
}

/// <summary>
/// Defines out-of-range behavior for gradient sampling.
/// </summary>
public enum BrushTileMode
{
    /// <summary>
    /// Clamp sample positions to the [0..1] range.
    /// </summary>
    Clamp,

    /// <summary>
    /// Repeat the gradient every unit interval.
    /// </summary>
    Repeat,

    /// <summary>
    /// Repeat with mirrored direction every other interval.
    /// </summary>
    Mirror,
}

/// <summary>
/// Represents a gradient stop.
/// </summary>
/// <param name="Offset">The stop offset in [0..1].</param>
/// <param name="Color">The stop color.</param>
public readonly record struct GradientStop(float Offset, Color Color);

/// <summary>
/// Represents a normalized point in brush space.
/// </summary>
/// <param name="X">The normalized X coordinate.</param>
/// <param name="Y">The normalized Y coordinate.</param>
public readonly record struct GradientPoint(float X, float Y);

/// <summary>
/// Represents an immutable brush used to sample colors for terminal cells.
/// </summary>
public readonly record struct Brush
{
    private readonly GradientStop[]? _stops;

    private Brush(
        BrushKind kind,
        BrushTileMode tileMode,
        ColorMixSpace? mixSpaceOverride,
        Color solidColor,
        GradientPoint start,
        GradientPoint end,
        GradientStop[]? stops)
    {
        Kind = kind;
        TileMode = tileMode;
        MixSpaceOverride = mixSpaceOverride;
        SolidColor = solidColor;
        Start = start;
        End = end;
        _stops = stops;
    }

    /// <summary>
    /// Gets the brush kind.
    /// </summary>
    public BrushKind Kind { get; }

    /// <summary>
    /// Gets the tile mode used for gradients.
    /// </summary>
    public BrushTileMode TileMode { get; }

    /// <summary>
    /// Gets an optional color mix-space override.
    /// </summary>
    public ColorMixSpace? MixSpaceOverride { get; }

    /// <summary>
    /// Gets the solid color (for <see cref="BrushKind.Solid"/>).
    /// </summary>
    public Color SolidColor { get; }

    /// <summary>
    /// Gets the gradient start point (for <see cref="BrushKind.LinearGradient"/>).
    /// </summary>
    public GradientPoint Start { get; }

    /// <summary>
    /// Gets the gradient end point (for <see cref="BrushKind.LinearGradient"/>).
    /// </summary>
    public GradientPoint End { get; }

    /// <summary>
    /// Gets the gradient stops (for <see cref="BrushKind.LinearGradient"/>).
    /// </summary>
    public ReadOnlyMemory<GradientStop> Stops => _stops ?? ReadOnlyMemory<GradientStop>.Empty;

    /// <summary>
    /// Creates a solid brush.
    /// </summary>
    /// <param name="color">The brush color.</param>
    /// <param name="mixSpaceOverride">Optional interpolation override used when this brush participates in composed effects.</param>
    /// <returns>The created brush.</returns>
    /// <exception cref="ArgumentException"><paramref name="color"/> is <see cref="ColorKind.Default"/>.</exception>
    public static Brush Solid(Color color, ColorMixSpace? mixSpaceOverride = null)
    {
        ValidateColor(color, nameof(color));
        return new Brush(
            kind: BrushKind.Solid,
            tileMode: BrushTileMode.Clamp,
            mixSpaceOverride: mixSpaceOverride,
            solidColor: color,
            start: default,
            end: default,
            stops: null);
    }

    /// <summary>
    /// Creates a linear gradient brush.
    /// </summary>
    /// <param name="start">Normalized gradient start point.</param>
    /// <param name="end">Normalized gradient end point.</param>
    /// <param name="stops">Gradient stops.</param>
    /// <returns>The created brush.</returns>
    /// <exception cref="ArgumentException">Stops are invalid.</exception>
    public static Brush LinearGradient(
        GradientPoint start,
        GradientPoint end,
        params GradientStop[] stops)
    {
        ArgumentNullException.ThrowIfNull(stops);
        return LinearGradient(start, end, stops.AsSpan(), BrushTileMode.Clamp, mixSpaceOverride: null);
    }

    /// <summary>
    /// Creates a linear gradient brush.
    /// </summary>
    /// <param name="start">Normalized gradient start point.</param>
    /// <param name="end">Normalized gradient end point.</param>
    /// <param name="stops">Gradient stops.</param>
    /// <param name="tileMode">Gradient tile mode.</param>
    /// <param name="mixSpaceOverride">Optional interpolation override.</param>
    /// <returns>The created brush.</returns>
    /// <exception cref="ArgumentException">Stops are invalid.</exception>
    public static Brush LinearGradient(
        GradientPoint start,
        GradientPoint end,
        ReadOnlySpan<GradientStop> stops,
        BrushTileMode tileMode = BrushTileMode.Clamp,
        ColorMixSpace? mixSpaceOverride = null)
    {
        var normalizedStops = NormalizeStops(stops);
        return new Brush(
            kind: BrushKind.LinearGradient,
            tileMode: tileMode,
            mixSpaceOverride: mixSpaceOverride,
            solidColor: default,
            start: start,
            end: end,
            stops: normalizedStops);
    }

    /// <summary>
    /// Samples the brush color at the specified cell position.
    /// </summary>
    /// <param name="cellX">The absolute cell x coordinate.</param>
    /// <param name="cellY">The absolute cell y coordinate.</param>
    /// <param name="brushRect">The rectangle defining the brush coordinate space.</param>
    /// <param name="defaultMixSpace">The default mix-space to use when no override is set.</param>
    /// <returns>The sampled color.</returns>
    public Color Sample(int cellX, int cellY, in Rectangle brushRect, ColorMixSpace defaultMixSpace)
    {
        return Kind switch
        {
            BrushKind.Solid => SolidColor,
            BrushKind.LinearGradient => SampleLinear(cellX, cellY, in brushRect, defaultMixSpace),
            _ => Color.Default,
        };
    }

    private Color SampleLinear(int cellX, int cellY, in Rectangle brushRect, ColorMixSpace defaultMixSpace)
    {
        if (_stops is not { Length: > 0 } stops)
        {
            return Color.Default;
        }

        if (brushRect.Width <= 0 || brushRect.Height <= 0)
        {
            return stops[0].Color;
        }

        var u = (cellX - brushRect.X + 0.5f) / brushRect.Width;
        var v = (cellY - brushRect.Y + 0.5f) / brushRect.Height;

        var dx = End.X - Start.X;
        var dy = End.Y - Start.Y;
        var lengthSq = (dx * dx) + (dy * dy);

        if (lengthSq <= float.Epsilon)
        {
            return stops[0].Color;
        }

        var t = (((u - Start.X) * dx) + ((v - Start.Y) * dy)) / lengthSq;
        t = Tile(t, TileMode);

        if (t <= stops[0].Offset)
        {
            return stops[0].Color;
        }

        var last = stops[^1];
        if (t >= last.Offset)
        {
            return last.Color;
        }

        for (var i = 1; i < stops.Length; i++)
        {
            var upper = stops[i];
            if (t > upper.Offset)
            {
                continue;
            }

            var lower = stops[i - 1];
            if (upper.Offset <= lower.Offset)
            {
                return upper.Color;
            }

            var localT = (t - lower.Offset) / (upper.Offset - lower.Offset);
            var mixSpace = MixSpaceOverride ?? defaultMixSpace;
            return Color.Mix(lower.Color, upper.Color, localT, mixSpace);
        }

        return last.Color;
    }

    private static float Tile(float t, BrushTileMode mode)
    {
        return mode switch
        {
            BrushTileMode.Repeat => t - MathF.Floor(t),
            BrushTileMode.Mirror => MirrorTile(t),
            _ => Math.Clamp(t, 0f, 1f),
        };
    }

    private static float MirrorTile(float t)
    {
        var floor = MathF.Floor(t);
        var fract = t - floor;
        return (((int)floor) & 1) == 0 ? fract : 1f - fract;
    }

    private static GradientStop[] NormalizeStops(ReadOnlySpan<GradientStop> stops)
    {
        if (stops.Length < 2)
        {
            throw new ArgumentException("A linear gradient brush requires at least two stops.", nameof(stops));
        }

        var normalized = stops.ToArray();
        var sorted = true;
        var previousOffset = float.NegativeInfinity;

        for (var i = 0; i < normalized.Length; i++)
        {
            var stop = normalized[i];
            if (stop.Offset < 0f || stop.Offset > 1f)
            {
                throw new ArgumentException($"Gradient stop offset must be in [0..1]. Invalid offset at index {i}.", nameof(stops));
            }

            ValidateColor(stop.Color, nameof(stops));

            if (i > 0 && stop.Offset < previousOffset)
            {
                sorted = false;
            }

            previousOffset = stop.Offset;
        }

        if (!sorted)
        {
            Array.Sort(normalized, static (left, right) => left.Offset.CompareTo(right.Offset));
        }

        return normalized;
    }

    private static void ValidateColor(Color color, string argumentName)
    {
        if (color.Kind == ColorKind.Default)
        {
            throw new ArgumentException("Color.Default is not supported in brushes. Use a concrete color.", argumentName);
        }
    }
}
