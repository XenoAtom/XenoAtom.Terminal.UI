// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI;

/// <summary>
/// Identifies the kind of content referenced by a <see cref="GraphicsCommand"/>.
/// </summary>
public enum TerminalGraphicContentKind
{
    /// <summary>
    /// No graphics content is available.
    /// </summary>
    None = 0,

    /// <summary>
    /// Encoded or raw content is available in memory.
    /// </summary>
    Bytes = 1,

    /// <summary>
    /// Content should be loaded from a file path.
    /// </summary>
    File = 2,

    /// <summary>
    /// Content is represented by an application or extension object.
    /// </summary>
    Object = 3,
}

/// <summary>
/// Provides a lightweight, codec-neutral descriptor for graphics content emitted by UI visuals.
/// </summary>
/// <remarks>
/// Core UI code stores descriptors only. Optional graphics packages are responsible for resolving descriptors to decoded
/// image frames and protocol payloads.
/// </remarks>
public sealed class TerminalGraphicContent
{
    private TerminalGraphicContent(
        TerminalGraphicContentKind kind,
        ReadOnlyMemory<byte> bytes,
        string? filePath,
        object? source,
        string? mediaType,
        string? cacheKey,
        long version)
    {
        Kind = kind;
        Bytes = bytes;
        FilePath = filePath;
        Source = source;
        MediaType = mediaType;
        CacheKey = cacheKey;
        Version = version;
    }

    /// <summary>
    /// Gets an empty graphics content descriptor.
    /// </summary>
    public static TerminalGraphicContent Empty { get; } = new(
        TerminalGraphicContentKind.None,
        ReadOnlyMemory<byte>.Empty,
        filePath: null,
        source: null,
        mediaType: null,
        cacheKey: null,
        version: 0);

    /// <summary>
    /// Gets the descriptor kind.
    /// </summary>
    public TerminalGraphicContentKind Kind { get; }

    /// <summary>
    /// Gets in-memory content bytes when <see cref="Kind"/> is <see cref="TerminalGraphicContentKind.Bytes"/>.
    /// </summary>
    public ReadOnlyMemory<byte> Bytes { get; }

    /// <summary>
    /// Gets the file path when <see cref="Kind"/> is <see cref="TerminalGraphicContentKind.File"/>.
    /// </summary>
    public string? FilePath { get; }

    /// <summary>
    /// Gets the extension-provided content object when <see cref="Kind"/> is <see cref="TerminalGraphicContentKind.Object"/>.
    /// </summary>
    public object? Source { get; }

    /// <summary>
    /// Gets the optional media type or pixel-format hint associated with the content.
    /// </summary>
    public string? MediaType { get; }

    /// <summary>
    /// Gets an optional stable cache key for the content.
    /// </summary>
    public string? CacheKey { get; }

    /// <summary>
    /// Gets an optional version that changes when the same source identity has new content.
    /// </summary>
    public long Version { get; }

    /// <summary>
    /// Creates an in-memory graphics content descriptor by copying <paramref name="bytes"/>.
    /// </summary>
    /// <param name="bytes">The content bytes.</param>
    /// <param name="mediaType">An optional media type or pixel-format hint.</param>
    /// <param name="cacheKey">An optional stable cache key.</param>
    /// <param name="version">An optional content version.</param>
    /// <returns>A graphics content descriptor.</returns>
    public static TerminalGraphicContent FromBytes(ReadOnlySpan<byte> bytes, string? mediaType = null, string? cacheKey = null, long version = 0)
        => bytes.IsEmpty
            ? Empty
            : new TerminalGraphicContent(TerminalGraphicContentKind.Bytes, bytes.ToArray(), filePath: null, source: null, mediaType, cacheKey, version);

    /// <summary>
    /// Creates an in-memory graphics content descriptor by copying <paramref name="bytes"/>.
    /// </summary>
    /// <param name="bytes">The content bytes.</param>
    /// <param name="mediaType">An optional media type or pixel-format hint.</param>
    /// <param name="cacheKey">An optional stable cache key.</param>
    /// <param name="version">An optional content version.</param>
    /// <returns>A graphics content descriptor.</returns>
    public static TerminalGraphicContent FromBytes(ReadOnlyMemory<byte> bytes, string? mediaType = null, string? cacheKey = null, long version = 0)
        => bytes.IsEmpty
            ? Empty
            : new TerminalGraphicContent(TerminalGraphicContentKind.Bytes, bytes.ToArray(), filePath: null, source: null, mediaType, cacheKey, version);

    /// <summary>
    /// Creates a file-backed graphics content descriptor.
    /// </summary>
    /// <param name="path">The file path.</param>
    /// <param name="mediaType">An optional media type hint.</param>
    /// <param name="cacheKey">An optional stable cache key.</param>
    /// <param name="version">An optional content version.</param>
    /// <returns>A graphics content descriptor.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="path"/> is <see langword="null"/> or empty.</exception>
    public static TerminalGraphicContent FromFile(string path, string? mediaType = null, string? cacheKey = null, long version = 0)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        return new TerminalGraphicContent(TerminalGraphicContentKind.File, ReadOnlyMemory<byte>.Empty, path, source: null, mediaType, cacheKey, version);
    }

    /// <summary>
    /// Creates an object-backed graphics content descriptor.
    /// </summary>
    /// <param name="source">The extension-provided source object.</param>
    /// <param name="mediaType">An optional media type or source kind hint.</param>
    /// <param name="cacheKey">An optional stable cache key.</param>
    /// <param name="version">An optional content version.</param>
    /// <returns>A graphics content descriptor.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is <see langword="null"/>.</exception>
    public static TerminalGraphicContent FromObject(object source, string? mediaType = null, string? cacheKey = null, long version = 0)
    {
        ArgumentNullException.ThrowIfNull(source);
        return new TerminalGraphicContent(TerminalGraphicContentKind.Object, ReadOnlyMemory<byte>.Empty, filePath: null, source, mediaType, cacheKey, version);
    }
}
