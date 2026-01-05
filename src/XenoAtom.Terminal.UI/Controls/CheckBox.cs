// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal;

namespace XenoAtom.Terminal.UI;

public sealed partial class CheckBox : Visual
{
    public CheckBox()
    {
        Focusable = true;
    }

    public CheckBox(string text, bool isChecked = false) : this()
    {
        Text = text;
        IsChecked = isChecked;
    }

    [Bindable]
    public partial string? Text { get; set; }

    [Bindable]
    public partial bool IsChecked { get; set; }

    protected override CellSize MeasureOverride(CellSize availableSize)
    {
        var text = Text ?? string.Empty;
        var width = Math.Min(availableSize.Width, TerminalTextUtility.GetWidth(text.AsSpan()) + 4);
        return new CellSize(width, 1);
    }

    protected override void ArrangeOverride(CellRect finalRect)
    {
        Bounds = finalRect;
    }

    protected override void RenderOverride(CellBuffer buffer)
    {
        var isFocused = ReferenceEquals(App?.FocusedElement, this);
        var theme = GetTheme();
        var checkBoxStyle = GetEnvironmentValue(CheckBoxStyle.Key);
        var style = checkBoxStyle.Resolve(theme, IsEnabled, isFocused, IsHovered);

        var rect = Bounds;
        var text = Text ?? string.Empty;

        buffer.WriteText(rect.X, rect.Y, IsChecked ? "[x] " : "[ ] ", style);
        buffer.WriteText(rect.X + 4, rect.Y, text.AsSpan(), style);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key is TerminalKey.Space or TerminalKey.Enter)
        {
            IsChecked = !IsChecked;
            e.Handled = true;
        }
    }

    protected override void OnPointerPressed(PointerEventArgs e)
    {
        if (e.Button != TerminalMouseButton.Left)
        {
            return;
        }

        IsChecked = !IsChecked;
        e.Handled = true;
    }
}
