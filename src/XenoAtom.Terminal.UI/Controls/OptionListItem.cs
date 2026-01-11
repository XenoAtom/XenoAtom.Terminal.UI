// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Layout;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.Controls;

public sealed partial class OptionListItem : Visual
{
    private Rectangle _contentRect;
    private Rectangle _shortcutRect;
    private Rectangle _descriptionRect;

    public OptionListItem()
    {
    }

    public OptionListItem(Visual content, Visual? shortcut = null)
    {
        this.Content(content);
        this.Shortcut(shortcut);
    }

    [Bindable]
    public partial Visual? Content { get; set; }

    [Bindable]
    public partial Visual? Shortcut { get; set; }

    [Bindable]
    public partial Visual? Description { get; set; }

    /// <summary>
    /// Optional search text used by <see cref="OptionList"/> for type-to-jump.
    /// </summary>
    [Bindable]
    public partial string? SearchText { get; set; }

    protected override int ChildrenCount
        => (Content is null ? 0 : 1) + (Shortcut is null ? 0 : 1) + (Description is null ? 0 : 1);

    protected override Visual GetChild(int index)
    {
        var i = index;
        if (Content is not null)
        {
            if (i == 0) return Content;
            i--;
        }

        if (Shortcut is not null)
        {
            if (i == 0) return Shortcut;
            i--;
        }

        if (Description is not null)
        {
            if (i == 0) return Description;
        }

        throw new ArgumentOutOfRangeException(nameof(index));
    }

    protected override SizeHints MeasureCore(in LayoutConstraints constraints)
    {
        var style = Get<OptionListStyle>();
        var gap = Math.Max(0, style.SpaceBetweenContentAndShortcut);
        var descriptionIndent = Math.Max(0, style.DescriptionIndent);

        var contentW = 0;
        var shortcutW = 0;
        var descriptionW = 0;

        if (Content is not null)
        {
            Content.Measure(new Size(LayoutConstants.Infinite, 1));
            contentW = Content.DesiredSize.Width;
        }

        if (Shortcut is not null)
        {
            Shortcut.Measure(new Size(LayoutConstants.Infinite, 1));
            shortcutW = Shortcut.DesiredSize.Width;
        }

        if (Description is not null)
        {
            Description.Measure(new Size(LayoutConstants.Infinite, 1));
            descriptionW = Description.DesiredSize.Width;
        }

        var width = contentW;
        if (shortcutW > 0)
        {
            width = contentW + gap + shortcutW;
        }

        if (descriptionW > 0)
        {
            width = Math.Max(width, descriptionIndent + descriptionW);
        }

        var height = Description is null ? 1 : 2;
        return SizeHints.Fixed(constraints.Clamp(new Size(width, height)));
    }

    protected override void ArrangeCore(in Rectangle finalRect)
    {
        var style = Get<OptionListStyle>();
        var gap = Math.Max(0, style.SpaceBetweenContentAndShortcut);
        var descriptionIndent = Math.Max(0, style.DescriptionIndent);

        var shortcut = Shortcut;
        var content = Content;
        var description = Description;

        _shortcutRect = default;
        _contentRect = default;
        _descriptionRect = default;

        var shortcutW = 0;
        if (shortcut is not null && finalRect.Height > 0)
        {
            shortcutW = Math.Min(finalRect.Width, shortcut.DesiredSize.Width);
            _shortcutRect = new Rectangle(finalRect.Right - shortcutW, finalRect.Y, shortcutW, Math.Min(1, finalRect.Height));
        }

        var contentW = finalRect.Width;
        if (shortcutW > 0)
        {
            contentW = Math.Max(0, finalRect.Width - shortcutW - gap);
        }

        if (finalRect.Height > 0)
        {
            _contentRect = new Rectangle(finalRect.X, finalRect.Y, contentW, Math.Min(1, finalRect.Height));
        }

        if (description is not null && finalRect.Height > 1)
        {
            _descriptionRect = new Rectangle(
                finalRect.X + descriptionIndent,
                finalRect.Y + 1,
                Math.Max(0, finalRect.Width - descriptionIndent),
                1);
        }

        content?.Arrange(_contentRect);
        shortcut?.Arrange(_shortcutRect);
        description?.Arrange(_descriptionRect);
    }
}
