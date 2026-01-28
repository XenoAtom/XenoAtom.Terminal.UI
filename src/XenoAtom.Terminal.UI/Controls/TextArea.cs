// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Text;
using XenoAtom.Terminal;
using XenoAtom.Terminal.UI.Commands;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Layout;
using XenoAtom.Terminal.UI.Rendering;
using XenoAtom.Terminal.UI.Styling;
using XenoAtom.Terminal.UI.Text;

namespace XenoAtom.Terminal.UI.Controls;

/// <summary>
/// Represents a multi-line text editor with word wrapping enabled by default.
/// </summary>
public sealed partial class TextArea : TextEditorBase
{
    private readonly SearchReplacePopup _searchPopup;

    /// <summary>
    /// Initializes a new instance of the <see cref="TextArea"/> class.
    /// </summary>
    public TextArea()
    {
        this.AcceptTab(true);
        this.WordWrap(true);
        this.HorizontalAlignment(Align.Stretch);
        this.VerticalAlignment(Align.Stretch);

        TextDocument = new DynamicTextDocument(
            getter: () => Text ?? string.Empty,
            setter: value => Text = value);

        _searchPopup = new SearchReplacePopup(CreateSearchReplaceTarget());
        AttachChild(_searchPopup);

        AddCommand(new Command
        {
            Id = "TextEditor.Find",
            LabelMarkup = "Find",
            DescriptionMarkup = "Search within the current document.",
            Gesture = new Input.KeyGesture(TerminalChar.CtrlF, TerminalModifiers.Ctrl),
            Importance = CommandImportance.Secondary,
            Presentation = CommandPresentation.CommandBar,
            Execute = static v => ((TextArea)v).OpenFind(),
        });

        AddCommand(new Command
        {
            Id = "TextEditor.Replace",
            LabelMarkup = "Replace",
            DescriptionMarkup = "Search and replace within the current document.",
            Gesture = new Input.KeyGesture(TerminalChar.CtrlH, TerminalModifiers.Ctrl),
            Importance = CommandImportance.Secondary,
            Presentation = CommandPresentation.CommandBar,
            Execute = static v => ((TextArea)v).OpenReplace(),
        });
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TextArea"/> class with initial text.
    /// </summary>
    /// <param name="text">The initial text.</param>
    public TextArea(string? text) : this()
    {
        this.Text(text);
    }

    /// <summary>
    /// Gets or sets the text content.
    /// </summary>
    [Bindable]
    public partial string? Text { get; set; }

    /// <inheritdoc />
    protected override bool IsSingleLine => false;

    /// <inheritdoc />
    protected override bool AcceptsReturn => true;

    /// <inheritdoc />
    protected override bool ShowPlaceholderWhenUnfocusedOnly => false;

    /// <inheritdoc />
    protected override SizeHints MeasureCore(in LayoutConstraints constraints)
    {
        var width = 32;
        var height = 10;

        return SizeHints.Fixed(constraints.Clamp(new Size(width, height)));
    }

    /// <inheritdoc/>
    protected override int ChildrenCount => 1;

    /// <inheritdoc/>
    protected override Visual GetChild(int index)
        => index == 0 ? _searchPopup : throw new ArgumentOutOfRangeException(nameof(index));

    /// <inheritdoc />
    protected override void ArrangeCore(in Rectangle finalRect)
    {
        var style = GetStyle<TextAreaStyle>();
        var padding = style.Padding;

        var innerLeft = finalRect.X;
        var innerTop = finalRect.Y;
        var innerWidth = finalRect.Width;
        var innerHeight = finalRect.Height;

        var contentRect = new Rectangle(
            innerLeft + padding.Left,
            innerTop + padding.Top,
            Math.Max(0, innerWidth - padding.Horizontal),
            Math.Max(0, innerHeight - padding.Vertical));

        UpdateEditorLayout(contentRect);
        _searchPopup.ArrangeWithin(contentRect);
    }

    /// <inheritdoc />
    protected override void RenderOverride(CellBuffer buffer)
    {
        var rect = Bounds;
        if (rect.Width <= 0 || rect.Height <= 0)
        {
            return;
        }

        var isFocused = HasFocus;
        var theme = GetTheme();
        var style = GetStyle<TextAreaStyle>();
        var selectionStyle = style.SelectionStyle(theme);
        var backgroundStyle = style.BackgroundStyle(theme, isFocused);
        var placeholderStyle = style.PlaceholderStyle(theme, isFocused);
        var padding = style.Padding;

        var innerLeft = rect.X;
        var innerTop = rect.Y;
        var innerWidth = rect.Width;
        var innerHeight = rect.Height;

        var contentRect = new Rectangle(
            innerLeft + padding.Left,
            innerTop + padding.Top,
            Math.Max(0, innerWidth - padding.Horizontal),
            Math.Max(0, innerHeight - padding.Vertical));

        if (contentRect.Width > 0 && contentRect.Height > 0)
        {
            for (var y = contentRect.Y; y < contentRect.Y + contentRect.Height; y++)
            {
                for (var x = contentRect.X; x < contentRect.X + contentRect.Width; x++)
                {
                    buffer.SetCell(x, y, new Rune(' '), backgroundStyle);
                }
            }
        }

        RenderEditor(buffer, contentRect, backgroundStyle, selectionStyle, placeholderStyle);
    }

    /// <inheritdoc />
    protected override bool TryOpenSearchReplacePopup(SearchReplaceMode mode, string? initialSearchText)
        => mode == SearchReplaceMode.Replace
            ? _searchPopup.OpenReplace(initialSearchText)
            : _searchPopup.OpenFind(initialSearchText);
}
