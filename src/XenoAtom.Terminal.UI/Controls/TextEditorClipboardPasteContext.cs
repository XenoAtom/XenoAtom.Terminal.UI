// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal;

namespace XenoAtom.Terminal.UI.Controls;

/// <summary>
/// Handles clipboard content captured before a text editor completes a Ctrl+V paste operation.
/// </summary>
/// <param name="context">The captured clipboard content.</param>
/// <returns>
/// The text to insert instead of the clipboard text; <see langword="null"/> to use the clipboard text when available.
/// Return <see cref="string.Empty"/> to suppress insertion.
/// </returns>
public delegate string? TextEditorClipboardPasteHandler(TextEditorClipboardPasteContext context);

/// <summary>
/// Represents a clipboard data payload captured for a text-editor paste operation.
/// </summary>
/// <param name="Format">The clipboard format identifier. Use <see cref="TerminalClipboardFormats"/> for well-known values.</param>
/// <param name="Data">The raw bytes for the format.</param>
public readonly record struct TextEditorClipboardData(string Format, ReadOnlyMemory<byte> Data);

/// <summary>
/// Provides clipboard content captured before a text editor completes a Ctrl+V paste operation.
/// </summary>
/// <remarks>
/// Text editors create this context only when <see cref="TextEditorBase.ClipboardPasteHandler"/> is set. The handler can
/// inspect advertised formats and raw bytes (for example an image payload) and return replacement text to insert into the editor.
/// </remarks>
public sealed class TextEditorClipboardPasteContext
{
    private static readonly string[] EmptyFormats = [];
    private static readonly TextEditorClipboardData[] EmptyData = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="TextEditorClipboardPasteContext"/> class.
    /// </summary>
    /// <param name="text">The clipboard text, when available.</param>
    /// <param name="formats">The advertised clipboard formats.</param>
    /// <param name="data">The raw data captured for the advertised formats.</param>
    public TextEditorClipboardPasteContext(string? text, IReadOnlyList<string>? formats, IReadOnlyList<TextEditorClipboardData>? data)
    {
        Text = text;
        Formats = formats is { Count: > 0 } ? formats.ToArray() : EmptyFormats;
        Data = data is { Count: > 0 } ? data.ToArray() : EmptyData;
    }

    /// <summary>
    /// Gets the clipboard text captured before the paste operation, when available.
    /// </summary>
    public string? Text { get; }

    /// <summary>
    /// Gets the advertised clipboard formats captured before the paste operation.
    /// </summary>
    public IReadOnlyList<string> Formats { get; }

    /// <summary>
    /// Gets the raw clipboard data captured for the advertised formats.
    /// </summary>
    public IReadOnlyList<TextEditorClipboardData> Data { get; }

    /// <summary>
    /// Gets a value indicating whether captured clipboard text is available and not empty.
    /// </summary>
    public bool HasText => !string.IsNullOrEmpty(Text);

    /// <summary>
    /// Gets a value indicating whether any captured format is not a text format.
    /// </summary>
    public bool HasNonTextFormats
    {
        get
        {
            foreach (var format in Formats)
            {
                if (!IsTextFormat(format))
                {
                    return true;
                }
            }

            return false;
        }
    }

    /// <summary>
    /// Gets a value indicating whether any captured format is an image format.
    /// </summary>
    public bool HasImage
    {
        get
        {
            foreach (var format in Formats)
            {
                if (IsImageFormat(format))
                {
                    return true;
                }
            }

            return false;
        }
    }

    /// <summary>
    /// Attempts to get captured clipboard data for the specified format.
    /// </summary>
    /// <param name="format">The format identifier. Use <see cref="TerminalClipboardFormats"/> for well-known values.</param>
    /// <param name="data">When this method returns <see langword="true"/>, contains the captured bytes for the requested format.</param>
    /// <returns><see langword="true"/> if data for <paramref name="format"/> was captured; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentException"><paramref name="format"/> is <see langword="null"/>, empty, or whitespace.</exception>
    public bool TryGetData(string format, out ReadOnlyMemory<byte> data)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(format);

        foreach (var entry in Data)
        {
            if (string.Equals(entry.Format, format, StringComparison.OrdinalIgnoreCase))
            {
                data = entry.Data;
                return true;
            }
        }

        data = default;
        return false;
    }

    internal static TextEditorClipboardPasteContext Capture(TerminalClipboard clipboard)
    {
        ArgumentNullException.ThrowIfNull(clipboard);

        var text = clipboard.TryGetText(out var clipboardText) ? clipboardText : null;
        var formats = clipboard.GetFormats();
        if (formats.Count == 0 && text is not null)
        {
            formats = [TerminalClipboardFormats.Text];
        }

        List<TextEditorClipboardData>? data = null;
        foreach (var format in formats)
        {
            if (clipboard.TryGetData(format, out var bytes))
            {
                data ??= new List<TextEditorClipboardData>(formats.Count);
                data.Add(new TextEditorClipboardData(format, bytes));
            }
        }

        return new TextEditorClipboardPasteContext(text, formats, data);
    }

    private static bool IsTextFormat(string format)
        => string.Equals(format, TerminalClipboardFormats.Text, StringComparison.OrdinalIgnoreCase)
        || format.StartsWith("text/", StringComparison.OrdinalIgnoreCase);

    private static bool IsImageFormat(string format)
        => format.StartsWith("image/", StringComparison.OrdinalIgnoreCase)
        || string.Equals(format, TerminalClipboardFormats.Png, StringComparison.OrdinalIgnoreCase)
        || string.Equals(format, TerminalClipboardFormats.Tiff, StringComparison.OrdinalIgnoreCase)
        || string.Equals(format, TerminalClipboardFormats.WindowsDeviceIndependentBitmap, StringComparison.OrdinalIgnoreCase)
        || string.Equals(format, TerminalClipboardFormats.WindowsDeviceIndependentBitmapV5, StringComparison.OrdinalIgnoreCase);
}
