// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Text;
using XenoAtom.Terminal.UI.Figlet;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Layout;
using XenoAtom.Terminal.UI.Rendering;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.Controls;

/// <summary>
/// Renders big banner text using a FIGlet font.
/// </summary>
public sealed partial class TextFiglet : Visual
{
    private string? _cachedText;
    private FigletFont? _cachedFont;
    private FigletRenderOptions _cachedOptions;
    private string[] _cachedLines = Array.Empty<string>();
    private int _cachedWidth;

    /// <summary>
    /// Initializes a new instance of the <see cref="TextFiglet"/> class.
    /// </summary>
    public TextFiglet()
    {
        this.Font(FigletFont.Block);
        this.LetterSpacing(1);
        this.TrimTrailingSpaces(true);
        this.MissingGlyph('?');
        this.TextAlignment(TextAlignment.Left);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TextFiglet"/> class with text.
    /// </summary>
    /// <param name="text">The text to render.</param>
    public TextFiglet(string text) : this()
    {
        this.Text(text);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TextFiglet"/> class with dynamic text.
    /// </summary>
    /// <param name="text">A delegate that returns the current text.</param>
    public TextFiglet(Func<string> text) : this()
    {
        this.Text(text);
    }

    /// <summary>
    /// Gets or sets the text to render.
    /// </summary>
    [Bindable]
    public partial string? Text { get; set; }

    /// <summary>
    /// Gets or sets the FIGlet font.
    /// </summary>
    [Bindable]
    public partial FigletFont? Font { get; set; }

    /// <summary>
    /// Gets or sets the number of spaces inserted between characters.
    /// </summary>
    [Bindable]
    public partial int LetterSpacing { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether trailing spaces are trimmed on each output line.
    /// </summary>
    [Bindable]
    public partial bool TrimTrailingSpaces { get; set; }

    /// <summary>
    /// Gets or sets the character used when the font does not define a glyph for the input character.
    /// </summary>
    [Bindable]
    public partial char MissingGlyph { get; set; }

    /// <summary>
    /// Gets or sets the horizontal alignment used when rendering FIGlet text inside the control bounds.
    /// </summary>
    [Bindable]
    public partial TextAlignment TextAlignment { get; set; }

    partial void OnLetterSpacingChanging(ref int value) => value = Math.Max(0, value);

    private void EnsureCache()
    {
        var text = Text ?? string.Empty;
        var font = Font ?? FigletFont.Block;
        var options = new FigletRenderOptions
        {
            LetterSpacing = LetterSpacing,
            TrimTrailingSpaces = TrimTrailingSpaces,
            MissingGlyph = MissingGlyph,
        };

        if (ReferenceEquals(_cachedFont, font) &&
            string.Equals(_cachedText, text, StringComparison.Ordinal) &&
            _cachedOptions.Equals(options))
        {
            return;
        }

        _cachedFont = font;
        _cachedText = text;
        _cachedOptions = options;

        _cachedLines = font.RenderLines(text, options);

        var width = 0;
        for (var i = 0; i < _cachedLines.Length; i++)
        {
            width = Math.Max(width, TerminalTextUtility.GetWidth(_cachedLines[i].AsSpan()));
        }

        _cachedWidth = width;
    }

    /// <inheritdoc />
    protected override SizeHints MeasureCore(in LayoutConstraints constraints)
    {
        EnsureCache();
        var size = new Size(_cachedWidth, _cachedLines.Length);
        return SizeHints.Fixed(constraints.Clamp(size));
    }

    /// <inheritdoc />
    protected override void RenderOverride(CellBuffer buffer)
    {
        EnsureCache();

        var rect = Bounds;
        if (rect.Width <= 0 || rect.Height <= 0)
        {
            return;
        }

        if (_cachedLines.Length == 0)
        {
            return;
        }

        var style = GetStyle<TextFigletStyle>().ResolveTextStyle(GetTheme());
        var alignment = TextAlignment;

        var maxLines = Math.Min(rect.Height, _cachedLines.Length);
        for (var i = 0; i < maxLines; i++)
        {
            var line = _cachedLines[i];
            var span = line.AsSpan();
            var w = TerminalTextUtility.GetWidth(span);
            var x = rect.X;

            if (rect.Width > w)
            {
                x = alignment switch
                {
                    TextAlignment.Center => rect.X + ((rect.Width - w) / 2),
                    TextAlignment.Right => rect.X + (rect.Width - w),
                    _ => rect.X,
                };
            }

            buffer.WriteText(x, rect.Y + i, span, style);
        }
    }
}
